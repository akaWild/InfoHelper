using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BitmapHelper;
using InfoHelper.Utils;

namespace InfoHelper.DataProcessor
{
    public class CursorManager
    {
        public static Point? FindCursor(BitmapDecorator bitmap)
        {
            Point? cursorPosition = null;

            Parallel.For(0, bitmap.Width, (i, state) => {
                bool innerLoopBreak = false;

                for (int j = 0; j < bitmap.Height; j++)
                {
                    if (bitmap.Height - j < Shared.MouseCursor.Height)
                        break;

                    if (CursorFound(bitmap, Shared.MouseCursor, i, j))
                    {
                        cursorPosition = new Point(i, j);

                        innerLoopBreak = true;

                        break;
                    }
                }

                if (innerLoopBreak)
                    state.Break();
            });

            return cursorPosition;
        }

        private static unsafe bool CursorFound(BitmapDecorator bitmap, BitmapDecorator cursor, int xOffset, int yOffset)
        {
            for (int i = 0; i < cursor.Width; i++)
            {
                for (int j = 0; j < cursor.Height; j++)
                {
                    byte* templateColorValues = cursor.GetColorValuesUnsafe(i, j);

                    byte* mainColorValues = bitmap.GetColorValuesUnsafe(i + xOffset, j + yOffset);

                    if (templateColorValues[3] == 0)
                        continue;

                    if (templateColorValues[2] != mainColorValues[2])
                        return false;

                    if (templateColorValues[1] != mainColorValues[1])
                        return false;

                    if (templateColorValues[0] != mainColorValues[0])
                        return false;
                }
            }

            return true;
        }
    }
}
