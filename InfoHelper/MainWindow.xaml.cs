using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using InfoHelper.Utils;
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

            DataContext = _win;

            new Controller(_win);
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            //PostflopHudEntity hpe = (PostflopHudEntity)grid1.PostflopPanel.DataContext;

            //hpe.IsHiddenRow = !hpe.IsHiddenRow;

            //hpe.UpdateBindings();
        }

        private void ButtonBase_OnClick1(object sender, RoutedEventArgs e)
        {
            ViewModelActionsState vmas = _win.HudsParentStates[0].ActionsState;

            vmas.Actions = @"xr/rcfr/bbrr";

            vmas.Visible = !vmas.Visible;

            vmas.UpdateBindings();

            ViewModelNameState vmns = _win.HudsParentStates[0].NameState;

            vmns.Name = "akaWild!!!";

            vmns.Visible = true;

            vmns.IsConfirmed = !vmns.IsConfirmed;

            vmns.UpdateBindings();

            #region General

            ViewModelStatsHud vmgh = _win.HudsParentStates[0].GeneralHudState;

            ValueCell handsCell = new ValueCell("Hands", "Hands played");

            handsCell.IncrementSample();

            StatsCell vpipCell = new StatsCell("Vpip", "Vpip");

            vpipCell.IncrementSample();
            vpipCell.IncrementSample();
            vpipCell.IncrementSample();
            vpipCell.IncrementValue();

            EvCell evCell = new EvCell("EvBb", "EvBb");

            evCell.IncrementSample();
            evCell.IncrementSample();
            evCell.IncrementValue(50);

            vmgh.Visible = true;

            vmgh.SetData(new DataCell[] {handsCell, vpipCell, evCell});

            vmgh.UpdateBindings();

            #endregion

            #region Preflop

            ViewModelStatsHud preflopBtn = _win.HudsParentStates[0].BtnPreflopHudState;

            StatsCell sc1 = new StatsCell("Btn_4bet_Fold", "BTN fold unopened pot");
            sc1.IncrementSample();
            sc1.IncrementSample();
            sc1.IncrementValue();

            preflopBtn.Visible = true;

            preflopBtn.SetData(new DataCell[] {sc1});
            preflopBtn.SetRows(new string[] { "" });

            preflopBtn.UpdateBindings();

            #endregion
            ViewModelStatsHud hpe = _win.HudsParentStates[0].PostflopFlopHudState;

            ViewModelPreflopMatrixState vmpms = _win.HudsParentStates[0].PreflopMatrixState;

            PreflopData cbData = new PreflopData();

            cbData.AddHand("9c", "Jd");

            cbData.AddHand("As", "2s");
            cbData.AddHand("As", "2s");

            cbData.AddHand("6c", "6h");
            cbData.AddHand("6c", "6h");
            cbData.AddHand("6c", "6h");

            cbData.AddHand("2s", "4s");
            cbData.AddHand("2s", "4s");
            cbData.AddHand("2s", "4s");
            cbData.AddHand("2s", "4s");

            cbData.AddHand("5h", "Qh");
            cbData.AddHand("5h", "Qh");
            cbData.AddHand("5h", "Qh");
            cbData.AddHand("5h", "Qh");
            cbData.AddHand("5h", "Qh");

            cbData.AddHand("7h", "6d");
            cbData.AddHand("7h", "6d");
            cbData.AddHand("7h", "6d");
            cbData.AddHand("7h", "6d");
            cbData.AddHand("7h", "6d");
            cbData.AddHand("7h", "6d");

            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");
            cbData.AddHand("Th", "Tc");

            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");
            cbData.AddHand("Qh", "Qc");

            vmpms.Header = "";
            vmpms.PreflopData = cbData;
            vmpms.Visible = !vmpms.Visible;

            vmpms.UpdateBindings();

            PreflopData cxData = new PreflopData();

            cxData.AddHand("Kc", "Kd");

            cxData.AddHand("5s", "8s");
            cxData.AddHand("5s", "8s");

            StatsCell cxCell = new StatsCell("CX_F", "Continuation check flop") { CellData = cxData };

            StatsCell cbCell = new StatsCell("CB_F", "Continuation bet flop") { CellData = cbData, ConnectedCells = new StatsCell[] {cxCell}};

            Random rnd = new Random();

            int iterations = 4400;

            for (int i = 0; i < iterations; i++)
            {
                if (rnd.Next(0, 2) == 1)
                    cbCell.IncrementValue();

                cbCell.IncrementSample();
            }

            hpe.SetData(new DataCell[] { cbCell });

            hpe.SetRows(new string[] { "IsHiddenRow" });

            hpe.Visible = !hpe.Visible;

            hpe.UpdateBindings();


            //Worker worker = new Worker(_win);

            //worker.DoWork();


            ViewModelWindowsInfoState vmwip = (ViewModelWindowsInfoState)_win.WindowsInfoState;

            Application.Current.Resources["ClientScreenWidth"] = 1920;
            Application.Current.Resources["ClientScreenHeight"] = 1040;

            WindowInfo[] rects = new WindowInfo[6]
            {
                new WindowInfo(new Rect(0, 0, 714, 520), ViewModel.DataEntities.WindowState.OkFront, true),
                new WindowInfo(new Rect(699, 0, 714, 520), ViewModel.DataEntities.WindowState.OkBack, false),
                new WindowInfo(new Rect(1418, 0, 714, 520), ViewModel.DataEntities.WindowState.WrongCaptionFront, false),
                new WindowInfo(new Rect(0, 500, 714, 520), ViewModel.DataEntities.WindowState.WrongCaptionBack, false),
                new WindowInfo(new Rect(699, 500, 714, 520), ViewModel.DataEntities.WindowState.ErrorFront, true),
                new WindowInfo(new Rect(1418, 500, 714, 520), ViewModel.DataEntities.WindowState.ErrorBack, true)
            };

            vmwip.WinInfos = rects;

            vmwip.UpdateBindings();
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
