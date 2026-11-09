using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceEvent.Data;
using RaceEvent.Models;

namespace RaceEvent.Controllers;

[Authorize]
public class CategoriesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public CategoriesController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    // Both roles: view categories for an event
    public async Task<IActionResult> Index(int eventId)
    {
        var ev = await _db.Events.AsNoTracking().FirstOrDefaultAsync(e => e.Id == eventId);
        if (ev == null) return NotFound();

        ViewBag.Event = ev;
        ViewBag.IsOwner = ev.OrganiserId == _users.GetUserId(User);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => c.EventId == eventId)
            .OrderBy(c => c.Kind).ThenBy(c => c.Name)
            .ToListAsync();
        return View(categories);
    }

    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create(int eventId)
    {
        var ev = await FindOwnedEventAsync(eventId);
        if (ev == null) return NotFound();

        ViewBag.EventName = ev.Name;
        return View(new Category { EventId = eventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Create([Bind("EventId,Name,Kind")] Category category)
    {
        var ev = await FindOwnedEventAsync(category.EventId);
        if (ev == null) return NotFound();

        if (await _db.Categories.AnyAsync(c => c.EventId == category.EventId && c.Name == category.Name))
            ModelState.AddModelError(nameof(Category.Name), "This event already has a category with that name.");

        if (!ModelState.IsValid)
        {
            ViewBag.EventName = ev.Name;
            return View(category);
        }

        _db.Add(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Category added.";
        return RedirectToAction(nameof(Index), new { eventId = category.EventId });
    }
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();
        var category = await FindOwnedCategoryAsync(id.Value);
        if (category == null) return NotFound();

        ViewBag.EventName = category.Event!.Name;
        return View(category);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Edit(int id)
    {
        var category = await FindOwnedCategoryAsync(id);
        if (category == null) return NotFound();

        var updated = await TryUpdateModelAsync(category, "", c => c.Name, c => c.Kind);

        if (updated && await _db.Categories.AnyAsync(c =>
                c.EventId == category.EventId && c.Name == category.Name && c.Id != id))
        {
            ModelState.AddModelError(nameof(Category.Name), "This event already has a category with that name.");
            updated = false;
        }

        if (!updated)
        {
            ViewBag.EventName = category.Event!.Name;
            return View(category);
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index), new { eventId = category.EventId });
    }

    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();
        var category = await FindOwnedCategoryAsync(id.Value);
        return category == null ? NotFound() : View(category);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Organiser")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var category = await FindOwnedCategoryAsync(id);
        if (category == null) return NotFound();

        if (await _db.Enrolments.AnyAsync(e => e.CategoryId == id))
        {
            TempData["Error"] = "Participants are enrolled in this category, so it cannot be deleted.";
            return RedirectToAction(nameof(Index), new { eventId = category.EventId });
        }

        var eventId = category.EventId;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Category deleted.";
        return RedirectToAction(nameof(Index), new { eventId });
    }

    private Task<Event?> FindOwnedEventAsync(int eventId)
    {
        var userId = _users.GetUserId(User);
        return _db.Events.FirstOrDefaultAsync(e => e.Id == eventId && e.OrganiserId == userId);
    }

    private Task<Category?> FindOwnedCategoryAsync(int id)
    {
        var userId = _users.GetUserId(User);
        return _db.Categories
            .Include(c => c.Event)
            .FirstOrDefaultAsync(c => c.Id == id && c.Event!.OrganiserId == userId);
    }
}
