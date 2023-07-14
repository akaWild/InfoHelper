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

using DbPlayer = StatDbUtility.Player;

namespace InfoHelper.DataProcessor
{
    public class PlayersManager
    {
        private readonly ViewModelPlayers _playersViewModel;

        private readonly string _ggPlayersConnectionString;

        private readonly object _lock = new object();

        public PlayersManager(ViewModelPlayers viewModel)
        {
            _playersViewModel = viewModel;

            ICollection<Player> ggPlayers;

            _ggPlayersConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = "PlayersGG"
            }.ConnectionString;

            using (PlayerContext plContext = new PlayerContext(_ggPlayersConnectionString, true))
                ggPlayers = plContext.Players.ToList();

            ICollection<DbPlayer> dbPlayers;

            string dbPlayersConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = Shared.DbName
            }.ConnectionString;

            using (StatDbContext statDbContext = new StatDbContext(dbPlayersConnectionString))
                dbPlayers = statDbContext.Players.ToList();

            foreach (DbPlayer player in dbPlayers)
            {
                Player matchedPlayer = ggPlayers.FirstOrDefault(p => p.Name == player.PlayerName);

                if (matchedPlayer == null)
                    ggPlayers.Add(new Player() {Name = player.PlayerName});
            }

            foreach (Player player in ggPlayers)
                player.Confirmed += Player_Confirmed;

            _playersViewModel.Players = new ObservableCollection<Player>(ggPlayers);

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
            using PlayerContext plContext = new PlayerContext(_ggPlayersConnectionString, false);

            Player player = (Player)sender;

            using FileStream fileStream = new FileStream(Path.Combine(Shared.PlayersImagesFolder, $"{player.Hash}.png"), FileMode.Create);

            BitmapEncoder encoder = new PngBitmapEncoder();

            encoder.Frames.Add(BitmapFrame.Create(player.Image));

            encoder.Save(fileStream);

            if (!plContext.Players.Contains(player))
                plContext.Players.Add(player);
            else
                plContext.Players.Update(player);

            plContext.SaveChanges();
        }
    }
}
