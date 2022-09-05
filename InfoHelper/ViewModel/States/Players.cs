using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PlayerControlsLibrary;

namespace InfoHelper.ViewModel.States
{
    public class ViewModelPlayers : INotifyPropertyChanged
    {
        private ObservableCollection<Player> _players;

        public ObservableCollection<Player> Players
        {
            get => _players;
            set
            {
                _players = value;

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
}
