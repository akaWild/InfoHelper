using System;
using System.Collections.Generic;
using System.Drawing;
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
using StatUtility;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Pen = System.Windows.Media.Pen;
using Point = System.Windows.Point;
using Size = System.Windows.Size;

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

        private const int CounterActionsWidth = 30;

        private readonly Typeface _typeFace = new Typeface("Tahoma");

        private Pen _defaultPen;
        private Pen _avgValuePen;
        private Pen _dfltValuePen;
        private Pen _dashedPen;

        private SolidColorBrush _headerForegroundBrush;
        private SolidColorBrush _headerBackgroundBrush;

        private SolidColorBrush _foregroundColor;
        private SolidColorBrush _gtoForegroundColor;
        private SolidColorBrush _backgroundColor;

        private SolidColorBrush _deviationBackgroudColor;

        static PostlopHandsTableControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PostlopHandsTableControl), new FrameworkPropertyMetadata(typeof(PostlopHandsTableControl)));
        }

        public int Round
        {
            get => (int)GetValue(RoundProperty);
            set => SetValue(RoundProperty, value);
        }

        public static readonly DependencyProperty RoundProperty =
            DependencyProperty.Register("Round", typeof(int), typeof(PostlopHandsTableControl), new PropertyMetadata(0));

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if(Visibility != Visibility.Visible)
                return;

            PostflopHandsGroup handsGroup = (PostflopHandsGroup)Data;

            if(handsGroup == null)
                return;

            _defaultPen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableDefaultPen");
            _avgValuePen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableAvgValuePen");
            _dfltValuePen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableDfltValuePen");
            _dashedPen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableDashedPen");

            _headerForegroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderForegroundBrush");
            _headerBackgroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderBackgroundBrush");

            _foregroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableForegroundBrush");
            _gtoForegroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableGtoForegroundBrush");
            _backgroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableBackgroundBrush");

            _deviationBackgroudColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableDeviationBackgroundBrush");

            drawingContext.DrawRectangle(_backgroundColor, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, RenderSize.Height)));

            double headerHeight = 0;

            if (!string.IsNullOrEmpty(Header))
            {
                headerHeight = HeaderHeight;

                drawingContext.DrawRectangle(_headerBackgroundBrush, null, new Rect(new Point(CounterActionsWidth, 0), new Size(RenderSize.Width - CounterActionsWidth, headerHeight)));

                FormattedText text = new FormattedText(Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, headerHeight - 4, _headerForegroundBrush, 1);

                Point textLocation = new Point(CounterActionsWidth + (RenderSize.Width - CounterActionsWidth) / 2 - text.Width / 2, headerHeight / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                drawingContext.DrawLine(_defaultPen, new Point(CounterActionsWidth, headerHeight), new Point(RenderSize.Width, headerHeight));
            }

            int panelsCount = Round == 4 ? 1 : 2;

            double groupGraphHeight = (RenderSize.Height - headerHeight) / panelsCount;

            if (panelsCount < 2)
                groupGraphHeight -= HeaderHeight;

            int max = 0;

            for (int i = 0; i < PostflopHandsGroup.HandCategoriesCount; i++)
            {
                int mhSum = handsGroup.MadeHands.Select(h => (int)h[i]).Sum();

                if (mhSum > max)
                    max = mhSum;

                int dhSum = handsGroup.DrawHands.Select(h => (int)h[i]).Sum();

                if (dhSum > max)
                    max = dhSum;
            }

            double columnWidth = (RenderSize.Width - CounterActionsWidth) / PostflopHandsGroup.HandCategoriesCount;

            double yIndent = headerHeight;

            if (panelsCount < 2)
                yIndent += HeaderHeight;

            RenderHandsGroup(handsGroup.MadeHandsDefaultValue, handsGroup.MadeHandsGtoValue, true);

            yIndent += groupGraphHeight;

            if (panelsCount == 2)
            {
                RenderHandsGroup(handsGroup.DrawHandsDefaultValue, handsGroup.DrawHandsGtoValue, false);

                drawingContext.DrawLine(_defaultPen, new Point(CounterActionsWidth, headerHeight + groupGraphHeight), new Point(RenderSize.Width, headerHeight + groupGraphHeight));
            }
            else
                drawingContext.DrawLine(_defaultPen, new Point(CounterActionsWidth, headerHeight + HeaderHeight), new Point(RenderSize.Width, headerHeight + HeaderHeight));

            for (int i = 1; i < 4; i++)
            {
                double lineIndent = CounterActionsWidth + (RenderSize.Width - CounterActionsWidth) * i / 4;

                drawingContext.DrawLine(_dashedPen, new Point(lineIndent, headerHeight), new Point(lineIndent, RenderSize.Height));
            }

            int[] counterActions = handsGroup.CounterActions;

            int counterActionsSum = counterActions.Sum();

            if (counterActionsSum > 0)
            {
                double counterActionsYIndent = 0;

                for (int i = 0; i < counterActions.Length; i++)
                {
                    if (counterActions[i] == 0)
                        continue;

                    SolidColorBrush brush = i switch
                    {
                        0 => Brushes.Red,
                        1 => Brushes.ForestGreen,
                        2 => Brushes.Blue,
                        _ => null
                    };

                    double ratio = (double)counterActions[i] / counterActionsSum;

                    double counterActionsHeight = RenderSize.Height * ratio;

                    Size counterActionsSize = new Size(CounterActionsWidth, counterActionsHeight);

                    drawingContext.DrawRectangle(brush, null, new Rect(new Point(0, counterActionsYIndent), counterActionsSize));

                    FormattedText text = new FormattedText($"{Math.Round(ratio * 100)}%", CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, CounterActionsWidth - 8, Brushes.White, 1);

                    if (text.Width < counterActionsSize.Height)
                    {
                        Point textLocation = new Point((float)CounterActionsWidth / 2 - text.Height / 2, counterActionsYIndent + counterActionsSize.Height / 2 + text.Width / 2);

                        drawingContext.PushTransform(new RotateTransform(-90, textLocation.X, textLocation.Y));

                        drawingContext.DrawText(text, textLocation);

                        drawingContext.Pop();
                    }

                    counterActionsYIndent += counterActionsHeight;
                }
            }
            else
                drawingContext.DrawRectangle(Brushes.DimGray, null, new Rect(new Point(0, 0), new Size(CounterActionsWidth, RenderSize.Height)));

            drawingContext.DrawLine(_defaultPen, new Point(CounterActionsWidth, 0), new Point(CounterActionsWidth, RenderSize.Height));

            void RenderHandsGroup(float defaultValue, float gtoValue, bool isMadeHands)
            {
                double xIndent = RenderSize.Width - columnWidth;

                ushort[][] hands = isMadeHands ? handsGroup.MadeHands : handsGroup.DrawHands;

                float accumEquity = (float)(isMadeHands ? handsGroup.MadeHandsAccumulatedEquity : handsGroup.DrawHandsAccumulatedEquity);

                float handsSum = hands.SelectMany(h => h).Select(v => (int)v).Sum();

                float avgEq = 0;

                if (handsSum > 0)
                    avgEq = accumEquity / handsSum;

                int accumHands = 0;

                for (int i = PostflopHandsGroup.HandCategoriesCount - 1; i >= 0; i--)
                {
                    double columnYIndent = yIndent + groupGraphHeight;

                    for (int j = 0; j < hands.Length; j++)
                    {
                        int handsCount = hands[j][i];

                        if (handsCount > 0)
                        {
                            double columnHeightRatio = (double)handsCount / max;

                            double columnHeight = groupGraphHeight * columnHeightRatio;

                            Rect rect = new Rect(xIndent, columnYIndent - columnHeight, columnWidth, columnHeight);

                            drawingContext.DrawRectangle(GetColumnBrush(j), null, rect);

                            columnYIndent -= columnHeight;
                        }

                        accumHands += handsCount;
                    }

                    if (panelsCount < 2 && i % 2 == 0)
                    {
                        string text = handsSum == 0 ? "0" : $"{Math.Round(accumHands * 100 / handsSum).ToString(CultureInfo.InvariantCulture)}";

                        FormattedText formattedText = new FormattedText(text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, HeaderHeight - 5, _foregroundColor, 1);

                        Point textLocation = new Point(xIndent + columnWidth - formattedText.Width / 2, headerHeight + (float)HeaderHeight / 2 - formattedText.Height / 2);

                        drawingContext.DrawText(formattedText, textLocation);

                        drawingContext.DrawLine(_defaultPen, new Point(xIndent, headerHeight), new Point(xIndent, headerHeight + HeaderHeight));
                    }

                    xIndent -= columnWidth;
                }

                Brush GetColumnBrush(int index)
                {
                    return index switch
                    {
                        4 => Brushes.DarkGray,
                        3 => Brushes.Blue,
                        2 => Brushes.Red,
                        1 => Brushes.Green,
                        0 => Brushes.DarkOrange,
                        _ => throw new Exception("Unknown index")
                    };
                }

                if (handsSum > 0)
                {
                    string groupText = $"[{(handsSum >= 100 ? "++" : handsSum)}] Eq: {Math.Round(avgEq, 1).ToString(CultureInfo.InvariantCulture)}%";

                    if (avgEq > 0)
                    {
                        float value = float.NaN;

                        if (!float.IsNaN(gtoValue))
                            value = gtoValue;
                        else if (!float.IsNaN(defaultValue))
                            value = defaultValue;

                        if (!float.IsNaN(value))
                            groupText += $" ({Math.Round(value, 1).ToString(CultureInfo.InvariantCulture)}%)";

                        double avgValueLineIndent = CounterActionsWidth + (RenderSize.Width - CounterActionsWidth) * avgEq / 100;

                        drawingContext.DrawLine(_avgValuePen, new Point(avgValueLineIndent, yIndent), new Point(avgValueLineIndent, yIndent + groupGraphHeight));

                        if (!float.IsNaN(value))
                        {
                            double dfltValueLineIndent = CounterActionsWidth + (RenderSize.Width - CounterActionsWidth) * value / 100;

                            drawingContext.DrawLine(_dfltValuePen, new Point(dfltValueLineIndent, yIndent), new Point(dfltValueLineIndent, yIndent + groupGraphHeight));
                        }
                    }

                    FormattedText formattedText = new FormattedText(groupText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, (RenderSize.Height - headerHeight) / 14, float.IsNaN(gtoValue) ? _foregroundColor : _gtoForegroundColor, 1);

                    formattedText.SetFontWeight(FontWeights.Bold);

                    Point textLocation = new Point(CounterActionsWidth + 3, yIndent);

                    drawingContext.DrawText(formattedText, textLocation);
                }
            }
        }
    }
}
