﻿using System;
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

            if(Visibility != Visibility.Visible)
                return;

            PostflopHandsGroup handsGroup = (PostflopHandsGroup)Data;

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

                drawingContext.DrawRectangle(_headerBackgroundBrush, null, new Rect(new Point(CounterActionsWidth, 0), new Size(RenderSize.Width - CounterActionsWidth, headerHeight)));

                FormattedText text = new FormattedText(Header, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, headerHeight - 4, _headerForegroundBrush, 1);

                Point textLocation = new Point(CounterActionsWidth + (RenderSize.Width - CounterActionsWidth) / 2 - text.Width / 2, headerHeight / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                drawingContext.DrawLine(_pen, new Point(CounterActionsWidth, headerHeight), new Point(RenderSize.Width, headerHeight));
            }

            double groupGraphHeight = (RenderSize.Height - headerHeight) / 3;

            int max = handsGroup.MadeHands.Select((v, i) => v + handsGroup.DrawHands[i] + handsGroup.ComboHands[i]).Max();

            double columnWidth = RenderSize.Width / PostflopHandsGroup.HandCategoriesCount;

            double yIndent = headerHeight;

            RenderHandsGroup(handsGroup.MadeHands, handsGroup.MadeHandsDefaultValue, Brushes.DarkGreen);

            yIndent += groupGraphHeight;

            drawingContext.DrawLine(_pen, new Point(CounterActionsWidth, yIndent), new Point(RenderSize.Width, yIndent));

            RenderHandsGroup(handsGroup.ComboHands, handsGroup.ComboHandsDefaultValue, Brushes.Blue);

            yIndent += groupGraphHeight;

            drawingContext.DrawLine(_pen, new Point(CounterActionsWidth, yIndent), new Point(RenderSize.Width, yIndent));

            RenderHandsGroup(handsGroup.DrawHands, handsGroup.DrawHandsDefaultValue, Brushes.Red);

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
                drawingContext.DrawRectangle(Brushes.DarkGray, null, new Rect(new Point(0, 0), new Size(CounterActionsWidth, RenderSize.Height)));

            drawingContext.DrawLine(_pen, new Point(CounterActionsWidth, 0), new Point(CounterActionsWidth, RenderSize.Height));

            void RenderHandsGroup(ushort[] hands, float defaultValue, SolidColorBrush brush)
            {
                double xIndent = CounterActionsWidth;

                float handsSum = 0;

                for (int i = 0; i < hands.Length; i++)
                {
                    int handGroupsCount = hands[i];

                    double columnHeightRatio = (double)handGroupsCount / max;

                    double columnHeight = groupGraphHeight * columnHeightRatio;

                    double columnYIndent = yIndent + (1 - columnHeightRatio) * groupGraphHeight;

                    if (handGroupsCount > 0)
                    {
                        Rect rect = new Rect(xIndent, columnYIndent, columnWidth, columnHeight);

                        drawingContext.DrawRectangle(brush, null, rect);
                    }

                    handsSum += handGroupsCount;

                    xIndent += columnWidth;
                }

                float avgEq = hands.Select((h, i) => handsSum == 0 ? 0 : h * (i + 1) / handsSum).Sum();

                string groupText = $"Avg eq: {(avgEq == 0 ? "--" : Math.Round(avgEq, 1).ToString(CultureInfo.InvariantCulture))}%";

                if (avgEq > 0 && !float.IsNaN(defaultValue))
                {
                    string sign = string.Empty;

                    if (avgEq > defaultValue)
                        sign = "+";
                    else if (avgEq < defaultValue)
                        sign = "-";

                    groupText += $" ({sign}{Math.Abs(Math.Round(avgEq - defaultValue, 1)).ToString(CultureInfo.InvariantCulture)})";
                }

                FormattedText formattedText = new FormattedText(groupText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, groupGraphHeight / 5, _foregroundColor, 1);

                Point textLocation = new Point(CounterActionsWidth + 3, yIndent);

                drawingContext.DrawText(formattedText, textLocation);
            }
        }
    }
}
