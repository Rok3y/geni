using LaunchService.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService.test
{
    class RocketLaunchIntegrationtests
    {
        //[Fact]
        //public async Task AnalyzeAndStoreLaunches_OnUpdatedWeek_ShouldNotCall_AddWeekAsync_But_Should_GetAllLaunchesForWeek()
        //{
        //    // Arrange
        //    // Seed database with the initial API response
        //    await ProcessMockApiResponse("response1.json");

        //    // Simulate that the week does not exist in the database
        //    //_dbServiceMock.Setup(db => db.GetWeek(It.IsAny<int>(), It.IsAny<int>()))
        //    //    .ReturnsAsync((Week)null);

        //    // Simulate that sending email succeeds
        //    _mailserviceMock.Setup(m => m.SendMailToRecipients(It.IsAny<List<Launch>>()))
        //        .ReturnsAsync(true);

        //    // Act
        //    await ProcessMockApiResponse("response1_updated_launch.json");

        //    // Assert
        //    _dbServiceMock.Verify(db => db.AddWeekAsync(It.IsAny<Week>()), Times.Never());
        //    _dbServiceMock.Verify(db => db.GetAllLaunchesForWeek(It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        //}

        //private async Task ProcessMockApiResponse(string responseString)
        //{
        //    string testDataFilePath = Path.Combine(testFolder, responseString);
        //    Assert.True(File.Exists(testDataFilePath));
        //    string responseBody = File.ReadAllText(testDataFilePath);

        //    var mockMessageHandler = GetMockMessageHandler(HttpStatusCode.OK, responseBody);
        //    _httpClientMock = new HttpClient(mockMessageHandler.Object);

        //    var rocketLaunchService = new RocketLaunchService(
        //        _httpClientMock, _dbServiceMock.Object, _configurationMock, _mailserviceMock.Object, NullLoggerFactory.Instance
        //    );

        //    var launches = await rocketLaunchService.FetchLaunches(_dateNow);
        //    await rocketLaunchService.AnalyzeAndStoreLaunches(launches, _dateNow);
        //}
    }
}
