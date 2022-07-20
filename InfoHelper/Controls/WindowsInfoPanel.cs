using System;
using System.Collections.Generic;
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
using InfoHelper.ViewModel.DataEntities;
using WindowState = InfoHelper.ViewModel.DataEntities.WindowState;

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
    ///     <MyNamespace:WindowsInfoPanel/>
    ///
    /// </summary>
    public class WindowsInfoPanel : Control
    {
        static WindowsInfoPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WindowsInfoPanel), new FrameworkPropertyMetadata(typeof(WindowsInfoPanel)));
        }

        public WindowInfo[] Data
        {
            get => (WindowInfo[])GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(WindowInfo[]), typeof(WindowsInfoPanel), new PropertyMetadata(null));

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if(e.Property == DataProperty)
                InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            if(Data == null)
                return;

            object GetBackgroundColor (WindowState winState)
            {
                return winState switch
                {
                    WindowState.OkFront => Application.Current.TryFindResource("OkFrontWindowBackgroundColor"),
                    WindowState.OkBack => Application.Current.TryFindResource("OkBackWindowBackgroundColor"),
                    WindowState.WrongCaption => Application.Current.TryFindResource("WrongCaptionWindowBackgroundColor"),
                    WindowState.Error => Application.Current.TryFindResource("ErrorWindowBackgroundColor"),
                    _ => null
                };
            }

            for (int i = 0; i < Data.Length; i++)
            {
                object colorResx = GetBackgroundColor(Data[i].WindowState);

                Color color;
                if (colorResx != null)
                    color = (Color)colorResx;

                drawingContext.DrawRectangle(new SolidColorBrush(color), new Pen(Brushes.Black, 1), Data[i].Rectangle);
            }
        }
    }
}
