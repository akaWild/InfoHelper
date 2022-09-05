using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PlayerControlsLibrary;

namespace InfoHelper.Db
{
    public sealed class PlayerContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<Player> Players { get; set; } = null!;

        public PlayerContext(string connectionString, bool ensureCreated)
        {
            _connectionString = connectionString;

            if (ensureCreated)
                Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>().HasKey(u => u.Hash);

            modelBuilder.Entity<Player>().Ignore(u => u.Image);
            modelBuilder.Entity<Player>().Ignore(u => u.IsConfirmed);
        }
    }
}
