using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using InfoHelper.Db;
using InfoHelper.StatsEntities;
using InfoHelper.Utils;
using Microsoft.Data.SqlClient;

namespace InfoHelper.DataProcessor
{
    public partial class StatManager
    {
        private partial void GetPlayerStats(string player, StatSet[] statSets, CancellationToken token)
        {
            SqlConnectionStringBuilder sqlBuilder = new SqlConnectionStringBuilder
            {
                DataSource = Shared.ServerName,
                IntegratedSecurity = true,
                InitialCatalog = Shared.DbName
            };

            using StatDbContext statDbContext = new StatDbContext(sqlBuilder.ConnectionString);

            var playerData =
                (from ph in statDbContext.PlayerHands
                join h in statDbContext.Hands
                    on ph.HandId equals h.Id
                join p in statDbContext.Players
                    on ph.PlayerId equals p.Id
                where p.PlayerName == player
                orderby h.DateTimePlayed
                select new { h, ph }).Take(Shared.GetLastNHands);

            foreach (var record in playerData.AsQueryable())
            {
                if(token.IsCancellationRequested)
                    break;
            }
        }
    }
}
