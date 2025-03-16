using LaunchService.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService.Services
{
    public interface ILaunchDbService
    {
        Task AddLaunchAsync(Launch launch);
        Task AddLaunchesAsync(List<Launch> launches);
        Task<Launch> GetLaunchById(string id);
        Task RemoveLaunchAsync(Launch launch);
        Task RemoveLaunchesAsync(List<Launch> launches);
        Task UpdateLaunch(Launch launch);

        Task AddWeekAsync(Week week);
        Task<Week> GetWeek(int weekNumber, int year);
        Task UpdateWeekAsync(Week week);
        Task<List<Launch>> GetAllLaunchesForWeek(int weekNumber, int year);
    }

    public class LaunchDbService : ILaunchDbService
    {
        private LaunchDbContext _dbContext;

        public LaunchDbService(LaunchDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        #region Launch
        public async Task AddLaunchAsync(Launch launch)
        {
            if(launch == null) throw new ArgumentNullException(nameof(launch));

            var Launch = _dbContext.Launches.Add(launch);
            await _dbContext.SaveChangesAsync();
        }

        public async Task AddLaunchesAsync(List<Launch> launches)
        {
            if (launches.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(launches));

            _dbContext.Launches.AddRange(launches);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Launch> GetLaunchById(string id)
        {
            return await _dbContext.Launches.SingleOrDefaultAsync(i => i.Id == id);
        }

        public async Task RemoveLaunchAsync(Launch launch)
        {
            if (launch == null)
                throw new ArgumentNullException(nameof(launch));

            _dbContext.Launches.Remove(launch);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveLaunchesAsync(List<Launch> launches)
        {
            if (launches.IsNullOrEmpty())
                throw new ArgumentNullException(nameof(launches));

            _dbContext.RemoveRange(launches);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateLaunch(Launch launch)
        {
            if (launch == null)
                throw new ArgumentNullException(nameof(launch));

            _dbContext.Launches.Update(launch);
            await _dbContext.SaveChangesAsync();
        }

        #endregion

        #region Week

        public async Task AddWeekAsync(Week week)
        {
            if (week == null)
                throw new ArgumentNullException(nameof(week));

            _dbContext.Weeks.Add(week);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<Week> GetWeek(int weekNumber, int year)
        {
            return await _dbContext.Weeks.SingleOrDefaultAsync(w => w.WeekNumber == weekNumber && w.Year == year);
        }

        public async Task<List<Launch>> GetAllLaunchesForWeek(int weekNumber, int year)
        {
            return await _dbContext.Launches
                .Where(l => l.Week.WeekNumber == weekNumber && l.Week.Year == year)
                .ToListAsync();
        }

        public async Task UpdateWeekAsync(Week week)
        {
            if (week == null)
                throw new ArgumentNullException(nameof(week));

            _dbContext.Weeks.Update(week);
            await _dbContext.SaveChangesAsync();
        }

        #endregion
    }
}
