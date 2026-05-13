using CarPark.Shared;

namespace CarPark.Models
{
    public class ParkingLot : BaseEntity
    {
        public string LotCode { get; set; } = string.Empty;

        public string LotName { get; set; } = string.Empty;

        public bool IsAllDay { get; set; } = true;

        public TimeSpan OpenTime { get; set; } = TimeSpan.FromHours(6);

        public TimeSpan CloseTime { get; set; } = TimeSpan.FromHours(22);

        public bool IsActive { get; set; } = true;

        public ICollection<ParkingRateRule> RateRules { get; set; } = new List<ParkingRateRule>();

        public ICollection<ParkingGate> Gates { get; set; } = new List<ParkingGate>();

        public ICollection<ParkingLotSchedule> Schedules { get; set; } = new List<ParkingLotSchedule>();
    }
}