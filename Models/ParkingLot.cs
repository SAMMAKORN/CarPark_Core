using CarPark.Shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Models
{
    public class ParkingLot : BaseEntity
    {
        public string LotName { get; set; } = string.Empty;

        public bool IsAllDay { get; set; } = true;

        [Column(TypeName = "time")]
        public TimeSpan OpenTime { get; set; } = TimeSpan.FromHours(6);

        [Column(TypeName = "time")]
        public TimeSpan CloseTime { get; set; } = TimeSpan.FromHours(22);

        [Column(TypeName = "time")]
        public TimeSpan? BillingStartTime { get; set; }

        [Column(TypeName = "time")]
        public TimeSpan? BillingEndTime { get; set; }

        public bool HasOvernightPenalty { get; set; } = false;

        public decimal OvernightPenaltyAmount { get; set; } = 0;

        public ICollection<ParkingRateRule> RateRules { get; set; } = new List<ParkingRateRule>();

        public ICollection<ParkingGate> Gates { get; set; } = new List<ParkingGate>();

        public ICollection<ParkingLotSchedule> Schedules { get; set; } = new List<ParkingLotSchedule>();
    }
}