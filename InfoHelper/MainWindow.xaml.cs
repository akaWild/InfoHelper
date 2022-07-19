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
using InfoHelper.StatsEntities;

namespace InfoHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            WindowEntity win = new WindowEntity();

            DataContext = win;

            grid1.PostflopPanel.DataContext = win[0].PostflopHud;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //PostflopHudEntity hpe = (PostflopHudEntity)grid1.PostflopPanel.DataContext;

            //hpe.IsHiddenRow = !hpe.IsHiddenRow;

            //hpe.UpdateBindings();
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            PostflopHudEntity hpe = (PostflopHudEntity)grid1.PostflopPanel.DataContext;

            StatsCell dc = new StatsCell("CB_F");

            Random rnd = new Random();

            dc.Mades = rnd.Next(0, 100);
            dc.Attempts = rnd.Next(0, 100);

            hpe.LoadData(new DataCell[] {dc});

            hpe.UpdateBindings();
        }
    }
}
