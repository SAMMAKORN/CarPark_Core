using CarPark.Shared;

namespace CarPark.Models
{
    public class ParkingLot : BaseEntity
    {
        public string LotCode { get; set; } = string.Empty;

        public string LotName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<ParkingRateRule> RateRules { get; set; } = new List<ParkingRateRule>();
    }
}