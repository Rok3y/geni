using LaunchService.Model;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LaunchService.Services;

namespace LaunchService.test
{
    public class LaunchDatabaseTests
    {
        private LaunchDbContext _dbContext;
        private ILaunchDbService _launchDbService;

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
                T0 = new DateTime(2025, 5, 10, 12, 30, 0),
                Notified = false,
                RocketName = "Falcon 9",
                Week = new Week { Id = "test1", WeekNumber = 12, Year = 2025, WeekStart = DateTime.UtcNow, WeekEnd = DateTime.UtcNow.AddDays(6) }
            };

            // Act
            await _launchDbService.AddLaunchAsync(launch);

            // Assert
            var savedLaunch = dbContext.Launches.Include(l => l.Week).FirstOrDefault(l => l.RocketId == "1");
            Assert.NotNull(savedLaunch);
            Assert.Equal("Falcon 9", savedLaunch.RocketName);
            Assert.Equal(12, savedLaunch.Week.WeekNumber);
        }

        [Fact]
        public async Task AddLaunch_ShouldRemoveLaunchFromDatabase()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var launch = await _launchDbService.GetLaunchById("1");

            // Act
            _launchDbService.RemoveLaunchAsync(launch);

            // Assert
            var savedLaunch = dbContext.Launches.Include(l => l.Week).FirstOrDefault(l => l.Id == "1");
            Assert.Null(savedLaunch);
        }

        [Fact]
        public async Task AddLaunch_ShouldSaveMutipleDataToDatabase()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0));
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
                Notified = false,
                RocketName = "Falcon 9",
                Week = week,
                WeekId = week.Id
            };

            var launch2 = new Launch()
            {
                RocketId = "2",
                T0 = startDate.AddDays(4).AddHours(14).AddMinutes(20),
                Notified = false,
                RocketName = "Ceres-1",
                Week = week,
                WeekId = week.Id

            };
            week.Launches.Add(launch);
            week.Launches.Add(launch2);

            // Act
            await _launchDbService.AddWeekAsync(week);

            // Assert
            var savedWeek = dbContext.Weeks.Include(l => l.Launches).FirstOrDefault(l => l.Id == "1");
            Assert.NotNull(savedWeek);
            Assert.Equal(12, savedWeek.WeekNumber);
            Assert.Equal(2, savedWeek.Launches.Count);

            var savedLaunch = dbContext.Launches.Include(l => l.Week).FirstOrDefault(l => l.RocketId == "1");
            Assert.NotNull(savedLaunch);
            Assert.Equal("Falcon 9", savedLaunch.RocketName);
            Assert.Equal(12, savedLaunch.Week.WeekNumber);

            var savedLaunch2 = dbContext.Launches.Include(l => l.Week).FirstOrDefault(l => l.RocketId == "2");
            Assert.NotNull(savedLaunch2);
            Assert.Equal("Ceres-1", savedLaunch2.RocketName);
            Assert.Equal(12, savedLaunch2.Week.WeekNumber);
        }

        [Fact]
        public async Task AddLaunch_ShouldRemoveMutipleDataFromDatabase()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0));
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
                Notified = false,
                RocketName = "Falcon 9",
                Week = week,
                WeekId = week.Id
            };

            var launch2 = new Launch()
            {
                RocketId = "2",
                T0 = startDate.AddDays(4).AddHours(14).AddMinutes(20),
                Notified = false,
                RocketName = "Ceres-1",
                Week = week,
                WeekId = week.Id

            };
            week.Launches.Add(launch);
            week.Launches.Add(launch2);

            await _launchDbService.AddWeekAsync(week);

            // Act
            await _launchDbService.RemoveLaunchesAsync(week.Launches.ToList());

            // Assert
            var savedWeek = dbContext.Weeks.Include(l => l.Launches).FirstOrDefault(l => l.Id == "1");
            Assert.NotNull(savedWeek);
            Assert.Equal(12, savedWeek.WeekNumber);
            Assert.Empty(savedWeek.Launches);
        }

        [Fact]
        public async Task UpdateWeekAsync_ShouldUpdateWeekAccordingly()
        {
            // Arrange
            using var dbContext = GetInMemoryDbContext();
            var (startDate, endDate) = Helper.GetNextWeekRange(new DateTime(2025, 3, 13, 0, 0, 0));
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

            var notifiedDate = new DateTime(2025, 3, 15, 12, 0, 0);
            var savedWeek = await _launchDbService.GetWeek(12, 2025);
            Assert.Equal(DateTime.MinValue, savedWeek.Notified);
            savedWeek.Notified = notifiedDate;

            // Act
            await _launchDbService.UpdateWeekAsync(savedWeek);

            // Assert
            var updatedWeek = dbContext.Weeks.Include(l => l.Launches).FirstOrDefault(l => l.Id == "1");
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
