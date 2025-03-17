using Azure.Core;
using LaunchService.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService
{
    public class Helper
    {
        public static (DateTime startDate, DateTime endDate) GetNextWeekRange(DateTime targetDate)
        {
            int daysUntilNextMonday = ((int)DayOfWeek.Monday - (int)targetDate.DayOfWeek + 7) % 7;
            if (daysUntilNextMonday == 0) daysUntilNextMonday = 7; 

            DateTime nextMonday = targetDate.AddDays(daysUntilNextMonday).Date; // Set to 00:00 time
            DateTime nextSunday = nextMonday.AddDays(6).Date.AddHours(23).AddMinutes(59).AddSeconds(59); // End of Sunday

            return (nextMonday, nextSunday);
        }

        public static int GetWeekNumber(DateTime date)
        {
            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                date,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday);
        }
    }
}
