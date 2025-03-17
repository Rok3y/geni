using LaunchService.Model;
using LaunchService.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System.Net;

namespace LaunchService.test
{
    public class RocketLaunchIntegrationtests
    {
        private readonly ILaunchDbService _launchDbService;
        private readonly LaunchDbContext _dbContext;
        private readonly Mock<IMailservice> _mailserviceMock;
        private readonly Configuration _configurationMock;
        private HttpClient? _httpClientMock;
        private readonly DateTime _dateNow = new DateTime(2025, 3, 15, 12, 0, 0, DateTimeKind.Utc);
        private readonly DateTime _startDate;
        private readonly DateTime _endDate;
        private readonly string testFolder;

        public RocketLaunchIntegrationtests()
        {
            // DB service
            _dbContext = GetInMemoryDbContext();
            _launchDbService = new LaunchDbService(_dbContext);

            // Configuration
            _configurationMock = new Configuration(NullLoggerFactory.Instance)
            {
                ApiBaseUrl = "https://test-mock-api.com",
                ApiLaunchesUrl = "/launches/upcoming",
                ApiKey = "mock-api-key",
                SmtpServer = "mock-smtp.com"
            };

            // Mail service
            _mailserviceMock = new Mock<IMailservice>();

            // Datetime
            (_startDate, _endDate) = Helper.GetNextWeekRange(_dateNow);

            // Test folder path
            testFolder = Path.Combine(AppContext.BaseDirectory, "TestData");
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithoutLaunches_ShouldNotAddToDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response_empty.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);

            // Act
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);

            // Assert
            Assert.Null(week);

            Assert.Equal(0, await _dbContext.Weeks.CountAsync());
            Assert.Equal(0, await _dbContext.Launches.CountAsync());
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithLaunches_ShouldAddToDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);

            // Act
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);

            // Assert
            Assert.NotNull(week);

            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(6, await _dbContext.Launches.CountAsync());
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithLaunches_CallingTwice_ShouldAddOnceToDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Week>()))
                .ReturnsAsync(true);

            // Act
            var launches = await rocketLaunchService.FetchLaunches(_dateNow);
            _ = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);
            var launches2 = await rocketLaunchService.FetchLaunches(_dateNow);
            _ = await rocketLaunchService.StoreAndNotifyLaunches(launches2, _dateNow);

            // Assert
            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(6, await _dbContext.Launches.CountAsync());
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<Week>()), Times.Once);
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>(), It.IsAny<Week>()), Times.Never);
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithNewLaunches_ShouldAddToExistingWeekInDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>(), It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);
            Assert.NotNull(week);

            _httpClientMock.Dispose();

            // Simulate second call with new response
            mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "reponse1_newLaunch.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);
            rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Act
            var newlaunches = await rocketLaunchService.FetchLaunches(_dateNow);
            week = await rocketLaunchService.StoreAndNotifyLaunches(newlaunches, _dateNow);

            // Assert
            Assert.NotNull(week);

            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(7, await _dbContext.Launches.CountAsync());
            var launch = week.Launches.SingleOrDefault(l => l.RocketId == "e652a538-6d40-4b55-97a6-7c757ec4e1e9" && l.RocketName == "Spectrum | Maiden Flight");
            Assert.NotNull(launch);
            Assert.Equal("Spectrum | Maiden Flight", launch.RocketName);
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithCanceledLaunches_ShouldModifyWeekInDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>(), It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);
            var launch = week.Launches.SingleOrDefault(l => l.RocketId == "ad025362-828f-448c-855d-bfe53e04cdeb" && l.RocketName == "Electron | High Five (KinÃ©is 21-25)");
            Assert.NotNull(launch);
            Assert.Equal(LaunchStatus.GoForLaunch, launch.Status);

            _httpClientMock.Dispose();

            // Simulate second call with new response
            mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1_canceled.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);
            rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Act
            var newlaunches = await rocketLaunchService.FetchLaunches(_dateNow);
            week = await rocketLaunchService.StoreAndNotifyLaunches(newlaunches, _dateNow);

            // Assert
            Assert.NotNull(week);

            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(6, await _dbContext.Launches.CountAsync());
            launch = week.Launches.SingleOrDefault(l => l.RocketId == "ad025362-828f-448c-855d-bfe53e04cdeb" && l.RocketName == "Electron | High Five (KinÃ©is 21-25)");
            Assert.NotNull(launch);
            Assert.Equal(LaunchStatus.OnHold, launch.Status);
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithUpdatedLaunches_ShouldModifyWeekInDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>(), It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);
            var launchFalcon = createLaunchCopy(week.Launches.SingleOrDefault(l => l.RocketId == "ff04cc6f-981d-4b55-8368-3b8a59c1120e" && l.RocketName == "Falcon 9 Block 5 | Starlink Group 12-25"));
            var launchCeres = createLaunchCopy(week.Launches.SingleOrDefault(l => l.RocketId == "558c07d5-625d-4b83-b7a0-91d3de5797ae" && l.RocketName == "Ceres-1 | Unknown Payload"));
            Assert.NotNull(launchFalcon);
            Assert.NotNull(launchCeres);

            _httpClientMock.Dispose();

            // Simulate second call with new response
            mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1_updated_launch.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);
            rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Act
            var newlaunches = await rocketLaunchService.FetchLaunches(_dateNow);
            week = await rocketLaunchService.StoreAndNotifyLaunches(newlaunches, _dateNow);

            // Assert
            Assert.NotNull(week);
            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(6, await _dbContext.Launches.CountAsync());
            var updatedLaunchFalcon = week.Launches.SingleOrDefault(l => l.RocketId == "ff04cc6f-981d-4b55-8368-3b8a59c1120e" && l.RocketName == "Falcon 9 Block 5 | Starlink Group 12-25");
            var updatedLaunchCeres = week.Launches.SingleOrDefault(l => l.RocketId == "558c07d5-625d-4b83-b7a0-91d3de5797ae" && l.RocketName == "Ceres-1 | Unknown Payload");

            Assert.NotNull(updatedLaunchFalcon);
            Assert.NotEqual(updatedLaunchFalcon.LastUpdated, launchFalcon.LastUpdated);
            Assert.NotEqual(updatedLaunchFalcon.T0, launchFalcon.T0);
            Assert.Equal(updatedLaunchFalcon.Id, launchFalcon.Id);
            Assert.Equal(updatedLaunchFalcon.RocketName, launchFalcon.RocketName);
            Assert.Equal(updatedLaunchFalcon.Status, launchFalcon.Status);

            Assert.NotNull(updatedLaunchCeres);
            Assert.NotEqual(updatedLaunchCeres.LastUpdated, launchCeres.LastUpdated);
            Assert.NotEqual(updatedLaunchCeres.T0, launchCeres.T0);
            Assert.Equal(updatedLaunchCeres.Id, launchCeres.Id);
            Assert.Equal(updatedLaunchCeres.RocketName, launchCeres.RocketName);
            Assert.Equal(updatedLaunchCeres.Status, launchCeres.Status);
        }

        [Fact]
        public async Task StoreAndNotifyLaunches_WithChangedLaunches_ShouldModifyWeekInDB()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>(), It.IsAny<Week>()))
                .ReturnsAsync(true);

            var launches = await rocketLaunchService.FetchLaunches(_dateNow);
            var week = await rocketLaunchService.StoreAndNotifyLaunches(launches, _dateNow);
            var launchFalcon = createLaunchCopy(week.Launches.SingleOrDefault(l => l.RocketId == "ff04cc6f-981d-4b55-8368-3b8a59c1120e" && l.RocketName == "Falcon 9 Block 5 | Starlink Group 12-25")); // Changed net
            var launchElectron = createLaunchCopy(week.Launches.SingleOrDefault(l => l.RocketId == "ad025362-828f-448c-855d-bfe53e04cdeb" && l.RocketName == "Electron | High Five (KinÃ©is 21-25)")); // Changed status
            Assert.NotNull(launchFalcon);
            Assert.NotNull(launchElectron);

            _httpClientMock.Dispose();

            // Simulate second call with new response
            mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, "response1_combined.json");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);
            rocketLaunchService = new RocketLaunchService(_httpClientMock, _launchDbService, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);

            // Act
            var newlaunches = await rocketLaunchService.FetchLaunches(_dateNow);
            week = await rocketLaunchService.StoreAndNotifyLaunches(newlaunches, _dateNow);

            // Assert
            Assert.NotNull(week);
            Assert.Equal(1, await _dbContext.Weeks.CountAsync());
            Assert.Equal(7, await _dbContext.Launches.CountAsync());
            var updatedLaunchFalcon = week.Launches.SingleOrDefault(l => l.RocketId == "ff04cc6f-981d-4b55-8368-3b8a59c1120e" && l.RocketName == "Falcon 9 Block 5 | Starlink Group 12-25");
            var updatedlaunchElectron = week.Launches.SingleOrDefault(l => l.RocketId == "ad025362-828f-448c-855d-bfe53e04cdeb" && l.RocketName == "Electron | High Five (KinÃ©is 21-25)");
            var addedNewLaunchElectron = week.Launches.SingleOrDefault(l => l.RocketId == "e652a538-6d40-4b55-97a6-7c757ec4e1e9" && l.RocketName == "Spectrum | Maiden Flight");

            // Assert new launch added
            Assert.NotNull(addedNewLaunchElectron);

            // Assert updated net change
            Assert.NotNull(updatedLaunchFalcon);
            Assert.NotEqual(updatedLaunchFalcon.LastUpdated, launchFalcon.LastUpdated);
            Assert.NotEqual(updatedLaunchFalcon.T0, launchFalcon.T0);
            Assert.Equal(updatedLaunchFalcon.Id, launchFalcon.Id);
            Assert.Equal(updatedLaunchFalcon.RocketName, launchFalcon.RocketName);
            Assert.Equal(updatedLaunchFalcon.Status, launchFalcon.Status);

            // Asseert canceled status change
            Assert.NotNull(updatedlaunchElectron);
            Assert.NotEqual(updatedlaunchElectron.LastUpdated, launchElectron.LastUpdated);
            Assert.Equal(updatedlaunchElectron.T0, launchElectron.T0);
            Assert.Equal(updatedlaunchElectron.Id, launchElectron.Id);
            Assert.Equal(updatedlaunchElectron.RocketName, launchElectron.RocketName);
            Assert.NotEqual(updatedlaunchElectron.Status, launchElectron.Status);
        }

        private LaunchDbContext GetInMemoryDbContext()
        {
            if (_dbContext != null)
                return _dbContext;

            var options = new DbContextOptionsBuilder<LaunchDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new LaunchDbContext(options);
        }

        private Mock<HttpMessageHandler> GetMockMessageHandler(HttpStatusCode statusCode, string responseBodyFile)
        {
            string testDataFilePath = Path.Combine(testFolder, responseBodyFile);
            Assert.True(File.Exists(testDataFilePath));
            string responseBody = File.ReadAllText(testDataFilePath);

            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected() // To mock procteded method of SendAsync in HttpMessageHandler class
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody)
                });

            return mockMessageHandler;
        }

        private Launch? createLaunchCopy(Launch originalLaunch)
        {
            return new Launch
            {
                Id = originalLaunch.Id,
                RocketId = originalLaunch.RocketId,
                RocketName = originalLaunch.RocketName,
                LastUpdated = originalLaunch.LastUpdated,
                Status = originalLaunch.Status,
                T0 = originalLaunch.T0,
                Week = originalLaunch.Week,
                WeekId = originalLaunch.WeekId
            };
        }
    }
}
