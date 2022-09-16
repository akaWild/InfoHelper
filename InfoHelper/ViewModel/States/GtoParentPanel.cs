using System.ComponentModel;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelGtoParentState : INotifyPropertyChanged
    {
        private string _error;
        public string Error
        {
            get => _error;
            set
            {
                _error = value;

                OnPropertyChanged();
            }
        }

        private bool _isSolverRunning;
        public bool IsSolverRunning
        {
            get => _isSolverRunning;
            set
            {
                _isSolverRunning = value;

                OnPropertyChanged();
            }
        }

        public ViewModelPreflopGtoState PreflopGtoState { get; } = new ViewModelPreflopGtoState();

        public ViewModelPostflopGtoState PostflopGtoState { get; } = new ViewModelPostflopGtoState();

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
