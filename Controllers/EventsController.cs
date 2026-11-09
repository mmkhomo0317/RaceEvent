
using RaceEvent.Data;
using RaceEvent.Models;
using RaceEvent.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace RaceEvent.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IBlobStorageService _blobs;

    public EventsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IBlobStorageService blobs)
    {
        _db = db;
        _users = users;
        _blobs = blobs;
    }

    // Both roles
    public async Task<IActionResult> Index()
    {
        ViewBag.UserId = _users.GetUserId(User);

        var events = await _db.Events
            .Include(e => e.Organiser)
            .AsNoTracking()
            .OrderBy(e => e.Date)
            .ToListAsync();

        return View(events);
    }

    // Both roles
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await _db.Events
            .Include(e => e.Organiser)
            .Include(e => e.Categories)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (ev == null)
            return NotFound();

        var userId = _users.GetUserId(User);

        ViewBag.IsOwner = ev.OrganiserId == userId;

        ViewBag.IsEnrolled = await _db.Enrolments
            .AnyAsync(en =>
                en.EventId == ev.Id &&
                en.ParticipantId == userId);

        return View(ev);
    }

    // Admin and Organiser
    [Authorize(Roles = "Admin,Organiser")]
    public IActionResult Create()
    {
        return View(new Event
        {
            Date = DateTime.Today.AddDays(30).AddHours(8)
        });
    }

    // Create event
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(
        [Bind("Name,Description,Date,Location,DistanceKm,EventType")] Event ev,
        IFormFile? bannerFile)
    {
        var hasBanner = bannerFile is { Length: > 0 };

        if (hasBanner)
        {
            var error = _blobs.Validate(bannerFile!);

            if (error != null)
                ModelState.AddModelError("BannerFile", error);
        }

        if (!ModelState.IsValid)
            return View(ev);

        ev.OrganiserId = _users.GetUserId(User)!;

        BlobUploadResult? uploaded = null;

        try
        {
            if (hasBanner)
            {
                uploaded = await _blobs.UploadBannerAsync(bannerFile!);

                ev.BannerBlobName = uploaded.BlobName;
                ev.BannerImageUrl = uploaded.Url;
            }

            _db.Events.Add(ev);

            await _db.SaveChangesAsync();
        }
        catch
        {
            // Remove uploaded image if database save fails
            if (uploaded != null)
                await _blobs.DeleteAsync(uploaded.BlobName);

            throw;
        }

        TempData["Success"] =
            "Event created. Now add the categories for it.";

        return RedirectToAction(
            "Index",
            "Categories",
            new { eventId = ev.Id });
    }

    // Edit event - GET
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // Edit event - POST
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5_000_000)]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(
        int id,
        IFormFile? bannerFile,
        bool removeBanner)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        var hasBanner = bannerFile is { Length: > 0 };

        if (hasBanner)
        {
            var error = _blobs.Validate(bannerFile!);

            if (error != null)
                ModelState.AddModelError("bannerFile", error);
        }

        // Only update permitted event properties
        var updated = await TryUpdateModelAsync(
            ev,
            "",
            e => e.Name,
            e => e.Description,
            e => e.Date,
            e => e.Location,
            e => e.DistanceKm,
            e => e.EventType);

        if (!updated || !ModelState.IsValid)
            return View(ev);

        var oldBlobName = ev.BannerBlobName;

        BlobUploadResult? uploaded = null;

        try
        {
            if (hasBanner)
            {
                uploaded = await _blobs.UploadBannerAsync(bannerFile!);

                ev.BannerBlobName = uploaded.BlobName;
                ev.BannerImageUrl = uploaded.Url;
            }
            else if (removeBanner)
            {
                ev.BannerBlobName = null;
                ev.BannerImageUrl = null;
            }

            await _db.SaveChangesAsync();
        }
        catch
        {
            // Delete new image if database update fails
            if (uploaded != null)
                await _blobs.DeleteAsync(uploaded.BlobName);

            throw;
        }

        // Delete old image after successful database update
        if ((uploaded != null || removeBanner) &&
            !string.IsNullOrWhiteSpace(oldBlobName))
        {
            await _blobs.DeleteAsync(oldBlobName);
        }

        TempData["Success"] = "Event updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Delete event - GET
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
            return NotFound();

        var ev = await FindOwnedEventAsync(id.Value);

        if (ev == null)
            return NotFound();

        return View(ev);
    }

    // Delete event - POST
    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var ev = await FindOwnedEventAsync(id);

        if (ev == null)
            return NotFound();

        if (await _db.Enrolments.AnyAsync(e => e.EventId == id))
        {
            TempData["Error"] =
                "This event has enrolments and cannot be deleted.";

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var blobName = ev.BannerBlobName;

        _db.Events.Remove(ev);

        // Save database changes first
        await _db.SaveChangesAsync();

        // Delete associated Azure image
        await _blobs.DeleteAsync(blobName);

        TempData["Success"] = "Event deleted successfully.";

        return RedirectToAction(nameof(Index));
    }

    // Find event belonging to current organiser
    private Task<Event?> FindOwnedEventAsync(int id)
    {
        var userId = _users.GetUserId(User);

        return _db.Events.FirstOrDefaultAsync(
            e => e.Id == id && e.OrganiserId == userId);
    }
}
