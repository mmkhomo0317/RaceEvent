using RaceEvent.Data;
using RaceEvent.Models;
using RaceEvent.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace RaceEvent.Controllers;

[Authorize]
public class ResultsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public ResultsController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Organiser: capture times and positions for all enrolments of one of their events
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Capture(int eventId)
    {
        var ev = await FindOwnedEventAsync(eventId);
        if (ev == null) return NotFound();

        if (ev.Date > DateTime.Now)   // remove this check if you want to capture results early while testing
        {
            TempData["Error"] = "Results can only be captured after the event has taken place.";
            return RedirectToAction("Details", "Events", new { id = eventId });
        }

        return View(await BuildViewModelAsync(ev));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Capture(CaptureResultsViewModel model)
    {
        var ev = await FindOwnedEventAsync(model.EventId);
        if (ev == null) return NotFound();

        if (ev.Date > DateTime.Now)
        {
            TempData["Error"] = "Results can only be captured after the event has taken place.";
            return RedirectToAction("Details", "Events", new { id = ev.Id });
        }

        // Only accept enrolments that really belong to this event
        var enrolments = await _db.Enrolments
            .Include(e => e.Result)
            .Where(e => e.EventId == ev.Id)
            .ToDictionaryAsync(e => e.Id);

        // Validate each row
        for (var i = 0; i < model.Rows.Count; i++)
        {
            var row = model.Rows[i];
            if (!enrolments.ContainsKey(row.EnrolmentId))
            {
                ModelState.AddModelError("", "Invalid enrolment submitted.");
                continue;
            }
            if (row.FinishTime.HasValue != row.Position.HasValue)
                ModelState.AddModelError($"Rows[{i}].Position", "Enter both a finish time and a position (or leave both blank).");
            if (row.FinishTime.HasValue && row.FinishTime.Value <= TimeSpan.Zero)
                ModelState.AddModelError($"Rows[{i}].FinishTime", "Finish time must be greater than zero.");
        }

        // Positions must be unique within the event
        var duplicates = model.Rows.Where(r => r.Position.HasValue)
            .GroupBy(r => r.Position!.Value).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Any())
            ModelState.AddModelError("", $"Duplicate finishing positions: {string.Join(", ", duplicates)}.");

        if (!ModelState.IsValid)
        {
            model.EventName = ev.Name;
            // Restore display-only fields that aren't posted back
            foreach (var row in model.Rows.Where(r => enrolments.ContainsKey(r.EnrolmentId)))
            {
                var en = await _db.Enrolments.Include(e => e.Participant).Include(e => e.Category)
                    .AsNoTracking().FirstAsync(e => e.Id == row.EnrolmentId);
                row.ParticipantName = $"{en.Participant!.FirstName} {en.Participant.LastName}";
                row.CategoryName = en.Category!.Name;
            }
            return View(model);
        }

        // Upsert / remove results
        foreach (var row in model.Rows)
        {
            var enrolment = enrolments[row.EnrolmentId];

            if (row.FinishTime.HasValue && row.Position.HasValue)
            {
                if (enrolment.Result == null)
                {
                    _db.Results.Add(new Result
                    {
                        EnrolmentId = enrolment.Id,
                        FinishTime = row.FinishTime.Value,
                        Position = row.Position.Value
                    });
                }
                else
                {
                    enrolment.Result.FinishTime = row.FinishTime.Value;
                    enrolment.Result.Position = row.Position.Value;
                    enrolment.Result.CapturedOn = DateTime.UtcNow;
                }
            }
            else if (enrolment.Result != null)
            {
                _db.Results.Remove(enrolment.Result);   // cleared by the organiser
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Results saved.";
        return RedirectToAction("ForEvent", "Enrolments", new { eventId = ev.Id });
    }

    // Participant: my own results only
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> My()
    {
        var userId = _users.GetUserId(User);
        var results = await _db.Results
            .Include(r => r.Enrolment!).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment!).ThenInclude(e => e.Category)
            .AsNoTracking()
            .Where(r => r.Enrolment!.ParticipantId == userId)
            .OrderByDescending(r => r.Enrolment!.Event!.Date)
            .ToListAsync();
        return View(results);
    }

    private Task<Event?> FindOwnedEventAsync(int eventId)
    {
        var userId = _users.GetUserId(User);
        return _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId && e.OrganiserId == userId);
    }

    private async Task<CaptureResultsViewModel> BuildViewModelAsync(Event ev)
    {
        var rows = await _db.Enrolments
            .Include(e => e.Participant).Include(e => e.Category).Include(e => e.Result)
            .AsNoTracking()
            .Where(e => e.EventId == ev.Id)
            .OrderBy(e => e.Result == null).ThenBy(e => e.Result!.Position)
            .ThenBy(e => e.Participant!.LastName)
            .Select(e => new ResultRow
            {
                EnrolmentId = e.Id,
                ParticipantName = e.Participant!.FirstName + " " + e.Participant.LastName,
                CategoryName = e.Category!.Name,
                FinishTime = e.Result != null ? e.Result.FinishTime : null,
                Position = e.Result != null ? e.Result.Position : null
            })
            .ToListAsync();

        return new CaptureResultsViewModel { EventId = ev.Id, EventName = ev.Name, Rows = rows };
    }
}
