using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;

namespace InfoHelper.Controls
{
    /// <summary>
    /// Follow steps 1a or 1b and then 2 to use this custom control in a XAML file.
    ///
    /// Step 1a) Using this custom control in a XAML file that exists in the current project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:InfoHelper.Controls"
    ///
    ///
    /// Step 1b) Using this custom control in a XAML file that exists in a different project.
    /// Add this XmlNamespace attribute to the root element of the markup file where it is 
    /// to be used:
    ///
    ///     xmlns:MyNamespace="clr-namespace:InfoHelper.Controls;assembly=InfoHelper.Controls"
    ///
    /// You will also need to add a project reference from the project where the XAML file lives
    /// to this project and Rebuild to avoid compilation errors:
    ///
    ///     Right click on the target project in the Solution Explorer and
    ///     "Add Reference"->"Projects"->[Browse to and select this project]
    ///
    ///
    /// Step 2)
    /// Go ahead and use your control in the XAML file.
    ///
    ///     <MyNamespace:PreflopMatrixControl/>
    ///
    /// </summary>
    public class PreflopMatrixControl : HeaderedControl
    {
        private readonly Typeface _typeFace = new Typeface("Tahoma");

        static PreflopMatrixControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PreflopMatrixControl), new FrameworkPropertyMetadata(typeof(PreflopMatrixControl)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            VisualEdgeMode = EdgeMode.Aliased;

            SolidColorBrush[] foregroundBrushesList = (SolidColorBrush[])Application.Current.TryFindResource("PreflopMatrixValueForegroundColors");
            SolidColorBrush[] backgroundBrushesList = (SolidColorBrush[])Application.Current.TryFindResource("PreflopMatrixValueBackgroundColors");

            SolidColorBrush defaultForegroundColor = (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixForegroundBrush");
            SolidColorBrush defaultBackgroundColor = (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixBackgroundBrush");

            Point GetPoint(double x, double y) => new Point(x, y);

            SolidColorBrush GetValueForegroundBrush(int sample)
            {
                if (sample == 0 || foregroundBrushesList == null)
                    return defaultForegroundColor;

                if (sample > foregroundBrushesList.Length)
                    return foregroundBrushesList[^1];

                return foregroundBrushesList[sample - 1];
            }

            SolidColorBrush GetValueBackgroundBrush(int sample)
            {
                if (sample == 0 || backgroundBrushesList == null)
                    return defaultBackgroundColor;

                if (sample > backgroundBrushesList.Length)
                    return backgroundBrushesList[^1];

                return backgroundBrushesList[sample - 1];
            }

            drawingContext.DrawRectangle((SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixBackgroundBrush"), null, new Rect(new Point(0, 0), new Size(RenderSize.Width, RenderSize.Height)));

            double columnWidth = RenderSize.Width / 13, rowHeight = RenderSize.Height / (string.IsNullOrEmpty(Header) ? 13 : 14);

            if(!string.IsNullOrEmpty(Header))
                drawingContext.DrawRectangle((SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixHeaderBackgroundBrush"), null, new Rect(new Point(0, 0), new Size(RenderSize.Width, rowHeight)));

            double headerHeight = !string.IsNullOrEmpty(Header) ? rowHeight : 0;

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                    drawingContext.DrawRectangle(GetValueBackgroundBrush(((PreflopData)Data)?[i * 13 + j] ?? 0), null, new Rect(new Point(j * columnWidth, i * rowHeight + headerHeight), new Size(columnWidth, rowHeight)));
            }

            double rowIndent = 0;

            Pen pen = (Pen)Application.Current.TryFindResource("PreflopMatrixBorderPen");

            if (!string.IsNullOrEmpty(Header))
            {
                FormattedText text = new FormattedText(Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, rowHeight - 2, (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixHeaderForegroundBrush"), 1);

                Point textLocation = new Point(RenderSize.Width / 2 - text.Width / 2, rowHeight / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                rowIndent += rowHeight;

                drawingContext.DrawLine(pen, GetPoint(0, rowIndent), GetPoint(RenderSize.Width, rowIndent));
            }

            for (int i = 0; i < 13; i++)
            {
                double columnIndent = 0;

                for (int j = 0; j < 13; j++)
                {
                    FormattedText text = new FormattedText(Common.HoleCards[i * 13 + j], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, rowHeight - 2, GetValueForegroundBrush(((PreflopData)Data)?[i * 13 + j] ?? 0), 1);

                    Point textLocation = new Point(columnIndent + columnWidth / 2 - text.Width / 2, rowIndent + rowHeight / 2 - text.Height / 2);

                    drawingContext.DrawText(text, textLocation);

                    columnIndent += columnWidth;

                    if (j != 12)
                        drawingContext.DrawLine(pen, GetPoint(columnIndent, rowIndent), GetPoint(columnIndent, rowIndent + rowHeight));
                }

                rowIndent += rowHeight;

                if (i != 12)
                    drawingContext.DrawLine(pen, GetPoint(0, rowIndent), GetPoint(RenderSize.Width, rowIndent));
            }
        }
    }
}
