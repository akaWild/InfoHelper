using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
        private const int PreflopPopupWidth = 500;
        private const int PreflopPopupOrdinaryHeight = 250;

        private const int PostflopPopupWidth = 1000;
        private const int PostflopPopupOrdinaryHeight = 400;

        protected static Popup PreflopPopup { get; }
        protected static Popup PostflopPopup { get; }

        static DataCellControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DataCellControl), new FrameworkPropertyMetadata(typeof(DataCellControl)));

            //Preflop popup initialization
            Grid preflopGrid = new Grid();

            preflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star)});
            preflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star)});

            PreflopMatrixControl[] preflopMatrices = new PreflopMatrixControl[2] {new PreflopMatrixControl(), new PreflopMatrixControl()};

            Grid.SetRow(preflopMatrices[0], 0);
            Grid.SetRow(preflopMatrices[1], 1);

            preflopGrid.Children.Add(preflopMatrices[0]);
            preflopGrid.Children.Add(preflopMatrices[1]);

            PreflopPopup = new Popup
            {
                Child = preflopGrid,
                StaysOpen = false,
                Width = PreflopPopupWidth
            };

            PreflopPopup.MouseLeftButtonUp += PreflopPopup_MouseLeftButtonUp;

            //Postflop popup initialization
            Grid postflopGrid = new Grid();

            postflopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
            postflopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            postflopGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

            postflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            postflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            postflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
            postflopGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

            PostlopHandsTableControl[] postflopHandTables = new PostlopHandsTableControl[10]
            {
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl(),
                new PostlopHandsTableControl()
            };

            Grid.SetColumn(postflopHandTables[0], 0);
            Grid.SetColumn(postflopHandTables[1], 1);
            Grid.SetColumn(postflopHandTables[2], 2);
            Grid.SetColumn(postflopHandTables[3], 1);
            Grid.SetColumn(postflopHandTables[4], 2);
            Grid.SetColumn(postflopHandTables[5], 0);
            Grid.SetColumn(postflopHandTables[6], 1);
            Grid.SetColumn(postflopHandTables[7], 2);
            Grid.SetColumn(postflopHandTables[8], 1);
            Grid.SetColumn(postflopHandTables[9], 2);

            Grid.SetRow(postflopHandTables[0], 0);
            Grid.SetRow(postflopHandTables[1], 0);
            Grid.SetRow(postflopHandTables[2], 0);
            Grid.SetRow(postflopHandTables[3], 1);
            Grid.SetRow(postflopHandTables[4], 1);
            Grid.SetRow(postflopHandTables[5], 2);
            Grid.SetRow(postflopHandTables[6], 2);
            Grid.SetRow(postflopHandTables[7], 2);
            Grid.SetRow(postflopHandTables[8], 3);
            Grid.SetRow(postflopHandTables[9], 3);

            Grid.SetRowSpan(postflopHandTables[0], 2);
            Grid.SetRowSpan(postflopHandTables[5], 2);

            postflopGrid.Children.Add(postflopHandTables[0]);
            postflopGrid.Children.Add(postflopHandTables[1]);
            postflopGrid.Children.Add(postflopHandTables[2]);
            postflopGrid.Children.Add(postflopHandTables[3]);
            postflopGrid.Children.Add(postflopHandTables[4]);
            postflopGrid.Children.Add(postflopHandTables[5]);
            postflopGrid.Children.Add(postflopHandTables[6]);
            postflopGrid.Children.Add(postflopHandTables[7]);
            postflopGrid.Children.Add(postflopHandTables[8]);
            postflopGrid.Children.Add(postflopHandTables[9]);

            PostflopPopup = new Popup
            {
                Child = postflopGrid,
                StaysOpen = false,
                Width = PostflopPopupWidth
            };

            PostflopPopup.MouseLeftButtonUp += PostflopPopup_MouseLeftButtonUp;
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

            if (Data.CellData != null)
            {
                if(!Regex.IsMatch(Data.Name, @"Fold|FCB|Fv|_F_F|_F_T|_F_R"))
                    dataCells.Add(Data);

                if (Data.ConnectedCells != null)
                    dataCells.AddRange(Data.ConnectedCells.Where(c => !Regex.IsMatch(c.Name, @"Fold|FCB|Fv|_F_F|_F_T|_F_R")));
            }

            if (dataCells.Count == 0)
                return;

            if (dataCells.Count > 2)
                throw new Exception("Too many data cells provided for popup");

            if(dataCells[0].CellData is PreflopData)
                ProcessPreflopData(dataCells.ToArray());
            else if (dataCells[0].CellData is PostflopData)
                ProcessPostflopData(dataCells.ToArray());
        }

        private void ProcessPreflopData(DataCell[] preflopDataCells)
        {
            Grid preflopGrid = (Grid)PreflopPopup.Child;

            for (int i = 0; i < preflopDataCells.Length; i++)
            {
                PreflopMatrixControl preflopMatrixControl = preflopGrid.Children.Cast<PreflopMatrixControl>().First(c => Grid.GetRow(c) == i);

                preflopMatrixControl.Visibility = Visibility.Visible;

                preflopMatrixControl.Data = (PreflopData)preflopDataCells[i].CellData;
                preflopMatrixControl.Header = preflopDataCells[i].Description;
            }

            if(preflopDataCells.Length == 1)
                preflopGrid.Children.Cast<PreflopMatrixControl>().First(c => Grid.GetRow(c) == 1).Visibility = Visibility.Hidden;

            PreflopPopup.Height = PreflopPopupOrdinaryHeight * preflopDataCells.Length;

            preflopGrid.RowDefinitions[1].Height = new GridLength(preflopDataCells.Length - 1, GridUnitType.Star);

            PreflopPopup.Placement = PlacementMode.Mouse;
            PreflopPopup.IsOpen = true;
        }

        private void ProcessPostflopData(DataCell[] postflopDataCells)
        {
            Grid postflopGrid = (Grid)PostflopPopup.Child;

            PostlopHandsTableControl[] postflopControls = postflopGrid.Children.Cast<PostlopHandsTableControl>().ToArray();

            postflopControls[0].Data = ((PostflopData)postflopDataCells[0].CellData).MainGroup;
            postflopControls[0].Header = postflopDataCells[0].Description;

            BetRange[] betRange1 = postflopDataCells[0].BetRanges;

            if (betRange1 == null)
            {
                Grid.SetColumnSpan(postflopControls[0], 3);

                for (int i = 1; i < 5; i++)
                    postflopControls[i].Visibility = Visibility.Hidden;
            }
            else
            {
                Grid.SetColumnSpan(postflopControls[0], 1);

                for (int i = 0; i < 4; i++)
                {
                    if (i < betRange1.Length)
                    {
                        postflopControls[i + 1].Visibility = Visibility.Visible;

                        postflopControls[i + 1].Data = ((PostflopData)postflopDataCells[0].CellData).SubGroups[i];

                        string amountString = betRange1[i].UpperBound == ushort.MaxValue ? $">={betRange1[i].LowBound}" : $"{betRange1[i].LowBound}<=..<{betRange1[i].UpperBound}";

                        postflopControls[i + 1].Header = $"{amountString}";
                    }
                    else
                        postflopControls[i + 1].Visibility = Visibility.Hidden;
                }
            }

            if (postflopDataCells.Length == 1)
            {
                for (int i = 5; i < 10; i++)
                    postflopControls[i].Visibility = Visibility.Hidden;
            }
            else
            {
                postflopControls[5].Visibility = Visibility.Visible;

                postflopControls[5].Data = ((PostflopData)postflopDataCells[1].CellData).MainGroup;
                postflopControls[5].Header = postflopDataCells[1].Description;

                BetRange[] betRange2 = postflopDataCells[1].BetRanges;

                if (betRange2 == null)
                {
                    Grid.SetColumnSpan(postflopControls[5], 3);

                    for (int i = 6; i < 10; i++)
                        postflopControls[i].Visibility = Visibility.Hidden;
                }
                else
                {
                    Grid.SetColumnSpan(postflopControls[5], 1);

                    for (int i = 0; i < 4; i++)
                    {
                        if (i < betRange2.Length)
                        {
                            postflopControls[i + 6].Visibility = Visibility.Visible;

                            postflopControls[i + 6].Data = ((PostflopData)postflopDataCells[1].CellData).SubGroups[i];

                            string amountString = betRange2[i].UpperBound == ushort.MaxValue ? $">={betRange2[i].LowBound}" : $"{betRange2[i].LowBound}<=..<{betRange2[i].UpperBound}";

                            postflopControls[i + 6].Header = $"{amountString}";
                        }
                        else
                            postflopControls[i + 6].Visibility = Visibility.Hidden;
                    }
                }
            }

            PostflopPopup.Height = PostflopPopupOrdinaryHeight * postflopDataCells.Length;

            postflopGrid.RowDefinitions[2].Height = new GridLength(postflopDataCells.Length - 1, GridUnitType.Star);
            postflopGrid.RowDefinitions[3].Height = new GridLength(postflopDataCells.Length - 1, GridUnitType.Star);

            PostflopPopup.Placement = PlacementMode.Mouse;
            PostflopPopup.IsOpen = true;
        }

        private static void PreflopPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PreflopPopup.IsOpen = false;
        }

        private static void PostflopPopup_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            PostflopPopup.IsOpen = false;
        }
    }
}
