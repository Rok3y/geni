using LaunchService;
using LaunchService.Model;
using LaunchService.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services )=> {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
        services.AddLogging(logging => 
        {
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
            logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
        });

        var config = context.Configuration;
        string dbProvider = config["DatabaseProvider"];

        services.AddSingleton<IConfiguration, Configuration>();
        services.AddSingleton<HttpClient>();
        services.AddDbContext<LaunchDbContext>(options =>
        {
            if (dbProvider == "SqlServer")
            {
                options.UseSqlServer(config["LaunchDbConnectionString"]);
            }
            else
            {
                string dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "launches.db");
                options.UseSqlite($"Data Source={dbPath}");
            }

            // Suppress SQL query logs
            options.LogTo(Console.WriteLine, LogLevel.Warning);
        });
        services.AddScoped<ILaunchDbService, LaunchDbService>();
        services.AddScoped<IMailservice, MailService>();
        services.AddScoped<IRocketLaunchService, RocketLaunchService>();
    })
    .Build();

host.Run();