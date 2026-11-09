

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RaceEvent.Data;
using RaceEvent.Models;
using RaceEvent.Models.ViewModels;

namespace RaceEvent.Controllers;

[Authorize(Roles = "Admin,Organiser,Participant")]
public class EnrolmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public EnrolmentsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Participant: enter an event by choosing a category
    [Authorize(Roles = "Admin,Organiser,Participant")]
    public async Task<IActionResult> Create(int eventId)
    {
        var ev = await _db.Events.Include(e => e.Categories).AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();

        var problem = await ValidateCanEnrolAsync(ev);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        return View(BuildViewModel(ev, null));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser,Participant")]
    public async Task<IActionResult> Create(EnrolViewModel model)
    {
        var ev = await _db.Events.Include(e => e.Categories).AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == model.EventId);
        if (ev == null) return NotFound();

        var problem = await ValidateCanEnrolAsync(ev);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction("Details", "Events", new { id = ev.Id });
        }

        // The chosen category must belong to THIS event
        if (model.CategoryId == null || !ev.Categories.Any(c => c.Id == model.CategoryId))
            ModelState.AddModelError(nameof(model.CategoryId), "Please select a valid category for this event.");

        if (!ModelState.IsValid) return View(BuildViewModel(ev, model.CategoryId));

        _db.Enrolments.Add(new Enrolment
        {
            EventId = ev.Id,
            CategoryId = model.CategoryId!.Value,
            ParticipantId = _users.GetUserId(User)!,
            EnrolledOn = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        TempData["Success"] = $"You're entered in {ev.Name}!";
        return RedirectToAction(nameof(My));
    }

    // Participant: my enrolments
    [Authorize(Roles = "Admin,Organiser,Participant")]
    public async Task<IActionResult> My()
    {
        var userId = _users.GetUserId(User);
        var enrolments = await _db.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Category)
            .Include(e => e.Result)
            .AsNoTracking()
            .Where(e => e.ParticipantId == userId)
            .OrderBy(e => e.Event!.Date)
            .ToListAsync();
        return View(enrolments);
    }

    // Participant: withdraw (only before the event and before a result exists)
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser,Participant")]
    public async Task<IActionResult> Withdraw(int id)
    {
        var userId = _users.GetUserId(User);
        var enrolment = await _db.Enrolments
            .Include(e => e.Event).Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.Id == id && e.ParticipantId == userId);
        if (enrolment == null) return NotFound();

        if (enrolment.Result != null || enrolment.Event!.Date <= DateTime.Now)
        {
            TempData["Error"] = "You can no longer withdraw from this event.";
            return RedirectToAction(nameof(My));
        }

        _db.Enrolments.Remove(enrolment);
        await _db.SaveChangesAsync();
        TempData["Success"] = "You've withdrawn from the event.";
        return RedirectToAction(nameof(My));
    }

    // Organiser: all enrolments for one of THEIR events
    [Authorize(Roles = "Admin,Organiser,Participant")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var userId = _users.GetUserId(User);
        var ev = await _db.Events.AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId && e.OrganiserId == userId);
        if (ev == null) return NotFound();

        ViewBag.Event = ev;
        var enrolments = await _db.Enrolments
            .Include(e => e.Participant)
            .Include(e => e.Category)
            .AsNoTracking()
            .Where(e => e.EventId == eventId)
            .OrderBy(e => e.Category!.Name).ThenBy(e => e.Participant!.LastName)
            .ToListAsync();
        return View(enrolments);
    }

    private async Task<string?> ValidateCanEnrolAsync(Event ev)
    {
        if (ev.Date <= DateTime.Now) return "This event has already taken place.";
        if (!ev.Categories.Any()) return "The organiser hasn't added any categories for this event yet.";

        var userId = _users.GetUserId(User);
        if (await _db.Enrolments.AnyAsync(e => e.EventId == ev.Id && e.ParticipantId == userId))
            return "You are already enrolled in this event.";

        return null;
    }

    private static EnrolViewModel BuildViewModel(Event ev, int? selectedCategoryId) => new()
    {
        EventId = ev.Id,
        EventName = ev.Name,
        EventDate = ev.Date,
        Location = ev.Location,
        CategoryId = selectedCategoryId,
        Categories = ev.Categories
            .OrderBy(c => c.Kind).ThenBy(c => c.Name)
            .Select(c => new SelectListItem($"{c.Name} ({c.Kind})", c.Id.ToString()))
    };
}
