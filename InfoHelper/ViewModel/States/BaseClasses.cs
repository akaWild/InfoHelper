using System.ComponentModel;
using System.Dynamic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace InfoHelper.ViewModel.States
{
    public abstract class ViewModelStateBase : DynamicObject, INotifyPropertyChanged
    {
        public bool Visible { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public abstract class ViewModelDeferredBindableState : ViewModelStateBase
    {
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
