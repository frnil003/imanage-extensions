namespace Samkorning.Models.ViewModels;

public class EventDetailViewModel
{
    public CompetitionEvent Event { get; set; } = null!;
    public List<DriverWithPassengers> Drivers { get; set; } = new();
    public List<RideBooking> UnassignedPassengers { get; set; } = new();
    public RideBooking? MyBooking { get; set; }
}

public class DriverWithPassengers
{
    public RideBooking Driver { get; set; } = null!;
    public List<RideBooking> Passengers { get; set; } = new();
    public int AvailableSeats => Driver.SeatsAvailable - 1 - Passengers.Sum(p => p.FamilySize);
}
