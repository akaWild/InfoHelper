using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GtoDbUtility;
using Microsoft.EntityFrameworkCore;


namespace InfoHelper.Db
{
    public sealed class GtoDbContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<PostflopEntry> PostflopEntries { get; set; } = null!;
        public DbSet<PostflopData> PostflopData { get; set; } = null!;

        public GtoDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(_connectionString);
        }
    }
}
