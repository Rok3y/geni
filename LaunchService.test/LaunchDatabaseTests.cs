using LaunchService.Model;
using Microsoft.EntityFrameworkCore;
using LaunchService.Services;

namespace LaunchService.test
{
    public class LaunchDatabaseTests
    {
        private readonly LaunchDbContext _dbContext;
        private readonly ILaunchDbService _launchDbService;

        public LaunchDatabaseTests() 
        {
            _dbContext = GetInMemoryDbContext();
            _launchDbService = new LaunchDbService(_dbContext);
        }

        [Fact]
        public async Task AddLaunch_ShouldAddLaunchToDatabase()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var launch = new Launch()
            {
                RocketId = "1",
                T0 = new DateTime(2025, 5, 10, 12, 30, 0, DateTimeKind.Utc),
                RocketName = "Falcon 9",
                Status = LaunchStatus.GoForLaunch,
                LastUpdated = DateTime.MinValue,
                Week = new Week { Id = "test1", WeekNumber = 12, Year = 2025, WeekStart = DateTime.UtcNow, WeekEnd = DateTime.UtcNow.AddDays(6) }
            };

            // Act
            await _launchDbService.AddLaunchAsync(launch);

            // Assert
            var savedLaunch = await dbContext.Launches.Include(l => l.Week).FirstOrDefaultAsync(l => l.RocketId == "1");
            Assert.NotNull(savedLaunch);
            Assert.Equal("Falcon 9", savedLaunch.RocketName);
            Assert.Equal(12, savedLaunch.Week.WeekNumber);
        }

        [Fact]
        public async Task AddLaunch_ShouldSaveMutipleDataToDatabase()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0, DateTimeKind.Utc));
            var week = new Week()
            {
                Id = "1",
                WeekNumber = Helper.GetWeekNumber(startDate),
                Year = startDate.Year,
                WeekStart = startDate,
                WeekEnd = endDate,
                Launches = new List<Launch>(),
                Notified = DateTime.MinValue
            };

            var launch = new Launch()
            {
                RocketId = "1",
                T0 = startDate.AddDays(3).AddHours(12).AddMinutes(30),
                RocketName = "Falcon 9",
                Status = LaunchStatus.GoForLaunch,
                LastUpdated = DateTime.MinValue,
                Week = week,
                WeekId = week.Id
            };

            var launch2 = new Launch()
            {
                RocketId = "2",
                T0 = startDate.AddDays(4).AddHours(14).AddMinutes(20),
                RocketName = "Ceres-1",
                Status = LaunchStatus.GoForLaunch,
                LastUpdated = DateTime.MinValue,
                Week = week,
                WeekId = week.Id

            };
            week.Launches.Add(launch);
            week.Launches.Add(launch2);

            // Act
            await _launchDbService.AddWeekAsync(week);

            // Assert
            var savedWeek = await dbContext.Weeks.Include(l => l.Launches).FirstOrDefaultAsync(l => l.Id == "1");
            Assert.NotNull(savedWeek);
            Assert.Equal(12, savedWeek.WeekNumber);
            Assert.Equal(2, savedWeek.Launches.Count);

            var savedLaunch = await dbContext.Launches.Include(l => l.Week).FirstOrDefaultAsync(l => l.RocketId == "1");
            Assert.NotNull(savedLaunch);
            Assert.Equal("Falcon 9", savedLaunch.RocketName);
            Assert.Equal(12, savedLaunch.Week.WeekNumber);

            var savedLaunch2 = await dbContext.Launches.Include(l => l.Week).FirstOrDefaultAsync(l => l.RocketId == "2");
            Assert.NotNull(savedLaunch2);
            Assert.Equal("Ceres-1", savedLaunch2.RocketName);
            Assert.Equal(12, savedLaunch2.Week.WeekNumber);
        }

        [Fact]
        public async Task AddLaunch_ShouldUpdateMutipleData()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0, DateTimeKind.Utc));
            var t0 = startDate.AddDays(4).AddHours(14).AddMinutes(20);
            var week = new Week()
            {
                Id = "1",
                WeekNumber = Helper.GetWeekNumber(startDate),
                Year = startDate.Year,
                WeekStart = startDate,
                WeekEnd = endDate,
                Launches = new List<Launch>(),
                Notified = DateTime.MinValue
            };

            var launch = new Launch()
            {
                RocketId = "1",
                T0 = t0,
                RocketName = "Falcon 9",
                Status = LaunchStatus.GoForLaunch,
                LastUpdated = DateTime.MinValue,
                Week = week,
                WeekId = week.Id
            };

            week.Launches.Add(launch);

            await _launchDbService.AddWeekAsync(week);

            // Update launches for this week
            week.Launches.ToList()[0].LastUpdated = DateTime.MinValue.AddDays(5).AddHours(3);
            week.Launches.ToList()[0].T0 = t0.AddDays(3);

            // Act
            await _launchDbService.UpdateWeekAsync(week);



            // Assert
            var savedWeek = await dbContext.Weeks.Include(l => l.Launches).FirstOrDefaultAsync(l => l.Id == "1");
            Assert.NotNull(savedWeek);
            Assert.Equal(12, savedWeek.WeekNumber);
            Assert.Single(savedWeek.Launches);

            var savedLaunch = await dbContext.Launches.Include(l => l.Week).FirstOrDefaultAsync(l => l.RocketId == "1");
            Assert.NotNull(savedLaunch);
            Assert.Equal("Falcon 9", savedLaunch.RocketName);
            Assert.Equal(12, savedLaunch.Week.WeekNumber);
            Assert.Equal(DateTime.MinValue.AddDays(5).AddHours(3), savedLaunch.LastUpdated);
            Assert.Equal(t0.AddDays(3), savedLaunch.T0);
        }

        [Fact]
        public async Task UpdateWeekAsync_ShouldUpdateWeekAccordingly()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0, DateTimeKind.Utc));
            var week = new Week()
            {
                Id = "1",
                WeekNumber = Helper.GetWeekNumber(startDate),
                Year = startDate.Year,
                WeekStart = startDate,
                WeekEnd = endDate,
                Launches = new List<Launch>(),
                Notified = DateTime.MinValue
            };
            await _launchDbService.AddWeekAsync(week);

            var notifiedDate = new DateTime(2025, 3, 15, 12, 0, 0, DateTimeKind.Utc);
            var savedWeek = await _launchDbService.GetWeek(12, 2025);
            Assert.Equal(DateTime.MinValue, savedWeek.Notified);
            savedWeek.Notified = notifiedDate;

            // Act
            await _launchDbService.UpdateWeekAsync(savedWeek);

            // Assert
            var updatedWeek = await dbContext.Weeks.Include(l => l.Launches).FirstOrDefaultAsync(l => l.Id == "1");
            Assert.NotNull(updatedWeek);
            Assert.Equal(notifiedDate, updatedWeek.Notified);
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
    }
}
