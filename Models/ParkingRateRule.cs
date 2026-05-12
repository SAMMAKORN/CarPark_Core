using CarPark.Shared;
using CarPark.Shared.Enums;

namespace CarPark.Models
{
    public class ParkingRateRule : BaseEntity
    {
        public Guid ParkingLotId { get; set; }

        public string RuleName { get; set; } = string.Empty;

        public int Sequence { get; set; }

        public int StartMinute { get; set; }

        public int? EndMinute { get; set; }

        public ParkingRateCalculationType CalculationType { get; set; }

        public decimal Amount { get; set; }

        public int? BillingStepMinutes { get; set; }

        public bool ApplyOnOvernight { get; set; }

        public bool IsActive { get; set; } = true;

        public ParkingLot? ParkingLot { get; set; }
    }
}