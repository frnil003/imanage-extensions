namespace Samkorning.Models.ViewModels;

public class EventActivitiesViewModel
{
    public CompetitionEvent Event { get; set; } = null!;
    public List<ActivityGroup> Groups { get; set; } = new();
    public string CurrentUserId { get; set; } = string.Empty;
}

public class ActivityGroup
{
    public string Name { get; set; } = string.Empty;
    public List<ActivityRow> Activities { get; set; } = new();
}

public class ActivityRow
{
    public Activity Activity { get; set; } = null!;
    public bool SignedUp { get; set; }
    public int? MySignupId { get; set; }
}
