using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using StatDbUtility;

namespace InfoHelper.Db
{
    public sealed class StatDbContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<Player> Players { get; set; } = null!;

        public DbSet<Hand> Hands { get; set; } = null!;

        public DbSet<PlayerHand> PlayerHands { get; set; } = null!;

        public DbSet<Game> Games { get; set; } = null!;

        public DbSet<Table> Tables { get; set; } = null!;

        public StatDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Player>();
            modelBuilder.Entity<Hand>();
            modelBuilder.Entity<PlayerHand>();
            modelBuilder.Entity<Game>();
            modelBuilder.Entity<Table>();
        }
    }
}
