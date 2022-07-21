using System;
using System.Windows.Input;
using System.Windows.Threading;
using InfoHelper.Utils;
using Microsoft.VisualBasic;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelControlsState : ViewModelStateBase
    {
        private readonly Dispatcher _dispatcher;

        public event EventHandler RunningStateChanged;

        public event EventHandler FlushPicturesRequested;

        public event EventHandler ExitRequested;

        public event EventHandler ShowOptionsRequested;

        public ViewModelControlsState(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public ViewModelProgressBarState ProgressBarState { get; } = new ViewModelProgressBarState();

        private bool _isRunning;
        public bool IsRunning
        {
            get => _isRunning;
            private set
            {
                _isRunning = value;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        public bool SavePictures { get; private set; }

        private bool _flushPicturesInProgress;
        public bool FlushPicturesInProgress
        {
            get => _flushPicturesInProgress;
            private set
            {
                _flushPicturesInProgress = value;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _startButtonText = "Start";
        public string StartButtonText
        {
            get => _startButtonText;
            private set
            {
                _startButtonText = value;

                OnPropertyChanged();
            }
        }

        private string _error;
        public string Error
        {
            get => _error;
            private set
            {
                _error = value;

                OnPropertyChanged();
            }
        }

        private bool _isCriticalError;
        public bool IsCriticalError
        {
            get => _isCriticalError;
            private set
            {
                _isCriticalError = value;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private Command _exitCommand;
        public Command ExitCommand => _exitCommand ??= new Command(obj =>
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }, (obj) => !IsRunning && !FlushPicturesInProgress);

        private Command _optionsCommand;
        public Command OptionsCommand => _optionsCommand ??= new Command(obj =>
        {
            ShowOptionsRequested?.Invoke(this, EventArgs.Empty);
        }, (obj) => !IsRunning && !FlushPicturesInProgress);

        private Command _startStopCommand;
        public Command StartStopCommand => _startStopCommand ??= new Command(obj =>
        {
            StartStop();

            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }, (obj) => !FlushPicturesInProgress && !IsCriticalError);

        private Command _flushPicturesCommand;
        public Command FlushPicturesCommand => _flushPicturesCommand ??= new Command(obj =>
        {
            FlushPicturesRequested?.Invoke(this, EventArgs.Empty);
        }, (obj) => !IsRunning && !FlushPicturesInProgress && !IsCriticalError);

        private Command _savePicturesCommand;
        public Command SavePicturesCommand => _savePicturesCommand ??= new Command(obj =>
        {
            SavePictures = !SavePictures;
        }, (obj) => !FlushPicturesInProgress);

        public void StartStop()
        {
            IsRunning = !IsRunning;

            StartButtonText = IsRunning ? "Stop" : "Start";
        }

        public void BeginFlushingPictures()
        {
            _dispatcher.Invoke(() => FlushPicturesInProgress = true);
        }

        public void EndFlushingPictures()
        {
            _dispatcher.Invoke(() => FlushPicturesInProgress = false);
        }

        public void SetError(string error, bool isCriticalError = false)
        {
            if (isCriticalError)
                error = error.Insert(0, "Critical error!!! ");

            Error = error;

            IsCriticalError = isCriticalError;
        }
    }
}
