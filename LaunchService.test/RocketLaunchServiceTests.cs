using LaunchService.Model;
using LaunchService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService.test
{
    public class RocketLaunchServiceTests
    {
        private Mock<ILaunchDbService> _dbServiceMock;
        private Mock<IMailservice> _mailserviceMock;
        private Configuration _configurationMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private HttpClient _httpClientMock;
        private readonly DateTime _dateNow = new DateTime(2025, 3, 15, 12, 0, 0);
        private DateTime _startDate;
        private DateTime _endDate;
        private string testFolder;

        public RocketLaunchServiceTests() 
        {
            // DB service
            _dbServiceMock = new Mock<ILaunchDbService>();

            // Configuration
            _configurationMock = new Configuration(NullLoggerFactory.Instance)
            {
                ApiBaseUrl = "https://test-mock-api.com",
                ApiLaunchesUrl = "/launches/upcoming",
                ApiKey = "mock-api-key",
                SmtpServer = "mock-smtp.com"
            };

            // HttpClient
            //_httpMessageHandlerMock = new Mock<HttpMessageHandler>();

            // Mail service
            _mailserviceMock = new Mock<IMailservice>();


            // Datetime
            // startDate ==> new DateTime(2025, 3, 17, 0, 0, 0);
            // endDate   ==> new DateTime(2025, 3, 23, 23, 59, 59);
            (_startDate, _endDate) = Helper.GetNextWeekRange(_dateNow);

            // Test folder path
            testFolder = Path.Combine(AppContext.BaseDirectory, "TestData");
        }

        [Fact]
        public async Task FeatchAndStoreData_HandlesFailedResponse()
        {
            // Arrange
            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.NotFound, "");
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            // Act & Assert
            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            await Assert.ThrowsAsync<HttpRequestException>(async () => await rocketLaunchService.FetchLaunches(_dateNow));
        }

        [Fact]
        public async Task FeatchAndStoreData_HandlesResponseMessage()
        {
            // Arrange
            string testDataFilePath = Path.Combine(testFolder, "response1.json");
            Assert.True(File.Exists(testDataFilePath));
            string responseBody = File.ReadAllText(testDataFilePath);

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, responseBody);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            // Act & Assert
            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var launches = await rocketLaunchService.FetchLaunches(_dateNow);

            Assert.NotEmpty(launches);
            Assert.NotNull(launches[0]);
            Assert.Equal(6, launches.Count);
        }

        [Fact]
        public async Task FeatchAndStoreData_HandlesEmptyResponseMessage()
        {
            // Arrange
            string testDataFilePath = Path.Combine(testFolder, "response_empty.json");
            Assert.True(File.Exists(testDataFilePath));
            string responseBody = File.ReadAllText(testDataFilePath);

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, responseBody);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            // Act & Assert
            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var launches = await rocketLaunchService.FetchLaunches(_dateNow);

            Assert.NotNull(launches);
            Assert.Empty(launches);
        }

        [Fact]
        public async Task AnalyzeAndStoreLaunches_WithOutLaunches_ShouldNotCall_AddWeekAsync_And_SendMail()
        {
            // Arrange

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, string.Empty);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var launches = new List<Launch> { };

            // Simulate that the week does not exist in the database
            _dbServiceMock.Setup(db => db.GetWeek(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((Week)null);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()))
                .ReturnsAsync(true);

            // Act
            var week = await rocketLaunchService.AnalyzeAndStoreLaunches(launches, _dateNow);

            // Assert
            Assert.Null(week);
            _dbServiceMock.Verify(db => db.AddWeekAsync(It.IsAny<Week>()), Times.Never());
            _dbServiceMock.Verify(db => db.UpdateWeekAsync(It.IsAny<Week>()), Times.Never());
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()), Times.Never());
        }

        [Fact]
        public async Task AnalyzeAndStoreLaunches_WithLaunches_ShouldCall_AddWeekAsync_And_SendMail() 
        {
            // Arrange

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, string.Empty);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var launches = PrepareBaseData().Launches.ToList();

            // Simulate that the week does not exist in the database
            _dbServiceMock.Setup(db => db.GetWeek(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync((Week)null);

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()))
                .ReturnsAsync(true);

            // Act
            await rocketLaunchService.AnalyzeAndStoreLaunches(launches, _dateNow);

            // Assert
            _dbServiceMock.Verify(db => db.AddWeekAsync(It.IsAny<Week>()), Times.Once());
            _dbServiceMock.Verify(db => db.UpdateWeekAsync(It.IsAny<Week>()), Times.Once());
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()), Times.Once());
        }

        [Fact]
        public async Task AnalyzeAndStoreLaunches_WithAddedLaunches_ShouldCall_AddLaunchesAsync_And_SendMail()
        {
            // Arrange

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, string.Empty);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var week = PrepareBaseData();

            // Simulate that the week does not exist in the database
            _dbServiceMock.Setup(db => db.GetWeek(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(createWeekCopy(week));

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()))
                .ReturnsAsync(true);

            // Update launch date time the for first launch object 
            var launches = week.Launches.ToList();
            launches.Add(new Launch()
            {
                RocketId = "2",
                T0 = _startDate.AddDays(4).AddHours(14).AddMinutes(20),
                Notified = false,
                RocketName = "Ceres-1",
                Week = week,
                WeekId = week.Id
            });

            // Act
            await rocketLaunchService.AnalyzeAndStoreLaunches(launches, _dateNow);

            // Assert
            _dbServiceMock.Verify(db => db.AddLaunchesAsync(new List<Launch> { launches[2] }), Times.Once());
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>()), Times.Once());
        }

        [Fact]
        public async Task AnalyzeAndStoreLaunches_WithRemovedLaunches_ShouldCall_AddLaunchesAsync_And_SendMail()
        { }

            [Fact]
        public async Task AnalyzeAndStoreLaunches_WithUpdatedLaunches_ShouldCall_GetAllLaunchesForWeek_And_SendMail()
        {
            // Arrange

            var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, string.Empty);
            _httpClientMock = new HttpClient(mockMessageHandler.Object);

            var rocketLaunchService = new RocketLaunchService(_httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance);
            var week = PrepareBaseData();

            // Simulate that the week does not exist in the database
            _dbServiceMock.Setup(db => db.GetWeek(It.IsAny<int>(), It.IsAny<int>()))
                .ReturnsAsync(createWeekCopy(week));

            // Simulate that sending email succeeds
            _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()))
                .ReturnsAsync(true);

            // Update launch date time the for first launch object 
            var launches = week.Launches.ToList();
            launches[0].T0 = launches[0].T0.AddDays(1).AddHours(-3);

            // Act
            await rocketLaunchService.AnalyzeAndStoreLaunches(launches, _dateNow);

            // Assert
            _dbServiceMock.Verify(db => db.UpdateLaunch(launches[0]), Times.Once());
            _mailserviceMock.Verify(m => m.SendMailToRecipients(It.IsAny<Dictionary<string, List<Launch>>>()), Times.Once());
        }

        private Week PrepareBaseData()
        {
            var week = new Week()
            {
                Id = "1",
                WeekNumber = Helper.GetWeekNumber(_startDate),
                Year = _startDate.Year,
                WeekStart = _startDate,
                WeekEnd = _endDate,
                Launches = new List<Launch>(),
                Notified = DateTime.MinValue
            };

            var launch = new Launch()
            {
                RocketId = "1",
                T0 = _startDate.AddDays(3).AddHours(12).AddMinutes(30),
                Notified = false,
                RocketName = "Falcon 9",
                Week = week,
                WeekId = week.Id
            };

            var launch2 = new Launch()
            {
                RocketId = "2",
                T0 = _startDate.AddDays(4).AddHours(14).AddMinutes(20),
                Notified = false,
                RocketName = "Ceres-1",
                Week = week,
                WeekId = week.Id
            };
            week.Launches.Add(launch);
            week.Launches.Add(launch2);

            return week;
        }

        private Week createWeekCopy(Week originalWeek)
        {
            return new Week
            {
                Id = originalWeek.Id,
                WeekNumber = originalWeek.WeekNumber,
                Year = originalWeek.Year,
                WeekStart = originalWeek.WeekStart,
                WeekEnd = originalWeek.WeekEnd,
                Notified = originalWeek.Notified,
                Launches = originalWeek.Launches.Select(l => new Launch
                {
                    Id = l.Id,
                    RocketId = l.RocketId,
                    RocketName = l.RocketName,
                    T0 = l.T0,
                    WeekId = l.WeekId
                }).ToList()
            };
        }

        private Mock<HttpMessageHandler> GetMockMessageHandler(HttpStatusCode statusCode, string responseBody)
        {
            var mockMessageHandler = new Mock<HttpMessageHandler>();
            mockMessageHandler.Protected() // To mock procteded method of SendAsync in HttpMessageHandler class
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(responseBody)
                });

            return mockMessageHandler;
        }

    }
}
