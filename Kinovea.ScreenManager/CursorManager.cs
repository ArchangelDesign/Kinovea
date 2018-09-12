﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Kinovea.ScreenManager
{
    public static class CursorManager
    {
        public static Cursor GetCursor(AbstractDrawingTool tool, double stretchFactor)
        {
            if (tool is DrawingToolPointer)
                return ((DrawingToolPointer)tool).GetCursor();

            if (tool is DrawingTool)
            {
                DrawingStyle style = ToolManager.GetStylePreset(tool.Name);

                if (tool.Name == "Pencil")
                {
                    return GetCursorPencil(style, stretchFactor);
                }
                else if (tool.Name == "CrossMark")
                {
                    return GetCursorCrossMark(style);
                }
                else
                {
                    return Cursors.Cross;
                }
            }
            else if (tool is DrawingToolCoordinateSystem ||
                     tool is DrawingToolAutoNumbers ||
                     tool is DrawingToolSpotlight ||
                     tool is DrawingToolGenericPosture ||
                     tool is DrawingToolTestGrid)
            {
                return Cursors.Cross;
            }
            else
            {
                return null;
            }
        }

        private static Cursor GetCursorPencil(DrawingStyle style, double stretchFactor)
        {
            // Custom cursor: Colored and sized circle.
            string keyColor = "color";
            string keySize = "pen size";

            if (!style.Elements.ContainsKey(keyColor))
                return null;

            if (!style.Elements.ContainsKey(keySize))
                return null;
            
            Color c = (Color)style.Elements[keyColor].Value;
            int size = (int)(stretchFactor * (int)style.Elements[keySize].Value);

            Pen p = new Pen(c, 1);
            Bitmap b = new Bitmap(size + 2, size + 2);
            Graphics g = Graphics.FromImage(b);
            g.DrawEllipse(p, 1, 1, size, size);
            p.Dispose();

            return new Cursor(b.GetHicon());
        }

        private static Cursor GetCursorCrossMark(DrawingStyle style)
        {
            // Custom cursor: cross inside a semi transparent circle (same as drawing).
            string keyColor = "back color";
            if (!style.Elements.ContainsKey(keyColor))
                return null;

            Color c = (Color)style.Elements[keyColor].Value;
            Pen p = new Pen(c, 1);
            Bitmap b = new Bitmap(9, 9);
            Graphics g = Graphics.FromImage(b);

            // Center point is {4,4}
            g.DrawLine(p, 1, 4, 7, 4);
            g.DrawLine(p, 4, 1, 4, 7);

            SolidBrush tempBrush = new SolidBrush(Color.FromArgb(32, c));
            g.FillEllipse(tempBrush, 0, 0, 8, 8);
            tempBrush.Dispose();
            p.Dispose();

            return new Cursor(b.GetHicon());
        }
    }
}