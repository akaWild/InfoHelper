using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
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
        private readonly ViewModelMain _win;

        public MainWindow()
        {
            InitializeComponent();

            _win = new ViewModelMain(Dispatcher);

            DataContext = _win.ControlsState;

            grid1.PostflopPanel.DataContext = _win.HudsParentStates[0].AggressorIpPostflopHudState;

            winInfo.DataContext = _win.WindowsInfoState;

            FlushPicturesProgressBar.DataContext = _win.ControlsState.ProgressBarState;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //PostflopHudEntity hpe = (PostflopHudEntity)grid1.PostflopPanel.DataContext;

            //hpe.IsHiddenRow = !hpe.IsHiddenRow;

            //hpe.UpdateBindings();
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            ViewModelStatsHud hpe = (ViewModelStatsHud)grid1.PostflopPanel.DataContext;

            StatsCell dc = new StatsCell("CB_F", string.Empty);

            Random rnd = new Random();

            int iterations = 4400;

            for (int i = 0; i < iterations; i++)
            {
                if(rnd.Next(0, 2) == 1)
                    dc.IncrementValue();

                dc.IncrementSample();
            }

            hpe.SetData(new DataCell[] { dc });

            hpe.SetRows(new string[] { "IsHiddenRow" });

            hpe.UpdateBindings();


            //Worker worker = new Worker(_win);

            //worker.DoWork();


            //ViewModelWindowsInfoState vmwip = (ViewModelWindowsInfoState)winInfo.DataContext;

            //Application.Current.Resources["ClientScreenWidth"] = 1920;
            //Application.Current.Resources["ClientScreenHeight"] = 1040;

            //WindowInfo[] rects = new WindowInfo[6]
            //{
            //    new WindowInfo(new Rect(0, 0, 714, 520), ViewModel.DataEntities.WindowState.OkFront, true),
            //    new WindowInfo(new Rect(699, 0, 714, 520), ViewModel.DataEntities.WindowState.OkBack, false),
            //    new WindowInfo(new Rect(1418, 0, 714, 520), ViewModel.DataEntities.WindowState.WrongCaptionFront, false),
            //    new WindowInfo(new Rect(0, 500, 714, 520), ViewModel.DataEntities.WindowState.WrongCaptionBack, false),
            //    new WindowInfo(new Rect(699, 500, 714, 520), ViewModel.DataEntities.WindowState.ErrorFront, true),
            //    new WindowInfo(new Rect(1418, 500, 714, 520), ViewModel.DataEntities.WindowState.ErrorBack, true)
            //};

            //vmwip.WinInfos = rects;

            //vmwip.UpdateBindings();
        }
    }

    public class Worker
    {
        private ViewModelMain _main;

        public Worker(ViewModelMain main)
        {
            _main = main;
        }

        public void DoWork()
        {
            ViewModelControlsState vmcs = _main.ControlsState;

            Thread thread = new Thread((() =>
            {

                vmcs.SetError(string.Empty);

                vmcs.BeginFlushingPictures();

                ViewModelProgressBarState vmpbs = vmcs.ProgressBarState;

                vmpbs.MinValue = 0;

                vmpbs.MaxValue = 100;

                vmpbs.Visible = true;

                for (int i = 0; i < vmpbs.MaxValue; i++)
                {
                    vmpbs.Value = i;

                    vmcs.SetError($"{Math.Round(i * 100d / vmpbs.MaxValue)}%", true);

                    Thread.Sleep(50);
                }

                vmpbs.Visible = false;

                vmcs.EndFlushingPictures();

                vmcs.SetError(string.Empty);
            }));

            thread.Start();
        }
    }
}
