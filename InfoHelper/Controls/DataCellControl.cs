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
        private const int PreflopPopupOrdinaryWidth = 500;
        private const int PreflopPopupHeight = 250;

        protected static Popup PreflopPopPopup { get; }

        static DataCellControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataCellControl), new FrameworkPropertyMetadata(typeof(DataCellControl)));

            Grid preflopGrid = new Grid();

            preflopGrid.ColumnDefinitions.Add(new ColumnDefinition() {Width = new GridLength(1, GridUnitType.Star)});
            preflopGrid.ColumnDefinitions.Add(new ColumnDefinition() {Width = new GridLength(1, GridUnitType.Star)});

            PreflopMatrixControl[] preflopMatrices = new PreflopMatrixControl[2] {new PreflopMatrixControl(), new PreflopMatrixControl()};

            Grid.SetColumn(preflopMatrices[0], 0);
            Grid.SetColumn(preflopMatrices[1], 1);

            preflopGrid.Children.Add(preflopMatrices[0]);
            preflopGrid.Children.Add(preflopMatrices[1]);

            PreflopPopPopup = new Popup
            {
                Child = preflopGrid,
                StaysOpen = false,
                Height = PreflopPopupHeight
            };

            PreflopPopPopup.MouseLeftButtonUp += PreflopPopPopup_MouseLeftButtonUp;
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

            if(Data == null)
                return;

            List<DataCell> dataCells = new List<DataCell>();

            if(Data.CellData != null)
                dataCells.Add(Data);

            if(Data.ConnectedCells != null)
                dataCells.AddRange(Data.ConnectedCells);

            if (dataCells.Count == 0)
                return;

            if (dataCells.Count > 2)
                throw new Exception("Too many data cells provided for popup");

            if(dataCells[0].CellData is PreflopData)
                ProcessPreflopData(dataCells.ToArray());
        }

        private void ProcessPreflopData(DataCell[] preflopDataCells)
        {
            Grid preflopGrid = (Grid)PreflopPopPopup.Child;

            for (int i = 0; i < preflopDataCells.Length; i++)
            {
                PreflopMatrixControl preflopMatrixControl = preflopGrid.Children.Cast<PreflopMatrixControl>().First(c => Grid.GetColumn(c) == i);

                preflopMatrixControl.Visibility = Visibility.Visible;

                preflopMatrixControl.Data = (PreflopData)preflopDataCells[i].CellData;
                preflopMatrixControl.Header = preflopDataCells[i].Description;
            }

            if(preflopDataCells.Length == 1)
                preflopGrid.Children.Cast<PreflopMatrixControl>().First(c => Grid.GetColumn(c) == 1).Visibility = Visibility.Hidden;

            PreflopPopPopup.Width = PreflopPopupOrdinaryWidth * preflopDataCells.Length;

            preflopGrid.ColumnDefinitions[1].Width = new GridLength(preflopDataCells.Length - 1, GridUnitType.Star);

            PreflopPopPopup.Placement = PlacementMode.Mouse;
            PreflopPopPopup.IsOpen = true;
        }

        private static void PreflopPopPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PreflopPopPopup.IsOpen = false;
        }
    }
}
