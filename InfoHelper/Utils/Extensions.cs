using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Color = System.Drawing.Color;

namespace InfoHelper.Utils
{
    public static class Extensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color ToDrawingColor(this System.Windows.Media.Color mediaColor)
        {
            return Color.FromArgb(mediaColor.A, mediaColor.R, mediaColor.G, mediaColor.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Windows.Media.Color ToMediaColor(this Color drawingColor)
        {
            return System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rectangle ToDrawingRectangle(this Rect windowsRect)
        {
            return new Rectangle((int)windowsRect.X, (int)windowsRect.Y, (int)windowsRect.Width, (int)windowsRect.Height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect ToWindowsRect(this Rectangle drawingRectangle)
        {
            return new Rect(drawingRectangle.X, drawingRectangle.Y, drawingRectangle.Width, drawingRectangle.Height);
        }
        
        public static BitmapSource ToBitmapSource(this Bitmap bitmap)
        {
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);

            BitmapSource bitmapSource = BitmapSource.Create(
                bitmapData.Width, bitmapData.Height,
                bitmap.HorizontalResolution, bitmap.VerticalResolution,
                PixelFormats.Bgr24, null,
                bitmapData.Scan0, bitmapData.Stride * bitmapData.Height, bitmapData.Stride);

            bitmap.UnlockBits(bitmapData);

            return bitmapSource;
        }

        public static bool ContainsRect(this Rectangle[] rects, Rectangle rectToTest)
        {
            foreach (Rectangle rect in rects)
            {
                if (rect.IntersectsWith(rectToTest))
                    return true;
            }

            return false;
        }
    }
}
