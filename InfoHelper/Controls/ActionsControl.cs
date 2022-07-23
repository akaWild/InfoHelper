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
    ///     <MyNamespace:ActionsControl/>
    ///
    /// </summary>
    public class ActionsControl : BaseControl
    {
        private readonly Typeface _typeFace = new Typeface("Verdana");

        static ActionsControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ActionsControl), new FrameworkPropertyMetadata(typeof(ActionsControl)));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            Height = (double)Application.Current.Resources["CellHeight"];
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            SolidColorBrush defaultForegroundColor = (SolidColorBrush)Application.Current.TryFindResource("PreflopMatrixForegroundBrush");
            SolidColorBrush defaultBackgroundColor = (SolidColorBrush)Application.Current.TryFindResource("ActionsPanelBackgroundBrush");

            drawingContext.DrawRectangle(defaultBackgroundColor, null, new Rect(new Point(0, 0), new Size(RenderSize.Width, RenderSize.Height)));

            SolidColorBrush GetForegroundBrush(char symbol)
            {
                return symbol switch
                {
                    'f' => Brushes.Red,
                    'x' => Brushes.Orange,
                    'c' => Brushes.ForestGreen,
                    'b' => Brushes.DodgerBlue,
                    'r' => Brushes.DarkMagenta,
                    _ => defaultForegroundColor
                };
            }

            if (Data == null)
                return;

            string actions = (string)Data;

            double textIndent = 2;

            foreach (char symbol in actions)
            {
                FormattedText text = new FormattedText(symbol.ToString(), CultureInfo.InvariantCulture, FlowDirection.LeftToRight, _typeFace, RenderSize.Height, GetForegroundBrush(symbol), 1);

                Point textLocation = new Point(textIndent, RenderSize.Height / 2 - text.Height / 2);

                drawingContext.DrawText(text, textLocation);

                textIndent += text.Width;
            }
        }
    }
}
