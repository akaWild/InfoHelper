using System;
using InfoHelper.Utils;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelControlsState : ViewModelStateBase
    {
        public event EventHandler RunningStateChanged;

        public event EventHandler SavePicturesValueChanged;

        public bool IsRunning { get; set; }

        public bool SavePictures { get; private set; }

        private Command _exitCommand;
        public Command ExitCommand => _exitCommand ??= new Command(obj =>
        {

        }, (obj) => !IsRunning);

        private Command _optionsCommand;
        public Command OptionsCommand => _optionsCommand ??= new Command(obj =>
        {

        }, (obj) => !IsRunning);

        private Command _startStopCommand;
        public Command StartStopCommand => _startStopCommand ??= new Command(obj =>
        {
            IsRunning = !IsRunning;

            StartButtonText = IsRunning ? "Stop" : "Start";

            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        });

        private Command _saveCommand;
        public Command SaveCommand => _saveCommand ??= new Command(obj =>
        {
            SavePictures = !SavePictures;

            SavePicturesValueChanged?.Invoke(this, EventArgs.Empty);
        }, (obj) => !IsRunning);

        private string _startStopButtonText = "Start";
        public string StartButtonText
        {
            get => _startStopButtonText;
            private set
            {
                _startStopButtonText = value;

                OnPropertyChanged();
            }
        }

        public ViewModelProgressBarState ProgressBarState { get; } = new ViewModelProgressBarState();

        public string Error { get; set; }
    }
}
