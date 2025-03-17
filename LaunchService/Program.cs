using LaunchService.Model;
using LaunchService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Runtime.CompilerServices;

namespace LaunchService
{
    internal class Program
    {
        private static Configuration _configuration;
        private static ILogger _logger;
        private static HttpClient _httpClient;
        private static IConfigurationManager _configurationManager;
        private static ILoggerFactory _loggerFactory;
        private static IServiceProvider _serviceProvider;
        private static IMailservice _mailservice;
        private static RocketLaunchService _rocketLaunchService;
        private static LaunchDbService _launchDbService;
        private static LaunchDbContext _db;

        static async Task Main(string[] args)
        {
            // Logger
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<Program>();

            // Load configuration
            try
            {
                _configuration = new Configuration(loggerFactory);
                _configuration.ReadConfiguration();
                _logger.LogInformation($"Configuration: {_configuration.ApiBaseUrl}{_configuration.ApiLaunchesUrl}");
            }
            catch ( Exception e )
            {
                _logger.LogError($"Error when reading configuration: {e.Message}");
            }

            // Setup Database connection
            _db = new LaunchDbContext();
            _launchDbService = new LaunchDbService(_db);
            _httpClient = new HttpClient();
            _mailservice = new MailService(_configuration, loggerFactory);
            _rocketLaunchService = new RocketLaunchService(_httpClient, _launchDbService, _configuration, _mailservice, loggerFactory);

            var currentDate = DateTime.Now;
            var launches = await _rocketLaunchService.FetchLaunches(currentDate);
            var week = await _rocketLaunchService.StoreAndNotifyLaunches(launches, currentDate);

            Console.WriteLine($"Number of launches: {launches.Count}");
            //await _mailservice.SendMailToRecipients(launches);

            Console.ReadLine();
        }


        // Maybe a azure function
        //private static async void GetLaunchesData()
        //{
        //    var (startdate, enddate) = Helper.GetNextWeekRange(DateTime.Now);

        //    var responseText = await _rocketLaunchService.GetLaunches(startdate, enddate);

        //    if (responseText == null) 
        //    {
        //        _logger.LogInformation("No data retrieved");
        //        return;
        //    }
        //}
    }
}
