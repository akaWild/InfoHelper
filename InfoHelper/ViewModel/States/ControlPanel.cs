using System;
using System.Windows.Input;
using System.Windows.Threading;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using InfoHelper.ViewModel.DataEntities;
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

        public event EventHandler ShowPlayersRequested;

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

        private string _equity;
        public string Equity
        {
            get => _equity;
            set
            {
                _equity = value;

                OnPropertyChanged();
            }
        }

        private HandType _handType;
        public HandType HandType
        {
            get => _handType;
            set
            {
                _handType = value;

                OnPropertyChanged();
            }
        }

        private string _potOdds;
        public string PotOdds
        {
            get => _potOdds;
            set
            {
                _potOdds = value;

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

        private ErrorType _errorType = ErrorType.NoError;
        public ErrorType ErrorType
        {
            get => _errorType;
            private set
            {
                _errorType = value;

                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _playersWindowVisible;
        public bool PlayersWindowVisible
        {
            get => _playersWindowVisible;
            set
            {
                _playersWindowVisible = value;

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
        }, (obj) => !IsRunning && !FlushPicturesInProgress && ErrorType != ErrorType.Critical);

        private Command _playersCommand;
        public Command PlayersCommand => _playersCommand ??= new Command(obj =>
        {
            ShowPlayersRequested?.Invoke(this, EventArgs.Empty);
        }, (obj) => ErrorType != ErrorType.Critical && ErrorType != ErrorType.Settings && !PlayersWindowVisible);

        private Command _startStopCommand;
        public Command StartStopCommand => _startStopCommand ??= new Command(obj =>
        {
            RunningStateChanged?.Invoke(this, EventArgs.Empty);
        }, (obj) => !FlushPicturesInProgress && ErrorType != ErrorType.Critical && ErrorType != ErrorType.Settings);

        private Command _flushPicturesCommand;
        public Command FlushPicturesCommand => _flushPicturesCommand ??= new Command(obj =>
        {
            FlushPicturesRequested?.Invoke(this, EventArgs.Empty);
        }, (obj) => !IsRunning && !FlushPicturesInProgress && ErrorType != ErrorType.Critical && ErrorType != ErrorType.Settings);

        private Command _savePicturesCommand;
        public Command SavePicturesCommand => _savePicturesCommand ??= new Command(obj =>
        {
            SavePictures = !SavePictures;
        });

        public void Start()
        {
            IsRunning = true;

            StartButtonText = "Stop";
        }

        public void Stop()
        {
            IsRunning = false;

            StartButtonText = "Start";
        }

        public void BeginFlushingPictures()
        {
            _dispatcher.Invoke(() => FlushPicturesInProgress = true);
        }

        public void EndFlushingPictures()
        {
            _dispatcher.Invoke(() => FlushPicturesInProgress = false);
        }

        public void SetError(string error, ErrorType errorType = ErrorType.Ordinary)
        {
            if (errorType == ErrorType.Settings)
                error = error.Insert(0, "Settings error!!! ");
            else if (errorType == ErrorType.Critical)
                error = error.Insert(0, "Critical error!!! ");

            Error = error;

            ErrorType = errorType;
        }

        public void ResetError()
        {
            Error = null;

            ErrorType = ErrorType.NoError;
        }
    }

    public enum ErrorType
    {
        NoError,
        Ordinary,
        Settings,
        Critical
    }
}
