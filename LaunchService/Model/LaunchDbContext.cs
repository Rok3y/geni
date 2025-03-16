using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService.Model
{
    public class LaunchDbContext : DbContext
    {
        public DbSet<Launch> Launches { get; set; }
        public DbSet<Week> Weeks { get; set; }
        public string DbPath { get; }

        public LaunchDbContext(DbContextOptions<LaunchDbContext> options) : base(options) { }

        public LaunchDbContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "launches.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (!options.IsConfigured)
            {
                options.UseSqlite($"Data Source={DbPath}");
            }
        }
    }
}
