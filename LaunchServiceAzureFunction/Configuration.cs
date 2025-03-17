using LaunchService.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LaunchService
{
    public interface IConfiguration
    {
        string ConfigurationSettingsFile { get; }
        string ConfigurationMailFile { get; }
        string ApiBaseUrl { get; set; }
        string ApiLaunchesUrl { get; set; }
        string ApiKey { get; set; }
        string SmtpServer { get; set; }
        List<string> Recipients { get; set; }

        void ReadConfiguration();
    }

    public class Configuration : IConfiguration
    {
        private ILogger<Configuration> _logger;

        public string ConfigurationSettingsFile { get; } = "appsettings.json";
        public string ConfigurationMailFile { get; } = "emails.json";

        public string ApiBaseUrl { get; set; } = string.Empty;
        public string ApiLaunchesUrl { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string SmtpServer { get; set; } = string.Empty;
        public List<string> Recipients { get; set; } = new List<string>();

        public string DbProvider { get; set; }
        public string ConnectionString { get; set; }
        public string DbFileName { get; set; }

        public Configuration(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger<Configuration>();
        }

        public void ReadConfiguration()
        {
            try
            {
                if (!File.Exists(ConfigurationSettingsFile))
                    throw new FileNotFoundException($"Configuration file '{ConfigurationSettingsFile}' not found.");

                var config = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(ConfigurationSettingsFile, optional: false, reloadOnChange: true)
                    .Build();

                this.ApiBaseUrl = config["ApiSettings:BaseUrl"];
                this.ApiLaunchesUrl = config["ApiSettings:LaunchesUrl"];
                this.ApiKey = config["ApiSettings:ApiKey"];
                this.SmtpServer = config["EmailSettings:SmtpServer"];

                if (!File.Exists(ConfigurationMailFile))
                    throw new FileNotFoundException($"Configuration mail file '{ConfigurationMailFile}' not found.");

                var mailConfig = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile(ConfigurationMailFile, optional: false, reloadOnChange: true)
                    .Build();

                Recipients = mailConfig.GetSection("emails").Get<List<string>>();
                if (Recipients == null || Recipients.Count == 0)
                {
                    _logger.LogWarning("No recipients defined in the configuration file!");
                }
            }
            catch (Exception ex) 
            {
                _logger.LogError($"Error reading configuration: {ex.Message}");
            }
        }
    }
}
