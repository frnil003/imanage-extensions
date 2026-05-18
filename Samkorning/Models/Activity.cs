using System.ComponentModel.DataAnnotations;

namespace Samkorning.Models;

public class Activity
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public CompetitionEvent Event { get; set; } = null!;

    [Required, MaxLength(200)]
    [Display(Name = "Aktivitet")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Beskrivning")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Display(Name = "Tidslucka / match")]
    [MaxLength(100)]
    public string? TimeSlot { get; set; }

    [Display(Name = "Max antal föräldrar")]
    [Range(1, 20)]
    public int MaxParticipants { get; set; } = 1;

    public ICollection<ActivitySignup> Signups { get; set; } = new List<ActivitySignup>();

    public int AvailableSpots => MaxParticipants - Signups.Count;
    public bool IsFull => Signups.Count >= MaxParticipants;
}
