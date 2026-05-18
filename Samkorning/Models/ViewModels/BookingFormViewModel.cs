using System.ComponentModel.DataAnnotations;

namespace Samkorning.Models.ViewModels;

public class BookingFormViewModel
{
    public int? Id { get; set; }
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }

    [Required]
    [Display(Name = "Jag vill")]
    public BookingRole Role { get; set; }

    [Display(Name = "Antal platser i bilen (inkl. förare)")]
    [Range(2, 9, ErrorMessage = "Ange 2–9 platser")]
    public int SeatsAvailable { get; set; } = 5;

    [Required]
    [Display(Name = "Antal personer från er familj")]
    [Range(1, 9)]
    public int FamilySize { get; set; } = 1;

    [Display(Name = "Föredragen förare (valfritt)")]
    public int? PreferredDriverBookingId { get; set; }

    [Display(Name = "Anteckningar")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    public List<DriverOption> AvailableDrivers { get; set; } = new();
}

public class DriverOption
{
    public int BookingId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public int AvailableSeats { get; set; }
    public string? Notes { get; set; }
}
