using LaunchService.Model;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.Text.Json;
using System.Web;
using System.Configuration;

namespace LaunchService.Services
{
    public interface IRocketLaunchService
    {
        Task<List<Launch>> FetchLaunches(DateTime currentDate);
        Task<Week> StoreAndNotifyLaunches(List<Launch> launches, DateTime currentDate);
    }

    public class RocketLaunchService : IRocketLaunchService
    {
        private readonly ILogger<RocketLaunchService> _logger;
        private readonly HttpClient _httpClient;
        private readonly ILaunchDbService _db;
        private readonly IMailservice _mailservice;

        public RocketLaunchService(
            HttpClient httpClient, 
            ILaunchDbService db,
            IMailservice mailservice,
            ILoggerFactory loggerFactory)
        {
            _httpClient = httpClient;
            _db = db;
            _mailservice = mailservice;
            _logger = loggerFactory.CreateLogger<RocketLaunchService>();

            if (_httpClient.BaseAddress == null)
            {
                string baseUrl = Environment.GetEnvironmentVariable("BaseUrl");
                if (!string.IsNullOrEmpty(baseUrl))
                {
                    _httpClient.BaseAddress = new Uri(baseUrl);
                }
            }
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

            _logger.LogInformation($"Success! Status: {response.StatusCode} for request {url}");
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
                string rocketId = item.GetProperty("id").GetString() ?? string.Empty;
                string rocketName = item.GetProperty("name").GetString() ?? "Unkown rocket";
                uint launchStatus = item.GetProperty("status").GetProperty("id").GetUInt32();
                DateTime lastUpdated = item.GetProperty("last_updated").GetDateTime();
                DateTime launchDate = item.GetProperty("net").GetDateTime();

                var launch = new Launch()
                {
                    RocketId = rocketId,
                    RocketName = rocketName,
                    Status = (LaunchStatus)launchStatus,
                    T0 = launchDate,
                    LastUpdated = lastUpdated
                };
                launches.Add(launch);
            }

            // Store to the database
            return launches;
        }

        public async Task<Week> StoreAndNotifyLaunches(List<Launch> launches, DateTime currentDate)
        {
            bool sendMailStatus = false;
            bool shouldSendEmail = false;
            var (startDate, endDate) = Helper.GetNextWeekRange(currentDate);
            int weekNumber = Helper.GetWeekNumber(startDate);

            var week = await _db.GetWeek(weekNumber, startDate.Year);

            if (launches.IsNullOrEmpty())
            {
                _logger.LogInformation($"No launches to store");
                return week;
            }

            if (week != null)
            {
                var launchesDict = new Dictionary<string, List<Launch>>();
                launchesDict["added"] = new List<Launch>();
                launchesDict["modified"] = new List<Launch>();

                // Check for differences and send the update
                List<Launch> storedLaunches = week.Launches.ToList();

                if (storedLaunches.Count != launches.Count)
                {
                    // Get all newly added launches for the current week
                    launchesDict["added"] = launches.Where(newLaunch => !storedLaunches.Any(oldLaunch => oldLaunch.RocketId == newLaunch.RocketId)).ToList();
                    launchesDict["added"].ForEach(newLaunch => week.Launches.Add(newLaunch));
                    shouldSendEmail = true;
                }

                // Get all modified launch objects
                foreach (var launch in launches)
                {
                    var storedLaunch = storedLaunches.SingleOrDefault(l => l.RocketId.Equals(launch.RocketId));
                    if (storedLaunch != null && storedLaunch.LastUpdated != launch.LastUpdated)
                    {
                        // Check if removed or status changed
                        if (storedLaunch.Status != launch.Status || !storedLaunch.T0.Equals(launch.T0))
                        {                            
                            //Update properties of the existing storedLaunch
                            storedLaunch.RocketName = launch.RocketName;
                            storedLaunch.Status = launch.Status;
                            storedLaunch.T0 = launch.T0;
                            storedLaunch.LastUpdated = launch.LastUpdated;
                            shouldSendEmail = true;

                            launchesDict["modified"].Add(storedLaunch);
                        }
                    }
                }

                // Update week with added/modified launches
                 if (!launchesDict["added"].IsNullOrEmpty())
                    await _db.AddLaunchesAsync(launchesDict["added"]);

                if (!launchesDict["modified"].IsNullOrEmpty())
                    await _db.UpdateLaunches(launchesDict["modified"]);

                if (shouldSendEmail)
                    sendMailStatus = await _mailservice.SendMailToRecipients(launchesDict, week);
                else
                    _logger.LogInformation($"No chanes for the upcoming week {week.WeekStart} - {week.WeekEnd}");
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
                shouldSendEmail = true;

                // Send the update
                sendMailStatus = await _mailservice.SendMailToRecipients(week);
            }

            if (shouldSendEmail && sendMailStatus)
            {
                week.Notified = DateTime.Now;
                await _db.UpdateWeekAsync(week);
            }
            else if (shouldSendEmail && !sendMailStatus)
                _logger.LogWarning("Could not send notification mail!");

            return week;
        }

        private Uri createUrl(DateTime start, DateTime end, Dictionary<string, string> queryParams)
        {
            var uriBuilder = new UriBuilder(Environment.GetEnvironmentVariable("BaseUrl"));
            uriBuilder.Path = Environment.GetEnvironmentVariable("LaunchesUrl");
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
