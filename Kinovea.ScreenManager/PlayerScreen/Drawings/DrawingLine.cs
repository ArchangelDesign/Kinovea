#region License
/*
Copyright � Joan Charmant 2008-2011.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/
#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using Kinovea.ScreenManager.Languages;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    [XmlType ("Line")]
    public class DrawingLine : AbstractDrawing, IKvaSerializable, IDecorable, IInitializable, ITrackable, IMeasurable
    {
        #region Events
        public event EventHandler<TrackablePointMovedEventArgs> TrackablePointMoved;
        public event EventHandler ShowMeasurableInfoChanged;
        #endregion
        
        #region Properties
        public override string DisplayName
        {
            get {  return ScreenManagerLang.ToolTip_DrawingToolLine2D; }
        }
        public override int ContentHash
        {
            get 
            {
                int iHash = 0;
                iHash ^= styleHelper.ContentHash;
                iHash ^= ShowMeasurableInfo.GetHashCode();
                iHash ^= infosFading.ContentHash;
                return iHash;
            }
        }
        public DrawingStyle DrawingStyle
        {
            get { return style;}
        }
        public override InfosFading InfosFading
        {
            get { return infosFading; }
            set { infosFading = value; }
        }
        public override DrawingCapabilities Caps
        {
            get { return DrawingCapabilities.ConfigureColorSize | DrawingCapabilities.Fading | DrawingCapabilities.Track; }
        }
        public override List<ToolStripItem> ContextMenu
        {
            get 
            {
                // Rebuild the menu to get the localized text.
                List<ToolStripItem> contextMenu = new List<ToolStripItem>();
                
                mnuShowMeasure.Text = ScreenManagerLang.mnuShowMeasure;
                mnuShowMeasure.Checked = ShowMeasurableInfo;
                mnuSealMeasure.Text = ScreenManagerLang.mnuCalibrate;
                
                contextMenu.Add(mnuShowMeasure);
                contextMenu.Add(mnuSealMeasure);
                
                return contextMenu; 
            }
        }
        
        public CalibrationHelper CalibrationHelper { get; set; }
        public bool ShowMeasurableInfo { get; set; }
        #endregion

        #region Members
        private Dictionary<string, PointF> points = new Dictionary<string, PointF>();
        private bool tracking;
        
        // Decoration
        private StyleHelper styleHelper = new StyleHelper();
        private DrawingStyle style;
        private KeyframeLabel labelMeasure;
        private InfosFading infosFading;
        
        // Context menu
        private ToolStripMenuItem mnuShowMeasure = new ToolStripMenuItem();
        private ToolStripMenuItem mnuSealMeasure = new ToolStripMenuItem();
        
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        #endregion

        #region Constructors
        public DrawingLine(Point start, Point end, long timestamp, long averageTimeStampsPerFrame, DrawingStyle preset, IImageToViewportTransformer transformer)
        {
            points["a"] = start;
            points["b"] = end;
            labelMeasure = new KeyframeLabel(GetMiddlePoint(), Color.Black, transformer);
            
            // Decoration
            styleHelper.Color = Color.DarkSlateGray;
            styleHelper.LineSize = 1;
            if(preset != null)
            {
                style = preset.Clone();
                BindStyle();
            }
            
            // Fading
            infosFading = new InfosFading(timestamp, averageTimeStampsPerFrame);
            
            // Context menu
            mnuShowMeasure.Click += new EventHandler(mnuShowMeasure_Click);
            mnuShowMeasure.Image = Properties.Drawings.measure;
            mnuSealMeasure.Click += new EventHandler(mnuSealMeasure_Click);
            mnuSealMeasure.Image = Properties.Drawings.linecalibrate;
        }
        public DrawingLine(XmlReader xmlReader, PointF scale, Metadata parent)
            : this(Point.Empty, Point.Empty, 0, 0, ToolManager.Line.StylePreset.Clone(), null)
        {
            ReadXml(xmlReader, scale);
        }
        #endregion

        #region AbstractDrawing Implementation
        public override void Draw(Graphics canvas, IImageToViewportTransformer transformer, bool selected, long currentTimestamp)
        {
            double opacityFactor = infosFading.GetOpacityFactor(currentTimestamp);
            
            if(tracking)
                opacityFactor = 1.0;
            
            if(opacityFactor <= 0)
                return;
            
            Point start = transformer.Transform(points["a"]);
            Point end = transformer.Transform(points["b"]);
            
            using(Pen penEdges = styleHelper.GetPen((int)(opacityFactor * 255), transformer.Scale))
            {
                canvas.DrawLine(penEdges, start, end);
                
                // Handlers
                penEdges.Width = selected ? 2 : 1;
                if(styleHelper.LineEnding.StartCap != LineCap.ArrowAnchor)
                    canvas.DrawEllipse(penEdges, start.Box(3));
                
                if(styleHelper.LineEnding.EndCap != LineCap.ArrowAnchor)
                    canvas.DrawEllipse(penEdges, end.Box(3));
            }

            if(ShowMeasurableInfo)
            {
                // Text of the measure. (The helpers knows the unit)
                PointF a = new PointF(points["a"].X, points["a"].Y);
                PointF b = new PointF(points["b"].X, points["b"].Y);
                labelMeasure.SetText(CalibrationHelper.GetLengthText(a, b, true, true));
                labelMeasure.Draw(canvas, transformer, opacityFactor);
            }
        }
        public override int HitTest(Point point, long currentTimestamp, IImageToViewportTransformer transformer, bool zooming)
        {
            int result = -1;
            double opacity = infosFading.GetOpacityFactor(currentTimestamp);
            
            if (tracking || opacity > 0)
            {
                if(ShowMeasurableInfo && labelMeasure.HitTest(point, transformer))
                    result = 3;
                else if (HitTester.HitTest(points["a"], point, transformer))
                    result = 1;
                else if (HitTester.HitTest(points["b"], point, transformer))
                    result = 2;
                else if (IsPointInObject(point, transformer))
                    result = 0;
            }
            
            return result;
        }
        public override void MoveHandle(PointF point, int handleNumber, Keys modifiers)
        {
            int constraintAngleSubdivisions = 8; // (Constraint by 45� steps).
            switch(handleNumber)
            {
                case 1:
                    if((modifiers & Keys.Shift) == Keys.Shift)
                        points["a"] = GeometryHelper.GetPointAtClosestRotationStepCardinal(points["b"], point, constraintAngleSubdivisions);
                    else
                        points["a"] = point;

                    labelMeasure.SetAttach(GetMiddlePoint(), true);
                    SignalTrackablePointMoved("a");
                    break;
                case 2:
                    if((modifiers & Keys.Shift) == Keys.Shift)
                        points["b"] = GeometryHelper.GetPointAtClosestRotationStepCardinal(points["a"], point, constraintAngleSubdivisions);
                    else
                        points["b"] = point;

                    labelMeasure.SetAttach(GetMiddlePoint(), true);
                    SignalTrackablePointMoved("b");
                    break;
                case 3:
                    // Move the center of the mini label to the mouse coord.
                    labelMeasure.SetLabel(point);
                    break;
            }
        }
        public override void MoveDrawing(float dx, float dy, Keys modifiers, bool zooming)
        {
            points["a"] = points["a"].Translate(dx, dy);
            points["b"] = points["b"].Translate(dx, dy);
            labelMeasure.SetAttach(GetMiddlePoint(), true);
            SignalAllTrackablePointsMoved();
        }
        #endregion

        #region KVA Serialization
        private void ReadXml(XmlReader xmlReader, PointF scale)
        {
            if (xmlReader.MoveToAttribute("id"))
                identifier = new Guid(xmlReader.ReadContentAsString());

            xmlReader.ReadStartElement();
            
            while(xmlReader.NodeType == XmlNodeType.Element)
            {
                switch(xmlReader.Name)
                {
                    case "Start":
                        {
                            PointF p = XmlHelper.ParsePointF(xmlReader.ReadElementContentAsString());
                            points["a"] = p.Scale(scale.X, scale.Y);
                            break;
                        }
                    case "End":
                        {
                            PointF p = XmlHelper.ParsePointF(xmlReader.ReadElementContentAsString());
                            points["b"] = p.Scale(scale.X, scale.Y);
                            break;
                        }
                    case "DrawingStyle":
                        style = new DrawingStyle(xmlReader);
                        BindStyle();
                        break;
                    case "InfosFading":
                        infosFading.ReadXml(xmlReader);
                        break;
                    case "MeasureVisible":
                        ShowMeasurableInfo = XmlHelper.ParseBoolean(xmlReader.ReadElementContentAsString());
                        break;
                    default:
                        string unparsed = xmlReader.ReadOuterXml();
                        log.DebugFormat("Unparsed content in KVA XML: {0}", unparsed);
                        break;
                }
            }
            
            xmlReader.ReadEndElement();
            
            labelMeasure.SetAttach(GetMiddlePoint(), true);
            SignalAllTrackablePointsMoved();
        }
        public void WriteXml(XmlWriter w)
        {
            w.WriteElementString("Start", XmlHelper.WritePointF(points["a"]));
            w.WriteElementString("End", XmlHelper.WritePointF(points["b"]));
            w.WriteElementString("MeasureVisible", ShowMeasurableInfo.ToString().ToLower());
            
            w.WriteStartElement("DrawingStyle");
            style.WriteXml(w);
            w.WriteEndElement();
            
            w.WriteStartElement("InfosFading");
            infosFading.WriteXml(w);
            w.WriteEndElement();  

            if(ShowMeasurableInfo)
            {
                // Spreadsheet support.
                w.WriteStartElement("Measure");
                
                PointF a = CalibrationHelper.GetPoint(new PointF(points["a"].X, points["a"].Y));
                PointF b = CalibrationHelper.GetPoint(new PointF(points["b"].X, points["b"].Y));

                float len = GeometryHelper.GetDistance(a, b);
                string value = String.Format("{0:0.00}", len);
                string valueInvariant = String.Format(CultureInfo.InvariantCulture, "{0:0.00}", len);

                w.WriteAttributeString("UserLength", value);
                w.WriteAttributeString("UserLengthInvariant", valueInvariant);
                w.WriteAttributeString("UserUnitLength", CalibrationHelper.GetLengthAbbreviation());
                
                w.WriteEndElement();
            }
        }
        #endregion
        
        #region IInitializable implementation
        public void ContinueSetup(PointF point, Keys modifiers)
        {
            MoveHandle(point, 2, modifiers);
        }
        #endregion
        
        #region ITrackable implementation and support.
        public TrackingProfile CustomTrackingProfile
        {
            get { return null; }
        }
        public Dictionary<string, PointF> GetTrackablePoints()
        {
            return points;
        }
        public void SetTracking(bool tracking)
        {
            this.tracking = tracking;
        }
        public void SetTrackablePointValue(string name, PointF value)
        {
            if(!points.ContainsKey(name))
                throw new ArgumentException("This point is not bound.");
            
            points[name] = value;
            labelMeasure.SetAttach(GetMiddlePoint(), true);
        }
        private void SignalAllTrackablePointsMoved()
        {
            if(TrackablePointMoved == null)
                return;
            
            foreach(KeyValuePair<string, PointF> p in points)
                TrackablePointMoved(this, new TrackablePointMovedEventArgs(p.Key, p.Value));
        }
        private void SignalTrackablePointMoved(string name)
        {
            if(TrackablePointMoved == null || !points.ContainsKey(name))
                return;
            
            TrackablePointMoved(this, new TrackablePointMovedEventArgs(name, points[name]));
        }
        #endregion
        
        public float Length()
        {
            return GeometryHelper.GetDistance(points["a"], points["b"]);
        }
        
        #region Context menu
        private void mnuShowMeasure_Click(object sender, EventArgs e)
        {
            // Enable / disable the display of the measure for this line.
            ShowMeasurableInfo = !ShowMeasurableInfo;
            if(ShowMeasurableInfoChanged != null)
                ShowMeasurableInfoChanged(this, EventArgs.Empty);
            
            CallInvalidateFromMenu(sender);
        }
        
        private void mnuSealMeasure_Click(object sender, EventArgs e)
        {
            if(points["a"].X == points["b"].X && points["a"].Y == points["b"].Y)
                return;
            
            if(!ShowMeasurableInfo)
            {
                ShowMeasurableInfo = true;
                if(ShowMeasurableInfoChanged != null)
                    ShowMeasurableInfoChanged(this, EventArgs.Empty);
            }
            
            FormCalibrateLine fcm = new FormCalibrateLine(CalibrationHelper, this);
            FormsHelper.Locate(fcm);
            fcm.ShowDialog();
            fcm.Dispose();
            
            CallInvalidateFromMenu(sender);
        }
        #endregion
        
        #region Lower level helpers
        private void BindStyle()
        {
            style.Bind(styleHelper, "Color", "color");
            style.Bind(styleHelper, "LineSize", "line size");
            style.Bind(styleHelper, "LineEnding", "arrows");
        }
        private bool IsPointInObject(Point point, IImageToViewportTransformer transformer)
        {
            using(GraphicsPath areaPath = new GraphicsPath())
            {
                if(points["a"] == points["b"])
                    areaPath.AddLine(points["a"].X, points["a"].Y, points["a"].X + 2, points["a"].Y + 2);
                else
                    areaPath.AddLine(points["a"], points["b"]);

                return HitTester.HitTest(areaPath, point, styleHelper.LineSize, false, transformer);
            }
        }
        private Point GetMiddlePoint()
        {
            // Used only to attach the measure.
            return GeometryHelper.GetMiddlePoint(points["a"], points["b"]).ToPoint();
        }
        
        #endregion
    }
}