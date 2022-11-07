using System;
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
    ///     <MyNamespace:PostlopHandsTableControl/>
    ///
    /// </summary>
    public class PostlopHandsTableControl : HeaderedControl
    {
        private const int HeaderHeight = 18;

        private readonly Typeface _typeFace = new Typeface("Tahoma");

        private Pen _pen;

        private SolidColorBrush _headerForegroundBrush;
        private SolidColorBrush _headerBackgroundBrush;

        private SolidColorBrush _foregroundColor;
        private SolidColorBrush _backgroundColor;

        static PostlopHandsTableControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PostlopHandsTableControl), new FrameworkPropertyMetadata(typeof(PostlopHandsTableControl)));
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            HandsGroup handsGroup = (HandsGroup)Data;

            if(handsGroup == null)
                return;

            VisualEdgeMode = EdgeMode.Aliased;

            _pen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTablePen");

            _headerForegroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderForegroundBrush");
            _headerBackgroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderBackgroundBrush");

            _foregroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableForegroundBrush");
            _backgroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableBackgroundBrush");

            drawingContext.DrawRectangle(_backgroundColor, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, RenderSize.Height)));

            double headerHeight = 0;

            if (!string.IsNullOrEmpty(Header))
            {
                headerHeight = HeaderHeight;

                drawingContext.DrawRectangle(_headerBackgroundBrush, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, headerHeight)));

                FormattedText text = new FormattedText(Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, headerHeight - 2, _headerForegroundBrush, 1);

                Point textLocation = new Point(RenderSize.Width / 2 - text.Width / 2, headerHeight / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                drawingContext.DrawLine(_pen, new Point(0, headerHeight), new Point(RenderSize.Width, headerHeight));
            }

            double graphHeight = RenderSize.Height - headerHeight;

            int max = handsGroup.MadeHands.Select((v, i) => v + handsGroup.DrawHands[i]).Max();

            int sum = handsGroup.MadeHands.Sum() + handsGroup.DrawHands.Sum();

            int groups = handsGroup.MadeHands.Length;

            double columnSpaceRatio = 0.2;

            double columnWidth = RenderSize.Width / (groups + columnSpaceRatio * (groups + 1));

            double xIndent = columnSpaceRatio * columnWidth;

            for (int i = 0; i < groups; i++)
            {
                int madeHands = handsGroup.MadeHands[i], drawHands = handsGroup.DrawHands[i];

                double columnRatio = (double)(madeHands + drawHands) / max;

                double columnHeight = graphHeight * columnRatio;

                double yIndent = (1 - columnRatio) * graphHeight + headerHeight;

                if (madeHands > 0)
                {
                    Rect rect = new Rect(xIndent, yIndent, columnWidth, columnHeight * ((double)madeHands / (madeHands + drawHands)));

                    drawingContext.DrawRectangle(Brushes.ForestGreen, null, rect);

                    yIndent += rect.Height;
                }

                if (drawHands > 0)
                {
                    Rect rect = new Rect(xIndent, yIndent, columnWidth, columnHeight * ((double)drawHands / (madeHands + drawHands)));

                    drawingContext.DrawRectangle(Brushes.Red, null, rect);
                }

                xIndent += columnWidth + columnSpaceRatio * columnWidth;
            }

            FormattedText attemptsText = new FormattedText($"{sum}", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, graphHeight - 10, _foregroundColor, 1);

            Point attemptsLocation = new Point(0, headerHeight + graphHeight / 2 - attemptsText.Height / 2);

            drawingContext.DrawText(attemptsText, attemptsLocation);
        }
    }
}
