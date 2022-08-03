using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace InfoHelper.Utils
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Color ToDrawingColor(this System.Windows.Media.Color mediaColor)
        {
            return System.Drawing.Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Windows.Media.Color ToMediaColor(this System.Drawing.Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Drawing.Rectangle ToDrawingRectangle(this System.Windows.Rect windowsRect)
        {
            return new System.Drawing.Rectangle((int)windowsRect.X, (int)windowsRect.Y, (int)windowsRect.Width, (int)windowsRect.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Windows.Rect ToWindowsRect(this System.Drawing.Rectangle drawingRectangle)
        {
            return new System.Windows.Rect(drawingRectangle.X, drawingRectangle.Y, drawingRectangle.Width, drawingRectangle.Height);
        }
    }
}
