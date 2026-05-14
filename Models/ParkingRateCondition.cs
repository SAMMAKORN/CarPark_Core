using CarPark.Shared;

namespace CarPark.Models
{
    public class ParkingRateCondition : BaseEntity
    {
        public Guid ParkingRateRuleId { get; set; }

        public string ConditionName { get; set; } = string.Empty;

        public ParkingRateRule? Rule { get; set; }
    }
}