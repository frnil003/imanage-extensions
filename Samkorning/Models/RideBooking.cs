using System.ComponentModel.DataAnnotations;

namespace Samkorning.Models;

public enum BookingRole
{
    [Display(Name = "Förare")]
    Driver,
    [Display(Name = "Passagerare")]
    Passenger
}

public class RideBooking
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public CompetitionEvent Event { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    [Display(Name = "Roll")]
    public BookingRole Role { get; set; }

    [Display(Name = "Antal platser i bilen (inkl. förare)")]
    [Range(1, 9)]
    public int SeatsAvailable { get; set; } = 5;

    [Display(Name = "Antal personer från familjen")]
    [Range(1, 9)]
    public int FamilySize { get; set; } = 1;

    [Display(Name = "Tilldelad förare")]
    public int? AssignedToDriverBookingId { get; set; }
    public RideBooking? AssignedToDriverBooking { get; set; }

    [Display(Name = "Anteckningar")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<RideBooking> AssignedPassengers { get; set; } = new List<RideBooking>();

    public int AvailableSeats =>
        SeatsAvailable - 1 - AssignedPassengers.Sum(p => p.FamilySize);
}
