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

            new Controller(_vmMain, vmPlayers);
        }
    }
}
