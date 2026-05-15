using System.ComponentModel.DataAnnotations.Schema;

namespace CarPark.Models
{
    public class ParkingConditionLot
    {
        [ForeignKey(nameof(Condition))]
        public Guid ParkingConditionId { get; set; }

        [ForeignKey(nameof(ParkingLot))]
        public Guid ParkingLotId { get; set; }

        public ParkingCondition? Condition { get; set; }

        public ParkingLot? ParkingLot { get; set; }
    }
}