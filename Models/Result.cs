using RaceEvent.Models;
using System.ComponentModel.DataAnnotations;

namespace RaceEvent.Models;

public class Result
{
    public int Id { get; set; }

    public int EnrolmentId { get; set; }
    public Enrolment? Enrolment { get; set; }

    [Display(Name = "Finish time")]
    public TimeSpan FinishTime { get; set; }

    [Range(1, 100000)]
    [Display(Name = "Finishing position")]
    public int Position { get; set; }

    public DateTime CapturedOn { get; set; } = DateTime.UtcNow;
}
