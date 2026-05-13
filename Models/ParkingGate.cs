using CarPark.Shared;

namespace CarPark.Models
{
    public class ParkingGate : BaseEntity
    {
        public Guid ParkingLotId { get; set; }

        public string GateName { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ParkingLot? ParkingLot { get; set; }
    }
}