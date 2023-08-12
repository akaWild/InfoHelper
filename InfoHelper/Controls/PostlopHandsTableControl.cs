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

        private const int InfoHeight = 20;

        private const int CounterActionsWidth = 30;

        private const double GroupNameWidthRatio = 0.1;

        private readonly Typeface _typeFace = new Typeface("Tahoma");

        private readonly string[] _groupNamesFlopTurn = new string[16] { "Q+", "FH", "F", "STR", "S/T", "2Ps", "OP", "TP", "MP", "WP", "LP", "HC", "CD", "FD", "SD", "GS"};
        private readonly string[] _groupNamesRiver = new string[16] { "Q+", "FH", "F", "STR", "S/T", "2Ps", "OP", "TP", "MP", "WP", "LP", "HC", "MCD", "MFD", "MSD", "MGS" };

        private readonly int[][][] _handGroupRanges = new int[3][][]
        {
            new int[16][]
            {
                new int[2] {54, 100},
                new int[2] {2, 78},
                new int[2] {34, 75},
                new int[2] {10, 80},
                new int[2] {22, 79},
                new int[2] {9, 56},
                new int[2] {4, 43},
                new int[2] {0, 50},
                new int[2] {0, 38},
                new int[2] {0, 35},
                new int[2] {0, 20},
                new int[2] {0, 7},
                new int[2] {0, 37},
                new int[2] {0, 28},
                new int[2] {0, 17},
                new int[2] {0, 11}
            },
            new int[16][]
            {
                new int[2] {32, 100},
                new int[2] {5, 76},
                new int[2] {14, 68},
                new int[2] {5, 75},
                new int[2] {8, 75},
                new int[2] {2, 56},
                new int[2] {2, 41},
                new int[2] {0, 39},
                new int[2] {0, 27},
                new int[2] {0, 26},
                new int[2] {0, 26},
                new int[2] {0, 30},
                new int[2] {0, 22},
                new int[2] {0, 18},
                new int[2] {0, 12},
                new int[2] {0, 13}
            },
            new int[16][]
            {
                new int[2] {31, 100},
                new int[2] {5, 83},
                new int[2] {2, 82},
                new int[2] {1, 68},
                new int[2] {6, 63},
                new int[2] {2, 47},
                new int[2] {2, 39},
                new int[2] {1, 37},
                new int[2] {0, 30},
                new int[2] {0, 27},
                new int[2] {0, 24},
                new int[2] {0, 44},
                new int[2] {0, 19},
                new int[2] {0, 19},
                new int[2] {0, 11},
                new int[2] {0, 19}
            },
        };

        private Pen _defaultPen;
        private Pen _defaultGroupDelimiterPen;

        private SolidColorBrush _headerForegroundBrush;
        private SolidColorBrush _headerBackgroundBrush;

        private SolidColorBrush _foregroundColor;
        private SolidColorBrush _gtoForegroundColor;
        private SolidColorBrush _backgroundColor;
        private SolidColorBrush _outOfRangeBackgroundColor;

        private SolidColorBrush[] _postflopHandsTableBackgroundColors;

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

        public bool ShowGroupHeader
        {
            get => (bool)GetValue(ShowGroupHeaderProperty);
            set => SetValue(ShowGroupHeaderProperty, value);
        }

        public static readonly DependencyProperty ShowGroupHeaderProperty =
            DependencyProperty.Register("ShowGroupHeader", typeof(bool), typeof(PostlopHandsTableControl), new PropertyMetadata(false));

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if(Visibility != Visibility.Visible)
                return;

            PostflopHandsGroup handsGroup = (PostflopHandsGroup)Data;

            if(handsGroup == null)
                return;

            VisualEdgeMode = EdgeMode.Aliased;

            _defaultPen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableDefaultPen");
            _defaultGroupDelimiterPen ??= (Pen)Application.Current.TryFindResource("PostflopHandsTableGroupDelimiterPen");

            _headerForegroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderForegroundBrush");
            _headerBackgroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableHeaderBackgroundBrush");

            _foregroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableForegroundBrush");
            _gtoForegroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableGtoForegroundBrush");
            _backgroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableBackgroundBrush");
            _outOfRangeBackgroundColor ??= (SolidColorBrush)Application.Current.TryFindResource("PostflopHandsTableOutOfRangeBackgroundBrush");

            _postflopHandsTableBackgroundColors ??= (SolidColorBrush[])Application.Current.TryFindResource("PostflopHandsTableBackgroundColors");

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

            double yIndent = headerHeight;

            double infoHeight = 0;

            if (ShowGroupHeader)
            {
                infoHeight = InfoHeight;

                float handsSum = handsGroup.Hands.Select(v => (int)v).Sum();

                if (handsSum > 0)
                {
                    float avgEv = (float)handsGroup.AccumulatedEv / handsSum;

                    string groupText = $"[{(handsSum >= 100 ? "++" : handsSum)}] Ev: {Math.Round(avgEv, 1).ToString(CultureInfo.InvariantCulture)}";

                    if (avgEv > 0)
                    {
                        float value = float.NaN;

                        if (!float.IsNaN(handsGroup.GtoValue))
                            value = handsGroup.GtoValue;
                        else if (!float.IsNaN(handsGroup.DefaultValue))
                            value = handsGroup.DefaultValue;

                        if (!float.IsNaN(value))
                            groupText += $" ({Math.Round(avgEv - value, 1).ToString("+0.#;-0.#", CultureInfo.InvariantCulture)})";
                    }

                    double madeHandsSumRatio = Math.Round(handsGroup.Hands.Take(12 * PostflopHandsGroup.HandCategoriesCount).Select(v => (int)v).Sum() * 100 / handsSum);

                    double drawHandsSumRatio = 100 - madeHandsSumRatio;

                    groupText += $" M/D: {madeHandsSumRatio.ToString(CultureInfo.InvariantCulture)}/{drawHandsSumRatio.ToString(CultureInfo.InvariantCulture)}%";

                    FormattedText formattedText = new FormattedText(groupText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, infoHeight - 6, float.IsNaN(handsGroup.GtoValue) ? _foregroundColor : _gtoForegroundColor, 1);

                    Point textLocation = new Point(CounterActionsWidth + 3, yIndent + infoHeight / 2f - formattedText.Height / 2);

                    drawingContext.DrawText(formattedText, textLocation);
                }

                yIndent += infoHeight;

                drawingContext.DrawLine(_defaultPen, new Point(CounterActionsWidth, yIndent), new Point(RenderSize.Width, yIndent));
            }

            double groupNameWidth = GroupNameWidthRatio * (RenderSize.Width - CounterActionsWidth);

            int rows = handsGroup.Hands.Length / PostflopHandsGroup.HandCategoriesCount;

            double cellWidth = (RenderSize.Width - CounterActionsWidth - groupNameWidth) / PostflopHandsGroup.HandCategoriesCount;

            double cellHeight = (RenderSize.Height - headerHeight - infoHeight) / rows;

            for (int i = 0; i < rows; i++)
            {
                FormattedText groupNameText = new FormattedText(Round < 4 ? _groupNamesFlopTurn[i] : _groupNamesRiver[i], CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, cellHeight, _foregroundColor, 1);

                Point textLocation = new Point(CounterActionsWidth + groupNameWidth / 2 - groupNameText.Width / 2, yIndent + cellHeight / 2f - groupNameText.Height / 2);

                drawingContext.DrawText(groupNameText, textLocation);

                yIndent += cellHeight;
            }

            double xIndent = CounterActionsWidth + groupNameWidth;

            yIndent = headerHeight + infoHeight;

            double evPerCell = PostflopHandsGroup.NutHandsEvThreshold / (PostflopHandsGroup.HandCategoriesCount - 1);

            int cellXOffset = 0;

            double evLowBound, evUpperBound;

            for (int i = 0; i < handsGroup.Hands.Length; i++)
            {
                bool lastCellInRow = (i + 1) % PostflopHandsGroup.HandCategoriesCount == 0;

                if (lastCellInRow)
                {
                    evLowBound = PostflopHandsGroup.NutHandsEvThreshold;
                    evUpperBound = double.MaxValue;
                }
                else
                {
                    evLowBound = cellXOffset * evPerCell;
                    evUpperBound = evLowBound + evPerCell;
                }

                int row = i / PostflopHandsGroup.HandCategoriesCount;

                if (_handGroupRanges[Round - 2][row][1] < evLowBound || _handGroupRanges[Round - 2][row][0] >= evUpperBound)
                    drawingContext.DrawRectangle(_outOfRangeBackgroundColor, null, new Rect(xIndent, yIndent, cellWidth, cellHeight));

                if (handsGroup.Hands[i] > 0)
                {
                    SolidColorBrush cellBrush = handsGroup.Hands[i] > _postflopHandsTableBackgroundColors.Length ? _postflopHandsTableBackgroundColors[^1] : _postflopHandsTableBackgroundColors[handsGroup.Hands[i] - 1];

                    drawingContext.DrawRectangle(cellBrush, null, new Rect(xIndent, yIndent, cellWidth, cellHeight));
                }

                xIndent += cellWidth;

                cellXOffset++;

                if (lastCellInRow)
                {
                    xIndent = CounterActionsWidth + groupNameWidth;

                    yIndent += cellHeight;

                    cellXOffset = 0;
                }
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

            for (int i = -1; i < PostflopHandsGroup.HandCategoriesCount - 1; i++)
            {
                xIndent = CounterActionsWidth + groupNameWidth + (i + 1) * cellWidth;
                
                drawingContext.DrawLine(_defaultPen, new Point(xIndent, headerHeight + infoHeight), new Point(xIndent, RenderSize.Height));
            }

            for (int i = 0; i < rows - 1; i++)
            {
                yIndent = headerHeight + infoHeight + (i + 1) * cellHeight;

                drawingContext.DrawLine(i == 11 ? _defaultGroupDelimiterPen : _defaultPen, new Point(CounterActionsWidth, yIndent), new Point(RenderSize.Width, yIndent));
            }
        }
    }
}
