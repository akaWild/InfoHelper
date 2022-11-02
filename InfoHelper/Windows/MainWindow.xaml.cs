using System;
using System.Threading;
using System.Windows;
using InfoHelper.DataProcessor;
using InfoHelper.StatsEntities;
using InfoHelper.ViewModel.DataEntities;
using InfoHelper.ViewModel.States;

namespace InfoHelper.Windows
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ViewModelMain _vmMain;

        public MainWindow()
        {
            InitializeComponent();

            MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;

            _vmMain = new ViewModelMain(Dispatcher);

            ViewModelPlayers vmPlayers = new ViewModelPlayers();

            DataContext = _vmMain;

            //Test();

            new Controller(_vmMain, vmPlayers);
        }

        //private void Test()
        //{
        //    ViewModelActionsState vmas = _vmMain.HudsParentStates[0].ActionsState;

        //    vmas.Actions = @"xr/rcfr/bbrr";

        //    vmas.Visible = !vmas.Visible;

        //    vmas.UpdateBindings();

        //    ViewModelNameState vmns = _vmMain.HudsParentStates[0].NameState;

        //    vmns.Name = "akaWild!!!";

        //    vmns.Visible = true;

        //    vmns.IsConfirmed = !vmns.IsConfirmed;

        //    vmns.UpdateBindings();

        //    #region General

        //    ViewModelStatsHud vmgh = _vmMain.HudsParentStates[0].GeneralHudState;

        //    ValueCell handsCell = new ValueCell("Hands", "Hands played");

        //    handsCell.IncrementSample();

        //    StatsCell vpipCell = new StatsCell("Vpip", "Vpip");

        //    vpipCell.IncrementSample();
        //    vpipCell.IncrementSample();
        //    vpipCell.IncrementSample();
        //    vpipCell.IncrementValue();

        //    EvCell evCell = new EvCell("EvBb", "EvBb");

        //    evCell.IncrementSample();
        //    evCell.IncrementSample();
        //    evCell.IncrementValue(50);

        //    vmgh.Visible = true;

        //    vmgh.SetData(new DataCell[] {handsCell, vpipCell, evCell});

        //    vmgh.UpdateBindings();

        //    #endregion

        //    #region Preflop

        //    //ViewModelStatsHud postflop = _win.HudsParentStates[0].PostflopRiverHudState;

        //    //StatsCell sc1 = new StatsCell("Sb_Isolate_CC_Fold", "BTN fold unopened pot");
        //    //sc1.IncrementSample();
        //    //sc1.IncrementSample();
        //    //sc1.IncrementValue();

        //    //postflop.Visible = true;

        //    //postflop.SetData(new DataCell[] {sc1});
        //    //postflop.SetRows(new string[]
        //    //{
        //    //    //"FvRaise_R_Row",
        //    //    //"Threebet_R_Row",
        //    //    //"FvBet_T_B_Row",
        //    //    //"FvBet_RvsX_Row",
        //    //    //"Raise_T_B_Row",
        //    //    //"FvRaise_T_Row",
        //    //    //"FvRaise_T_B_Row",
        //    //    //"Threebet_T_Row",
        //    //});

        //    //postflop.UpdateBindings();

        //    #endregion

        //    ViewModelStatsHud hpe = _vmMain.HudsParentStates[0].PostflopFlopHudState;

        //    ViewModelPreflopMatrixState vmpms = _vmMain.HudsParentStates[0].PreflopMatrixState;

        //    PreflopData cbData = new PreflopData();

        //    cbData.AddHand("9c", "Jd");

        //    cbData.AddHand("As", "2s");
        //    cbData.AddHand("As", "2s");

        //    cbData.AddHand("6c", "6h");
        //    cbData.AddHand("6c", "6h");
        //    cbData.AddHand("6c", "6h");

        //    cbData.AddHand("2s", "4s");
        //    cbData.AddHand("2s", "4s");
        //    cbData.AddHand("2s", "4s");
        //    cbData.AddHand("2s", "4s");

        //    cbData.AddHand("5h", "Qh");
        //    cbData.AddHand("5h", "Qh");
        //    cbData.AddHand("5h", "Qh");
        //    cbData.AddHand("5h", "Qh");
        //    cbData.AddHand("5h", "Qh");

        //    cbData.AddHand("7h", "6d");
        //    cbData.AddHand("7h", "6d");
        //    cbData.AddHand("7h", "6d");
        //    cbData.AddHand("7h", "6d");
        //    cbData.AddHand("7h", "6d");
        //    cbData.AddHand("7h", "6d");

        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");
        //    cbData.AddHand("Th", "Tc");

        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");
        //    cbData.AddHand("Qh", "Qc");

        //    vmpms.Header = "Test header";
        //    vmpms.PreflopData = cbData;
        //    vmpms.Visible = true;

        //    vmpms.UpdateBindings();

        //    PreflopData cxData = new PreflopData();

        //    cxData.AddHand("Kc", "Kd");

        //    cxData.AddHand("5s", "8s");
        //    cxData.AddHand("5s", "8s");

        //    StatsCell cxCell = new StatsCell("CX_F", "Continuation check flop") { CellData = cxData };

        //    StatsCell cbCell = new StatsCell("CB_F", "Continuation bet flop") { CellData = cbData, ConnectedCells = new DataCell[] { cxCell } };

        //    Random rnd = new Random();

        //    int iterations = 4400;

        //    for (int i = 0; i < iterations; i++)
        //    {
        //        if (rnd.Next(0, 2) == 1)
        //            cbCell.IncrementValue();

        //        cbCell.IncrementSample();
        //    }

        //    hpe.SetData(new DataCell[] { cbCell });

        //    hpe.SetRows(new string[] { "IsHiddenRow" });

        //    hpe.Visible = true;

        //    hpe.UpdateBindings();
        //}
    }
}
