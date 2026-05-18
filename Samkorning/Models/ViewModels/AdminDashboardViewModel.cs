namespace Samkorning.Models.ViewModels;

public class AdminDashboardViewModel
{
    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public List<EventSummary> EventSummaries { get; set; } = new();
    public List<FamilyObligation> FamilyObligations { get; set; } = new();
    public int MinDriverObligation { get; set; } = 2;
}

public class EventSummary
{
    public CompetitionEvent Event { get; set; } = null!;
    public int DriverCount { get; set; }
    public int RequiredDrivers { get; set; }
    public int TotalPassengers { get; set; }
    public bool NeedsMoreDrivers => DriverCount < RequiredDrivers;
}

public class UserRoleRow
{
    public string UserId { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
}

public class FamilyObligation
{
    public string UserId { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DriverCount { get; set; }
    public bool MetObligation { get; set; }
}
