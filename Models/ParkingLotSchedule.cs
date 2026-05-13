using CarPark.Shared;

namespace CarPark.Models
{
    public class ParkingLotSchedule : BaseEntity
    {
        public Guid ParkingLotId { get; set; }

        public string ScheduleName { get; set; } = string.Empty;

        /// <summary>วันทำการ — flags ของ WeekDays</summary>
        public int DaysOfWeek { get; set; }

        public bool IsAllDay { get; set; } = false;

        public TimeSpan OpenTime { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan CloseTime { get; set; } = TimeSpan.FromHours(22);

        public bool IsActive { get; set; } = true;

        public ParkingLot? ParkingLot { get; set; }

        public ICollection<ParkingRateRule> RateRules { get; set; } = new List<ParkingRateRule>();
    }
}