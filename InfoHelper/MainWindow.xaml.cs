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
using InfoHelper.ViewModel;
using InfoHelper.ViewModel.DataEntities;
using InfoHelper.ViewModel.States;

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

            ViewModelMain win = new ViewModelMain();

            DataContext = win.ControlsState;

            grid1.PostflopPanel.DataContext = win.HudsParentStates[0].AggressorIpPostflopHudState;

            winInfo.DataContext = win.WindowsInfoState;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //PostflopHudEntity hpe = (PostflopHudEntity)grid1.PostflopPanel.DataContext;

            //hpe.IsHiddenRow = !hpe.IsHiddenRow;

            //hpe.UpdateBindings();
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            //ViewModelStatsHud hpe = (ViewModelStatsHud)grid1.PostflopPanel.DataContext;

            //StatsCell dc = new StatsCell("CB_F");

            //Random rnd = new Random();

            //dc.Mades = rnd.Next(0, 100);
            //dc.Attempts = rnd.Next(0, 100);

            //hpe.SetData(new DataCell[] { dc });

            //hpe.SetRows(new string[] { "IsHiddenRow" });

            //hpe.UpdateBindings();

            ViewModelWindowsInfoState vmwip = (ViewModelWindowsInfoState)winInfo.DataContext;

            WindowInfo[] rects = new WindowInfo[3]
            {
                new WindowInfo(new Rect(10, 10, 100, 100), ViewModel.DataEntities.WindowState.OkBack, false),
                new WindowInfo(new Rect(50, 50, 100, 100), ViewModel.DataEntities.WindowState.WrongCaption, false),
                new WindowInfo(new Rect(200, 200, 50, 50), ViewModel.DataEntities.WindowState.OkFront, false),
            };

            vmwip.WinInfos = rects;

            vmwip.UpdateBindings();
        }
    }
}
