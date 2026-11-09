using RaceEvent.Models;

namespace RaceEvent.Models.ViewModels;

// ---------- Organiser ----------
public class OrganiserDashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public int TotalEvents { get; set; }
    public int UpcomingEvents { get; set; }
    public int TotalEnrolments { get; set; }
    public int ResultsCaptured { get; set; }

    public List<OrganiserEventRow> Upcoming { get; set; } = new();
    public List<OrganiserEventRow> AwaitingResults { get; set; } = new();
    public List<RecentEnrolmentRow> RecentEnrolments { get; set; } = new();
}

public class OrganiserEventRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public int CategoryCount { get; set; }
    public int EnrolmentCount { get; set; }
    public int ResultCount { get; set; }
}

public class RecentEnrolmentRow
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public DateTime EnrolledOn { get; set; }
}

// ---------- Participant ----------
public class ParticipantDashboardViewModel
{
    public string DisplayName { get; set; } = string.Empty;

    public int UpcomingCount { get; set; }
    public int CompletedCount { get; set; }
    public int ResultsCount { get; set; }
    public int? BestPosition { get; set; }

    public ParticipantEnrolmentRow? NextEvent { get; set; }
    public List<ParticipantEnrolmentRow> Upcoming { get; set; } = new();
    public List<ParticipantEnrolmentRow> RecentResults { get; set; } = new();
    public List<OpenEventRow> OpenEvents { get; set; } = new();
}

public class ParticipantEnrolmentRow
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public decimal DistanceKm { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TimeSpan? FinishTime { get; set; }
    public int? Position { get; set; }
}

public class OpenEventRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Location { get; set; } = string.Empty;
    public EventType EventType { get; set; }
    public decimal DistanceKm { get; set; }
}
