using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LaunchService.Model
{
    public class Week
    {
        public string Id { get; set; }
        public int WeekNumber { get; set; }
        public int Year { get; set; }
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public DateTime Notified { get; set; }
        public ICollection<Launch> Launches { get; set; } = new List<Launch>();

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;

            if (obj == null || !(obj is Week))
                return false;

            return Equals((Week)obj);
        }

        public bool Equals(Week other)
        {
            if (other == null) return false;

            if (this.Id == other.Id &&
                this.WeekNumber == other.WeekNumber &&
                this.Year == other.Year &&
                this.WeekStart.Equals(other.WeekStart) &&
                this.WeekEnd.Equals(other.WeekEnd) &&
                this.Notified.Equals(other.Notified) && 
                this.Launches == other.Launches)
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, WeekNumber, Year, Notified);
        }

        public override string ToString()
        {
            return $"Week: {WeekNumber}\n" +
                $"Date range: {WeekStart} - {WeekEnd}\n" +
                $"Notified: {Notified}\n" +
                $"Number of launches: {Launches.Count}";
        }
    }
}
