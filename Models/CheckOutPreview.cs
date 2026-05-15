namespace CarPark.Models
{
    public record CheckOutPreview(
        ParkingTransaction Transaction,
        List<ParkingCondition> ApplicableConditions,
        decimal NormalCharge
    );
}