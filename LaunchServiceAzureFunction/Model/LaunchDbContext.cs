using System.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                string dbProvider = Environment.GetEnvironmentVariable("DatabaseProvider");
                if (dbProvider == "SqlServer")
                {
                    string sqlConnectionString = Environment.GetEnvironmentVariable("LaunchDbConnectionString");
                    options.UseSqlServer(sqlConnectionString);
                }
                else
                {
                    // Default to SQLite for local testing
                    string dbPath = Environment.GetEnvironmentVariable("LaunchDbConnectionString");
                    options.UseSqlite($"Data Source={dbPath}");
                }

                options.LogTo(Console.WriteLine, LogLevel.Warning);
            }
        }
    }
}
