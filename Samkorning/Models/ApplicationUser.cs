using Microsoft.AspNetCore.Identity;

namespace Samkorning.Models;

public class ApplicationUser : IdentityUser
{
    public string FamilyName { get; set; } = string.Empty;
    public ICollection<RideBooking> Bookings { get; set; } = new List<RideBooking>();
}
