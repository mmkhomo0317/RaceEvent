using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RaceEvent.Models.ViewModels;

public class EnrolViewModel
{
    public int EventId { get; set; }
    public string EventName { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a category.")]
    [Display(Name = "Category")]
    public int? CategoryId { get; set; }

    public IEnumerable<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
}
