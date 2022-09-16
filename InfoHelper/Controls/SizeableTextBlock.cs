using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace InfoHelper.Controls
{
    public class SizeableTextBlock : TextBlock
    {
        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);

            if (!IsLoaded)
                return;

            Size n = sizeInfo.NewSize;
            Size p = sizeInfo.PreviousSize;

            double l = n.Width / p.Width;

            if (l != double.PositiveInfinity)
                FontSize *= l;
        }
    }
}
