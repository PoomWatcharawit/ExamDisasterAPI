using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DisasterAPI.Models
{
    public class Regions
    {
        [Key]
        public string RegionID { get; set; }
        public LocationCoordinates LocationCoordinates { get; set; }
        public List<string> DisasterTypes { get; set; }
    }

    [Owned]
    public class LocationCoordinates
    {
        [NotMapped]
        public string? ID { get; set; } = Guid.NewGuid().ToString();
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AlertSetting
    {
        [Key]
        public string? ID { get; set; } = Guid.NewGuid().ToString();
        public string RegionID { get; set; }
        public string DisasterType { get; set; }
        public int ThresholdScore { get; set; }
    }
    public enum RiskLevel
    {
        Low,
        Medium,
        High
    }

    public class DisasterRiskReport
    {
        [Key]
        public string? ID { get; set; } = Guid.NewGuid().ToString();
        public string RegionID { get; set; }
        public string DisasterType { get; set; }
        public int RiskScore { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public bool AlertTriggered { get; set; }
    }

    public class Alert
    {
        [Key]
        public string AlertID { get; set; }
        public string RegionID { get; set; }
        public string DisasterType { get; set; }
        public RiskLevel RiskLevel { get; set; }
        public string AlertMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Email { get; set; }
        public DateTime CreateAt { get; set; }
    }
}
