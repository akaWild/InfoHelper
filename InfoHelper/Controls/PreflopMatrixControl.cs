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

        private Pen _borderPen;
        private SolidColorBrush _headerForegroundBrush;
        private SolidColorBrush _headerBackgroundBrush;
        private SolidColorBrush[] _foregroundBrushesList;
        private SolidColorBrush[] _backgroundBrushesList;
        private SolidColorBrush _defaultForegroundColor;
        private SolidColorBrush _defaultBackgroundColor;

        static PreflopMatrixControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PreflopMatrixControl), new FrameworkPropertyMetadata(typeof(PreflopMatrixControl)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            VisualEdgeMode = EdgeMode.Aliased;

            _borderPen ??= (Pen)Application.Current.TryFindResource("PreflopMatrixBorderPen");

            _headerForegroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixHeaderForegroundBrush");
            _headerBackgroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixHeaderBackgroundBrush");

            _foregroundBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("PreflopMatrixValueForegroundColors");
            _backgroundBrushesList ??= (SolidColorBrush[])Application.Current.TryFindResource("PreflopMatrixValueBackgroundColors");

            _defaultForegroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixForegroundBrush");
            _defaultBackgroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixBackgroundBrush");

            Point GetPoint(double x, double y) => new Point(x, y);

            SolidColorBrush GetValueForegroundBrush(int sample)
            {
                if (sample == 0 || _foregroundBrushesList == null)
                    return _defaultForegroundColor;

                if (sample > _foregroundBrushesList.Length)
                    return _foregroundBrushesList[^1];

                return _foregroundBrushesList[sample - 1];
            }

            SolidColorBrush GetValueBackgroundBrush(int sample)
            {
                if (sample == 0 || _backgroundBrushesList == null)
                    return _defaultBackgroundColor;

                if (sample > _backgroundBrushesList.Length)
                    return _backgroundBrushesList[^1];

                return _backgroundBrushesList[sample - 1];
            }

            drawingContext.DrawRectangle(_defaultBackgroundColor, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, RenderSize.Height)));

            double columnWidth = RenderSize.Width / 13, rowHeight = RenderSize.Height / (string.IsNullOrEmpty(Header) ? 13 : 14);

            if(!string.IsNullOrEmpty(Header))
                drawingContext.DrawRectangle(_headerBackgroundBrush, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, rowHeight)));

            double headerHeight = !string.IsNullOrEmpty(Header) ? rowHeight : 0;

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                    drawingContext.DrawRectangle(GetValueBackgroundBrush(((PreflopData)Data)?.PocketHands[i * 13 + j] ?? 0), null, new Rect(new Point(j * columnWidth, i * rowHeight + headerHeight), new Size(columnWidth, rowHeight)));
            }

            double rowIndent = 0;

            if (!string.IsNullOrEmpty(Header))
            {
                FormattedText text = new FormattedText(Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, rowHeight - 2, _headerForegroundBrush, 1);

                Point textLocation = new Point(RenderSize.Width / 2 - text.Width / 2, rowHeight / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                rowIndent += rowHeight;

                drawingContext.DrawLine(_borderPen, GetPoint(0, rowIndent), GetPoint(RenderSize.Width, rowIndent));
            }

            for (int i = 0; i < 13; i++)
            {
                double columnIndent = 0;

                for (int j = 0; j < 13; j++)
                {
                    FormattedText text = new FormattedText(Common.HoleCards[i * 13 + j], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, rowHeight - 2, GetValueForegroundBrush(((PreflopData)Data)?.PocketHands[i * 13 + j] ?? 0), 1);

                    Point textLocation = new Point(columnIndent + columnWidth / 2 - text.Width / 2, rowIndent + rowHeight / 2 - text.Height / 2);

                    drawingContext.DrawText(text, textLocation);

                    columnIndent += columnWidth;

                    if (j != 12)
                        drawingContext.DrawLine(_borderPen, GetPoint(columnIndent, rowIndent), GetPoint(columnIndent, rowIndent + rowHeight));
                }

                rowIndent += rowHeight;

                if (i != 12)
                    drawingContext.DrawLine(_borderPen, GetPoint(0, rowIndent), GetPoint(RenderSize.Width, rowIndent));
            }
        }
    }
}
