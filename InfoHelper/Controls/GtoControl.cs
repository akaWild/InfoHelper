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
using GtoUtility;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
using Microsoft.Win32;

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
    ///     <MyNamespace:GtoParentPanel/>
    ///
    /// </summary>
    public class GtoControl : BaseDataControl
    {
        private bool _squareSizeProportionalToWeight = false;
        private bool _normalizeSquares = false;

        private Pen _borderPen;
        private Pen _selectedHandBorderPen;
        private SolidColorBrush _headerForegroundBrush;
        private SolidColorBrush _headerBackgroundBrush;
        private SolidColorBrush _foregroundBrush;
        private SolidColorBrush _foldBrush;
        private SolidColorBrush _checkOrCallBrush;
        private SolidColorBrush[] _raiseBrushes;
        private SolidColorBrush[] _diffsBrushes;

        private readonly Typeface _typeFace = new Typeface("Times New Roman");

        private const double HeaderHeight = 0.07;
        private const double BodyHeight = 0.7;
        private const double LegendHeight = 0.16;
        private const double DiffsHeight = 0.07;

        static GtoControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GtoControl), new FrameworkPropertyMetadata(typeof(GtoControl)));
        }

        public GtoControl()
        {
            RegistryKey regKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\InfoHelper6Max\\GtoPanel");

            _squareSizeProportionalToWeight = (int)(regKey?.GetValue("SquareSizeProportionalToWeight") ?? 0) == 1;

            _normalizeSquares = (int)(regKey?.GetValue("NormalizeSquares") ?? 0) == 1;

            regKey?.Close();

            ContextMenu contextMenu = new ContextMenu();

            MenuItem propMi = new MenuItem()
            {
                Name = "tsSquareSizeProportionalToWeight", 
                Header = "Square size proportional to weight", 
                IsChecked = _squareSizeProportionalToWeight
            };

            propMi.Click += PropMi_Click;

            MenuItem normMi = new MenuItem()
            {
                Name = "tsNormalizeSquares",
                Header = "Normalize squares",
                IsChecked = _normalizeSquares
            };

            normMi.Click += PropMi_Click;

            contextMenu.Items.Add(propMi);
            contextMenu.Items.Add(normMi);

            ContextMenu = contextMenu;
        }

        private void PropMi_Click(object sender, RoutedEventArgs e)
        {
            MenuItem selectedMenuItem = (MenuItem)sender;

            if (selectedMenuItem.Name == "tsSquareSizeProportionalToWeight")
            {
                _squareSizeProportionalToWeight = !_squareSizeProportionalToWeight;

                RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\InfoHelper6Max\\GtoPanel");

                regKey?.SetValue("SquareSizeProportionalToWeight", _squareSizeProportionalToWeight ? 1 : 0);

                regKey?.Close();
            }

            if (selectedMenuItem.Name == "tsNormalizeSquares")
            {
                _normalizeSquares = !_normalizeSquares;

                RegistryKey regKey = Registry.CurrentUser.CreateSubKey("SOFTWARE\\InfoHelper6Max\\GtoPanel");

                regKey?.SetValue("NormalizeSquares", _normalizeSquares ? 1 : 0);

                regKey?.Close();
            }

            selectedMenuItem.IsChecked = !selectedMenuItem.IsChecked;

            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if (Data == null)
                return;

            VisualEdgeMode = EdgeMode.Aliased;

            GtoRenderInfo renderInfo = null;

            if (Data is PreflopGtoInfo preflopGtoData)
                renderInfo = GetRenderInfoPreflop(preflopGtoData);

            if(renderInfo == null)
                return;

            foreach (object drawItem in renderInfo.DrawItems)
            {
                if(drawItem is DrawRect dr)
                    drawingContext.DrawRectangle(dr.Brush, dr.Pen, dr.Rect);
                else if(drawItem is DrawText dt)
                    drawingContext.DrawText(dt.Text, dt.Origin);
                else if(drawItem is DrawLine dl)
                    drawingContext.DrawLine(dl.Pen, dl.Point0, dl.Point1);
            }
        }

        private GtoRenderInfo GetRenderInfoPreflop(PreflopGtoInfo preflopGtoData)
        {
            _borderPen ??= (Pen)Application.Current.TryFindResource("GtoBorderPen");
            _selectedHandBorderPen ??= (Pen)Application.Current.TryFindResource("GtoSelectedHandBorderPen");
            _headerForegroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("GtoHeaderForegroundBrush");
            _headerBackgroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("GtoHeaderBackgroundBrush");
            _foregroundBrush ??= (SolidColorBrush)Application.Current.TryFindResource("GtoForegroundBrush");
            _foldBrush ??= (SolidColorBrush)Application.Current.TryFindResource("GtoFoldBrush");
            _checkOrCallBrush ??= (SolidColorBrush)Application.Current.TryFindResource("GtoCheckCallBrush");
            _raiseBrushes = (SolidColorBrush[])Application.Current.TryFindResource("GtoRaiseBrushes");
            _diffsBrushes = (SolidColorBrush[])Application.Current.TryFindResource("GtoDiffBrushes");

            GtoRenderInfo renderInfo = new GtoRenderInfo();

            //Header
            double headerHeight = RenderSize.Height * HeaderHeight;

            renderInfo.Add(new DrawRect(new Rect(new Point(0, 0), new Size(RenderSize.Width, headerHeight)), _headerBackgroundBrush, null));

            FormattedText headerText = new FormattedText(preflopGtoData.Title, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, headerHeight - 6, _headerForegroundBrush, 1);

            Point headerTextLocation = new Point(RenderSize.Width / 2 - headerText.Width / 2, headerHeight / 2 - headerText.Height / 2);

            renderInfo.Add(new DrawText(headerText, headerTextLocation));

            //Body
            double bodyHeight = RenderSize.Height * BodyHeight;

            double cellWidth = RenderSize.Width / 13;
            double cellHeight = bodyHeight / 13F;

            Rect selectedHandRect = default;

            float normCoeff = 1;

            if (_normalizeSquares)
            {
                float maxAbsValue = preflopGtoData.GtoStrategyContainer.Values.Max(v => v[0].Abs / 100);

                normCoeff = maxAbsValue == 0 ? normCoeff : 1 / maxAbsValue;
            }

            GtoStrategy[] strategies = null;

            double stratXIndent = 0;

            int raisesCounter = 0;

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    string pocket;

                    if (j > i)
                        pocket = $"{Common.FaceValuesRevert[i]}{Common.FaceValuesRevert[j]}s";
                    else if (j < i)
                        pocket = $"{Common.FaceValuesRevert[j]}{Common.FaceValuesRevert[i]}o";
                    else
                        pocket = $"{Common.FaceValuesRevert[i]}{Common.FaceValuesRevert[j]}";

                    Rect cellRect = new Rect(cellWidth * j, headerHeight + cellHeight * i, cellWidth, cellHeight);

                    if (pocket == preflopGtoData.Pocket)
                        selectedHandRect = cellRect;

                    if (preflopGtoData.GtoStrategyContainer[pocket][0].Abs > 0)
                    {
                        double fillHeight = cellHeight * (_squareSizeProportionalToWeight ? preflopGtoData.GtoStrategyContainer[pocket][0].Abs / 100 * normCoeff : 1);

                        strategies = preflopGtoData.GtoStrategyContainer[pocket];

                        stratXIndent = 0;

                        raisesCounter = 0;

                        foreach (GtoStrategy strategy in strategies)
                        {
                            if (strategy.ActionInfo.Action == GtoAction.Raise)
                                raisesCounter++;

                            double stratWidth = cellWidth * strategy.Percent;

                            Rect fiilRect = new Rect(stratXIndent + cellRect.X, cellRect.Y + cellHeight - fillHeight, stratWidth, fillHeight);

                            renderInfo.Add(new DrawRect(fiilRect, GetActionBrush(strategy.ActionInfo.Action, raisesCounter), null));

                            stratXIndent += stratWidth;
                        }
                    }

                    FormattedText text = new FormattedText(pocket, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, cellHeight - 8, _foregroundBrush, 1);

                    Point textLocation = new Point(cellRect.X + cellWidth / 2 - text.Width / 2, cellRect.Y + cellHeight / 2 - text.Height / 2);

                    renderInfo.Add(new DrawText(text, textLocation));
                }
            }

            //Legend
            double legendHeight = RenderSize.Height * LegendHeight / 2;
            double legendWidth = RenderSize.Width * 0.2;

            strategies = preflopGtoData.GtoStrategyContainer[preflopGtoData.Pocket];

            raisesCounter = 0;

            for (int i = 0; i < strategies.Length; i++)
            {
                if (strategies[i].ActionInfo.Action == GtoAction.Raise)
                    raisesCounter++;

                Rect legendRect = default;

                if (i <= 5)
                {
                    (int column, int row) = GetStrategyCellPosition(i);

                    legendRect = new Rect(legendWidth * column, headerHeight + bodyHeight + legendHeight * row, legendWidth, legendHeight);
                }

                double xIndent = 3;

                double squareSide = legendHeight * 0.4;

                Rect squareRect = new Rect(legendRect.X + xIndent, legendRect.Y + legendRect.Height / 2 - squareSide / 2, squareSide, squareSide);

                renderInfo.Add(new DrawRect(squareRect, GetActionBrush(strategies[i].ActionInfo.Action, raisesCounter), _borderPen));

                xIndent += squareRect.Width + 3;

                string outputText = $"{ConvertAction(strategies[i].ActionInfo.Action)}";

                if (strategies[i].ActionInfo.Amount > 0)
                    outputText += $" {strategies[i].ActionInfo.Amount.ToString(CultureInfo.InvariantCulture)}";

                if(strategies[i].Abs > 0)
                    outputText += $" ({Math.Round(strategies[i].Ev, 2).ToString(CultureInfo.InvariantCulture)})";

                FormattedText text = new FormattedText(outputText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, legendHeight - 16, _foregroundBrush, 1);

                Point textLocation = new Point(legendRect.X + xIndent, legendRect.Y + legendRect.Height / 2 - text.Height / 2);

                renderInfo.Add(new DrawText(text, textLocation));
            }

            //Legend hand
            double legendHandHeight = RenderSize.Height * LegendHeight;
            double legendHandWidth = RenderSize.Width * 0.4;

            Rect legendHandRect = new Rect(RenderSize.Width - legendHandWidth, headerHeight + bodyHeight, legendHandWidth, legendHandHeight);

            if (preflopGtoData.GtoStrategyContainer[preflopGtoData.Pocket][0].Abs > 0)
            {
                stratXIndent = 0;

                raisesCounter = 0;

                foreach (GtoStrategy strategy in strategies)
                {
                    if (strategy.ActionInfo.Action == GtoAction.Raise)
                        raisesCounter++;

                    double stratWidth = legendHandWidth * strategy.Percent;

                    Rect fiilRect = new Rect(stratXIndent + legendHandRect.X, legendHandRect.Y, stratWidth, legendHandHeight);

                    renderInfo.Add(new DrawRect(fiilRect, GetActionBrush(strategy.ActionInfo.Action, raisesCounter), null));

                    stratXIndent += stratWidth;
                }
            }

            FormattedText legendHandText = new FormattedText(preflopGtoData.Pocket, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, legendHandHeight - 22, _foregroundBrush, 1);

            Point legendHandTextLocation = new Point(legendHandRect.X + legendHandWidth / 2 - legendHandText.Width / 2, legendHandRect.Y + legendHandRect.Height / 2 - legendHandText.Height / 2);

            renderInfo.Add(new DrawText(legendHandText, legendHandTextLocation));

            //Diffs
            double diffsHeight = RenderSize.Height * DiffsHeight;
            double diffsWidth = RenderSize.Width / 2;

            Rect bbDiffRect = new Rect(0, headerHeight + bodyHeight + legendHandHeight, diffsWidth, diffsHeight);

            renderInfo.Add(new DrawRect(bbDiffRect, GetDiffsBrush(preflopGtoData.GtoDiffs.BbDiffPercent), null));

            string bbDiffSign = preflopGtoData.GtoDiffs.BbDiff > 0 ? "+" : "";
            string bbDiffText = $"ΔBB: {bbDiffSign}{Math.Round(preflopGtoData.GtoDiffs.BbDiff, 2).ToString(CultureInfo.InvariantCulture)} ({bbDiffSign}{Math.Round(preflopGtoData.GtoDiffs.BbDiffPercent, 2).ToString(CultureInfo.InvariantCulture)}%)";

            FormattedText bbDiffsText = new FormattedText(bbDiffText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, diffsHeight - 6, _foregroundBrush, 1);

            Point bbDiffsTextLocation = new Point(bbDiffRect.X + bbDiffRect.Width / 2 - bbDiffsText.Width / 2, bbDiffRect.Y + bbDiffRect.Height / 2 - bbDiffsText.Height / 2);

            renderInfo.Add(new DrawText(bbDiffsText, bbDiffsTextLocation));

            Rect amtDiffRect = new Rect(diffsWidth, headerHeight + bodyHeight + legendHandHeight, diffsWidth, diffsHeight);

            renderInfo.Add(new DrawRect(amtDiffRect, GetDiffsBrush(preflopGtoData.GtoDiffs.AmountDiffPercent), null));

            string amtDiffSign = preflopGtoData.GtoDiffs.AmountDiff > 0 ? "+" : "";
            string amtDiffText = $"P/O: {amtDiffSign}{Math.Round(preflopGtoData.GtoDiffs.AmountDiff, 2).ToString(CultureInfo.InvariantCulture)} ({amtDiffSign}{Math.Round(preflopGtoData.GtoDiffs.AmountDiffPercent, 2).ToString(CultureInfo.InvariantCulture)}%)";

            FormattedText amtDiffsText = new FormattedText(amtDiffText, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, diffsHeight - 6, _foregroundBrush, 1);

            Point amtDiffsTextLocation = new Point(amtDiffRect.X + amtDiffRect.Width / 2 - amtDiffsText.Width / 2, amtDiffRect.Y + amtDiffRect.Height / 2 - amtDiffsText.Height / 2);

            renderInfo.Add(new DrawText(amtDiffsText, amtDiffsTextLocation));

            //Draw lines and rectangles
            renderInfo.Add(new DrawLine(_borderPen, new Point(0, headerHeight), new Point(RenderSize.Width, headerHeight)));

            for (int i = 0; i < 13; i++)
            {
                for (int j = 0; j < 13; j++)
                {
                    if (j == 12)
                        renderInfo.Add(new DrawLine(_borderPen, new Point(0, headerHeight + cellHeight * i + cellHeight), new Point(RenderSize.Width, headerHeight + cellHeight * i + cellHeight)));
                    else
                    {
                        if (i == 12)
                            renderInfo.Add(new DrawLine(_borderPen, new Point(cellWidth * j + cellWidth, headerHeight), new Point(cellWidth * j + cellWidth, headerHeight + bodyHeight)));
                    }
                }
            }

            if (selectedHandRect != default)
                renderInfo.Add(new DrawRect(selectedHandRect, null, _selectedHandBorderPen));

            renderInfo.Add(new DrawLine(_borderPen, new Point(0, headerHeight + bodyHeight + legendHeight), new Point(legendWidth * 3, headerHeight + bodyHeight + legendHeight)));
            renderInfo.Add(new DrawLine(_borderPen, new Point(0, headerHeight + bodyHeight + legendHandHeight), new Point(RenderSize.Width, headerHeight + bodyHeight + legendHandHeight)));
            renderInfo.Add(new DrawLine(_borderPen, new Point(legendWidth, headerHeight + bodyHeight), new Point(legendWidth, headerHeight + bodyHeight + legendHandHeight)));
            renderInfo.Add(new DrawLine(_borderPen, new Point(legendWidth * 2, headerHeight + bodyHeight), new Point(legendWidth * 2, headerHeight + bodyHeight + legendHandHeight)));
            renderInfo.Add(new DrawLine(_borderPen, new Point(legendWidth * 3, headerHeight + bodyHeight), new Point(legendWidth * 3, headerHeight + bodyHeight + legendHandHeight)));

            renderInfo.Add(new DrawLine(_borderPen, new Point(diffsWidth, headerHeight + bodyHeight + legendHandHeight), new Point(diffsWidth, headerHeight + bodyHeight + legendHandHeight + diffsHeight)));

            return renderInfo;
        }

        private SolidColorBrush GetActionBrush(GtoAction action, int raisesCounter)
        {
            return (action, raisesCounter) switch
            {
                (GtoAction.Fold, _) => _foldBrush,
                (GtoAction.Check or GtoAction.Call, _) => _checkOrCallBrush,
                (GtoAction.Raise, 1) => _raiseBrushes[0],
                (GtoAction.Raise, 2) => _raiseBrushes[1],
                (GtoAction.Raise, 3) => _raiseBrushes[2],
                _ => null
            };
        }

        private SolidColorBrush GetDiffsBrush(float amount)
        {
            return amount switch
            {
                > 0 and <= 10 => _diffsBrushes[0],
                > 10 and <= 20 => _diffsBrushes[1],
                > 20 and <= 30 => _diffsBrushes[2],
                > 30 => _diffsBrushes[3],
                < -30 => _diffsBrushes[4],
                >= -30 and < -20 => _diffsBrushes[5],
                >= -20 and < -10 => _diffsBrushes[6],
                >= -10 and < 0 => _diffsBrushes[7],
                _ => null
            };
        }

        private (int, int) GetStrategyCellPosition(int index)
        {
            int row = index <= 2 ? 0 : 1;

            if (index > 2)
                index -= 3;

            return (index, row);
        }

        private string ConvertAction(GtoAction action)
        {
            return (action) switch
            {
                GtoAction.Fold => "F",
                GtoAction.Check => "X",
                GtoAction.Call => "C",
                GtoAction.Raise => "R",
                _ => null
            };
        }

        #region Private classes

        private class GtoRenderInfo
        {
            private List<object> _drawItems = new List<object>();

            public object[] DrawItems => _drawItems.ToArray();

            public void Add(object item)
            {
                _drawItems.Add(item);
            }
        }

        private class DrawRect
        {
            public Rect Rect { get; }

            public SolidColorBrush Brush { get; }

            public Pen Pen { get; }

            public DrawRect(Rect rect, SolidColorBrush brush, Pen pen)
            {
                Rect = rect;
                Brush = brush;
                Pen = pen;
            }
        }

        private class DrawText
        {
            public FormattedText Text { get; }

            public Point Origin { get; }

            public DrawText(FormattedText text, Point origin)
            {
                Text = text;
                Origin = origin;
            }
        }

        private class DrawLine
        {
            public Pen Pen { get; }

            public Point Point0 { get; }

            public Point Point1 { get; }

            public DrawLine(Pen pen, Point point0, Point point1)
            {
                Pen = pen;
                Point0 = point0;
                Point1 = point1;
            }
        }

        #endregion
    }
}
