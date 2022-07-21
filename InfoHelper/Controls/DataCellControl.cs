using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    ///     <MyNamespace:DataCellCustomControl/>
    ///
    /// </summary>
    public class DataCellControl : CellControl
    {
        protected static Popup Popup { get; }

        static DataCellControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataCellControl), new FrameworkPropertyMetadata(typeof(DataCellControl)));

            Popup = new Popup
            {
                StaysOpen = false,
                Child = new Border() { BorderBrush = Brushes.Black, BorderThickness = new Thickness(1) },
                Width = 300,
                Height = 300
            };
        }

        public DataCell Data
        {
            get => (DataCell)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(DataCell), typeof(DataCellControl), new PropertyMetadata(null));

        public int Precision
        {
            get => (int)GetValue(PrecisionProperty);
            set => SetValue(PrecisionProperty, value);
        }

        public static readonly DependencyProperty PrecisionProperty =
            DependencyProperty.Register("Precision", typeof(int), typeof(DataCellControl), new PropertyMetadata(0));

        public bool ShowSample
        {
            get => (bool)GetValue(ShowSampleProperty);
            set => SetValue(ShowSampleProperty, value);
        }

        public static readonly DependencyProperty ShowSampleProperty =
            DependencyProperty.Register("ShowSample", typeof(bool), typeof(DataCellControl), new PropertyMetadata(true));

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            Border popupContent = (Border)Popup.Child;

            popupContent.Child = new Canvas(){Background = Brushes.Coral};

            Popup.Placement = PlacementMode.Mouse;
            Popup.IsOpen = true;
        }
    }
}
