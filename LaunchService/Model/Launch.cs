using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace LaunchService.Model
{
    public enum LaunchStatus
    {
        GoForLaunch = 1,
        LaunchSuccessful = 3,
        LaunchFailure = 4,
        OnHold = 5,
        LaunchInFlight = 6,
        PartialFailure = 7,
        ToBeConfirmed = 8,
        PayloadDeployed = 9,
        ToBeDetermined = 2
    }

    public class Launch
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RocketId { get; set; }
        public LaunchStatus Status { get; set; }
        public DateTime T0 { get; set; }
        public DateTime LastUpdated { get; set; }
        public string RocketName { get; set; }

        public string WeekId { get; set; }
        public Week Week { get; set; } = null!;

        public override bool Equals(object? obj)
        {
            if (obj == null || !(obj is Launch))
                return false;

            return Equals((Launch)obj);
        }

        public bool Equals(Launch other)
        {
            if (other == null) return false;

            if (this.RocketId == other.RocketId &&
                this.Status == other.Status &&
                this.T0.Equals(other.T0) &&
                this.LastUpdated.Equals(other.LastUpdated) &&
                this.RocketName == other.RocketName) 
                return true;

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(this.RocketId, this.T0, this.RocketName);
        }
    }
}
