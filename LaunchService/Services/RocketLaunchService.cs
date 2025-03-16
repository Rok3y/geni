using LaunchService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
using System.Globalization;

namespace LaunchService.Services
{
    public interface IRocketLaunchService
    {
        Task<List<Launch>> FetchLaunches(DateTime currentDate);
        Task<Week> AnalyzeAndStoreLaunches(List<Launch> launches, DateTime currentDate);
    }

    public class RocketLaunchService : IRocketLaunchService
    {
        private readonly ILogger<RocketLaunchService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ILaunchDbService _db;
        private readonly IConfiguration _configuration;
        private readonly IMailservice _mailservice;

        public RocketLaunchService(
            HttpClient httpClient, 
            ILaunchDbService db, 
            IConfiguration configuration, 
            IMailservice mailservice,
            ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _db = db;
            _configuration = configuration;
            _mailservice = mailservice;
            _logger = loggerFactory.CreateLogger<RocketLaunchService>();

            _httpClient.BaseAddress = new Uri(_configuration.ApiBaseUrl);
        }

        public async Task<List<Launch>> FetchLaunches(DateTime currentDate)
        {
            Dictionary<string, string> queryParam = new Dictionary<string, string>();
            queryParam.Add("format", "json");
            queryParam.Add("ordering", "net");
            queryParam.Add("mode", "list");

            var (startDate, endDate) = Helper.GetNextWeekRange(currentDate);
            string url = createUrl(startDate, endDate, queryParam).ToString();

            using HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                throw new HttpRequestException($"API requst failed with status {response.StatusCode}: {response.ReasonPhrase}");
            }

            _logger.LogInformation($"Success! Status: {response.StatusCode}");
            string responseBody = await response.Content.ReadAsStringAsync();

            // Deserialize JSON
            using JsonDocument doc = JsonDocument.Parse(responseBody);
            int resultCount = 0;
            string countText = doc.RootElement.GetProperty("count").GetRawText();
            if (!int.TryParse(countText, out resultCount))
            {
                _logger.LogError($"Could not parse count property");
                throw new DataMisalignedException($"Could not parse count property: {countText}");
            }
            _logger.LogInformation($"Recieved {resultCount} launches for the next week");

            // Handle results
            var launches = new List<Launch>();
            var results = doc.RootElement.GetProperty("results");
            foreach (var item in results.EnumerateArray())
            {
                string launchId = item.GetProperty("id").GetString() ?? string.Empty;
                string rocketName = item.GetProperty("name").GetString() ?? "Unkown rocket";
                DateTime launchDate = item.GetProperty("net").GetDateTime();
                var launch = new Launch()
                {
                    Id = launchId,
                    T0 = launchDate,
                    RocketName = rocketName,
                    Notified = false
                };
                launches.Add(launch);
            }

            // Store to the database
            return launches;
        }

        public async Task<Week> AnalyzeAndStoreLaunches(List<Launch> launches, DateTime currentDate)
        {
            if (launches.IsNullOrEmpty())
            {
                _logger.LogInformation($"No launches to store");
                return null;
            }

            bool sendMailStatus = false;
            var (startDate, endDate) = Helper.GetNextWeekRange(currentDate);
            int weekNumber = Helper.GetWeekNumber(startDate);

            var week = await _db.GetWeek(weekNumber, startDate.Year);

            if (week != null)
            {
                var launchesDict = new Dictionary<string, List<Launch>>();
                launchesDict["added"] = new List<Launch>();
                launchesDict["removed"] = new List<Launch>();
                launchesDict["modified"] = new List<Launch>();

                // Check for differences and send the update
                List<Launch> storedLaunches = week.Launches.ToList();

                if (storedLaunches.Count != launches.Count)
                {
                    // Get all newly added/removed launches that from the current week
                    launchesDict["added"] = launches.Where(newLaunch => !storedLaunches.Any(oldLaunch => oldLaunch == newLaunch)).ToList();
                    launchesDict["removed"] = storedLaunches.Where(oldLaunch => !launches.Any(newLaunch => newLaunch == oldLaunch)).ToList();
                }

                // Get all modified launch objects
                launchesDict["modified"] = launches.Where(l => !storedLaunches.Contains(l)).ToList();
                
                // Update week with modified launches
                 if (!launchesDict["added"].IsNullOrEmpty())
                    await _db.AddLaunchesAsync(launchesDict["added"]);

                if (!launchesDict["removed"].IsNullOrEmpty())
                    await _db.RemoveLaunchesAsync(launchesDict["removed"]);

                foreach(var l in launchesDict["modified"])
                    await _db.UpdateLaunch(l);

                sendMailStatus = await _mailservice.SendMailToRecipients(launchesDict);
            }
            else 
            {
                week = new Week()
                {
                    Id = Guid.NewGuid().ToString(),
                    WeekNumber = weekNumber,
                    Year = startDate.Year,
                    WeekStart = startDate,
                    WeekEnd = endDate,
                    Notified = DateTime.MinValue,
                    Launches = launches
                };

                foreach(var launch in launches)
                {
                    launch.WeekId = week.Id;
                    launch.Week = week;
                }

                await _db.AddWeekAsync(week);

                // Send the update
                sendMailStatus = await _mailservice.SendMailToRecipients(launches);
            }

            if (sendMailStatus)
            {
                week.Notified = DateTime.Now;
                await _db.UpdateWeekAsync(week);
            }
            else
                _logger.LogWarning("Could not send notification mail!");

            return week;
        }

        private async Task<bool> CheckForUpdate(int weekNumber, DateTime date)
        {
            List<Launch> storedLaunches = await _db.GetAllLaunchesForWeek(weekNumber, date.Year);
            return true;

        }

        [assembly: InternalsVisibleTo("LaunchService.test")]
        private Uri createUrl(DateTime start, DateTime end, Dictionary<string, string> queryParams)
        {
            var uriBuilder = new UriBuilder(_configuration.ApiBaseUrl);
            uriBuilder.Path = _configuration.ApiLaunchesUrl;
            var query = HttpUtility.ParseQueryString(string.Empty);

            foreach (var item in queryParams)
            {
                query[item.Key] = item.Value;
            }
            query["net__gte"] = start.ToString("yyyy-MM-ddTHH:mm:ssZ");
            query["net__lte"] = end.ToString("yyyy-MM-ddTHH:mm:ssZ");

            uriBuilder.Query = query.ToString();

            return uriBuilder.Uri;
        }
    }
}
