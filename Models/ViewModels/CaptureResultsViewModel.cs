using System.ComponentModel.DataAnnotations;

namespace RaceEvent.Models.ViewModels;

public class CaptureResultsViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public List<ResultRow> Rows { get; set; } = new();
}

public class ResultRow
{
    public int EnrolmentId { get; set; }
    public string ParticipantName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;

    [Display(Name = "Finish time")]
    public TimeSpan? FinishTime { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Position")]
    public int? Position { get; set; }
}
