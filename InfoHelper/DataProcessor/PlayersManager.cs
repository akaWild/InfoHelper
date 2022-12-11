using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using InfoHelper.Db;
using InfoHelper.Utils;
using InfoHelper.ViewModel.States;
using Microsoft.Data.SqlClient;
using PlayerControlsLibrary;

namespace InfoHelper.DataProcessor
{
    public class PlayersManager
    {
        private readonly ViewModelPlayers _playersViewModel;

        private readonly string _connectionString;

        private readonly object _lock = new object();

        public PlayersManager(ViewModelPlayers viewModel)
        {
            _playersViewModel = viewModel;

            ICollection<Player> players;

            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = "PlayersGG"
            };

            _connectionString = sqlBuilder.ConnectionString;

            using (PlayerContext plContext = new PlayerContext(_connectionString, true))
                players = plContext.Players.ToList();

            foreach (Player player in players)
                player.Confirmed += Player_Confirmed;

            _playersViewModel.Players = new ObservableCollection<Player>(players);

            BindingOperations.EnableCollectionSynchronization(_playersViewModel.Players, _lock);
        }

        public (string name, bool isConfirmed) GetPlayer(BitmapSource image, string hash)
        {
            Player matchPlayer = null;

            lock (_lock)
                matchPlayer = _playersViewModel.Players.FirstOrDefault(p => p.Hash == hash);

            if (matchPlayer == null)
            {
                matchPlayer = new Player
                {
                    Image = image,
                    Hash = hash,
                    Name = string.Empty,
                    IsConfirmed = false,
                };

                matchPlayer.Confirmed += Player_Confirmed;

                lock (_lock)
                    _playersViewModel.Players.Add(matchPlayer);
            }

            return (matchPlayer.Name, matchPlayer.IsConfirmed);
        }

        private void Player_Confirmed(object sender, EventArgs e)
        {
            using PlayerContext plContext = new PlayerContext(_connectionString, false);

            Player player = (Player)sender;

            if (!plContext.Players.Contains(player))
                plContext.Players.Add(player);
            else
                plContext.Players.Update(player);

            plContext.SaveChanges();
        }
    }
}
