using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace InfoHelper.ViewModel.States
{
    public abstract class ViewModelStateBase : DynamicObject, INotifyPropertyChanged
    {
        protected bool _visible;

        public virtual bool Visible
        {
            get => _visible;
            set
            {
                _visible = value;

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

    public abstract class ViewModelDeferredBindableState : ViewModelStateBase
    {
        public override bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public void UpdateBindings()
        {
            OnPropertyChanged(string.Empty);
        }
    }

    public abstract class ViewModelDeferredBindableHeaderedState : ViewModelDeferredBindableState
    {
        public string Header { get; set; }
    }
}
