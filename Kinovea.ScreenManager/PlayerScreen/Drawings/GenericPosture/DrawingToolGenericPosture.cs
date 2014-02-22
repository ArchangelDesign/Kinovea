﻿#region License
/*
Copyright © Joan Charmant 2012.
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
using System.Drawing;
using System.IO;
using System.Windows.Forms;

using Kinovea.ScreenManager.Languages;

namespace Kinovea.ScreenManager
{
    public class DrawingToolGenericPosture : AbstractDrawingTool
    {
        #region Properties
        public override string DisplayName
        {
            get { return displayName;}
        }
        public override Bitmap Icon
        {
            get { return icon; }
        }
        public override bool Attached
        {
            get { return true; }
        }
        public override bool KeepTool
        {
            get { return false; }
        }
        public override bool KeepToolFrameChanged
        {
            get { return false; }
        }
        public override DrawingStyle StylePreset
        {
            get { return m_StylePreset;}
            set { m_StylePreset = value;}
        }
        public override DrawingStyle DefaultStylePreset
        {
            get { return m_DefaultStylePreset;}
        }
        #endregion
        
        #region Members
        private DrawingStyle m_DefaultStylePreset = new DrawingStyle();
        private DrawingStyle m_StylePreset;
        private Guid id;
        private string displayName = "Generic Posture";
        private Bitmap icon = Properties.Drawings.generic_posture;
        #endregion
        
        #region Constructor
        public DrawingToolGenericPosture()
        {
            m_DefaultStylePreset.Elements.Add("line color", new StyleElementColor(Color.DarkOliveGreen));
            m_StylePreset = m_DefaultStylePreset.Clone();
        }
        #endregion
        
        #region Public Methods
        public override AbstractDrawing GetNewDrawing(Point _Origin, long _iTimestamp, long _AverageTimeStampsPerFrame, IImageToViewportTransformer transformer)
        {
            m_StylePreset = ToolManager.GenericPosture.m_StylePreset.Clone();
            
            GenericPosture posture = GenericPostureManager.Instanciate(id, false);
            return new DrawingGenericPosture(posture, _iTimestamp, _AverageTimeStampsPerFrame, m_StylePreset);
        }
        public override Cursor GetCursor(double _fStretchFactor)
        {
            return Cursors.Cross;
        }
        public void SetInfo(GenericPosture posture)
        {
            this.id = posture.Id;
            
            if(!string.IsNullOrEmpty(posture.Name))
               displayName = posture.Name;
            
            if(posture.Icon != null && posture.Icon.Width == 16 && posture.Icon.Height == 16)
              icon = posture.Icon;
        }
        #endregion
    }
}




