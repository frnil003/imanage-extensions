using System.ComponentModel.DataAnnotations;

namespace Samkorning.Models;

public class CompetitionEvent
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    [Display(Name = "Namn")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Datum")]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Display(Name = "Plats")]
    [MaxLength(300)]
    public string? Location { get; set; }

    [Display(Name = "Beskrivning")]
    public string? Description { get; set; }

    [Display(Name = "Antal bilar som behövs")]
    [Range(1, 20)]
    public int RequiredDrivers { get; set; } = 4;

    public ICollection<RideBooking> Bookings { get; set; } = new List<RideBooking>();
    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public int DriverCount => Bookings.Count(b => b.Role == BookingRole.Driver);
    public int PassengerCount => Bookings.Where(b => b.Role == BookingRole.Passenger).Sum(b => b.FamilySize);
    public bool IsFull => DriverCount >= RequiredDrivers;
    public bool IsUpcoming => Date >= DateTime.Today;
}
