namespace Samkorning.Models.ViewModels;

public class SummaryViewModel
{
    public int Year { get; set; }
    public List<int> AvailableYears { get; set; } = new();
    public int MinDriverObligation { get; set; }
    public string? CurrentUserId { get; set; }
    public List<FamilySummaryRow> Rows { get; set; } = new();
}

public class FamilySummaryRow
{
    public string UserId { get; set; } = string.Empty;
    public string FamilyName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int DriverCount { get; set; }
    public bool MetObligation { get; set; }
    public List<DrivenEvent> DrivenEvents { get; set; } = new();
}

public class DrivenEvent
{
    public string EventName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}
