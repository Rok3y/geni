using System;
using LaunchService.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace LaunchServiceAzureFunction
{
    public class RocketLaunchFunction
    {
        private readonly ILogger _logger;
        private readonly IRocketLaunchService _rocketLaunchService;

        public RocketLaunchFunction(IRocketLaunchService rocketLaunchService, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<RocketLaunchFunction>();
            _rocketLaunchService = rocketLaunchService;
        }

        [Function("LaunchService")]
        public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var currentDate = DateTime.Now;
            var launches = await _rocketLaunchService.FetchLaunches(currentDate);
            _ = await _rocketLaunchService.StoreAndNotifyLaunches(launches, currentDate);
            Console.WriteLine($"Number of launches: {launches.Count}");


            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}
