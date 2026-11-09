using RaceEvent.Data;
using System.ComponentModel.DataAnnotations;

namespace RaceEvent.Models;

public enum EventType
{
    Run,
    Walk,
    Sprint
}

public class Event
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [DataType(DataType.DateTime)]
    [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd HH:mm}", ApplyFormatInEditMode = true)]
    public DateTime Date { get; set; }

    [Required, StringLength(200)]
    public string Location { get; set; } = string.Empty;

    [Range(0.1, 1000, ErrorMessage = "Distance must be between 0.1 and 1000 km.")]
    [Display(Name = "Distance (km)")]
    public decimal DistanceKm { get; set; }

    [Display(Name = "Event type")]
    public EventType EventType { get; set; }

    [StringLength(500)]
    [Display(Name = "Banner image")]
    public string? BannerImageUrl { get; set; }

    [StringLength(200)]
    //[Display(Name = "Banner blob name")]
    public string? BannerBlobName { get; set; }


    // The organiser who owns this event
    public string OrganiserId { get; set; } = string.Empty;
    public ApplicationUser? Organiser { get; set; }

    public ICollection<Category> Categories { get; set; } = new List<Category>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}
