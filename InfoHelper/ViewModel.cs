using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using JetBrains.Annotations;

namespace InfoHelper
{
    public class WindowEntity : INotifyPropertyChanged
    {
        private bool _isRunning = false;

        private readonly HudPanelEntity[] _hudPanels = new HudPanelEntity[5] {new HudPanelEntity(), new HudPanelEntity(), new HudPanelEntity(), new HudPanelEntity(), new HudPanelEntity()};

        public HudPanelEntity this[int index] => _hudPanels[index];

        private Command _exitCommand;
        public Command ExitCommand => _exitCommand ??= new Command(obj => MessageBox.Show("Exit!"), (obj) =>!_isRunning);

        private Command _optionsCommand;
        public Command OptionsCommand => _optionsCommand ??= new Command(obj => MessageBox.Show("Options!"), (obj) => !_isRunning);

        private Command _startStopCommand;
        public Command StartStopCommand => _startStopCommand ??= new Command(obj =>
        {
            _isRunning = !_isRunning;

            StartButtonText = _isRunning ? "Stop" : "Start";
        });

        private Command _saveCommand;
        public Command SaveCommand => _saveCommand ??= new Command(obj =>
        { }, (obj) => !_isRunning);

        private string _startStopButtonText = "Start";
        public string StartButtonText
        {
            get => _startStopButtonText;
            set
            {
                _startStopButtonText = value;

                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PostflopHudEntity : INotifyPropertyChanged
    {
        private readonly Dictionary<string, DataCell> _cells = new Dictionary<string, DataCell>() { { "CB_F", null } };

        public StatsCell CB_F => GetCell();

        public bool IsHiddenRow { get; set; } = true;

        public void LoadData(DataCell[] cells)
        {
            foreach (DataCell cell in cells)
                _cells[cell.Name] = cell;
        }

        public void UpdateBindings()
        {
            OnPropertyChanged(string.Empty);
        }

        private StatsCell GetCell([CallerMemberName] string cellName = null) => (StatsCell)_cells[cellName];

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class HudPanelEntity : INotifyPropertyChanged
    {
        public PostflopHudEntity PostflopHud { get; } = new PostflopHudEntity();

        //private readonly Dictionary<string, DataCell> _cells = new Dictionary<string, DataCell>() { { "CB_F", null } };

        //public StatsCell CB_F => GetCell();

        //public void LoadData(DataCell[] cells)
        //{
        //    foreach (DataCell cell in cells)
        //        _cells[cell.Name] = cell;
        //}

        //public void UpdateBindings()
        //{
        //    OnPropertyChanged(string.Empty);
        //}

        //private StatsCell GetCell([CallerMemberName] string cellName = null) => (StatsCell)_cells[cellName];

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
