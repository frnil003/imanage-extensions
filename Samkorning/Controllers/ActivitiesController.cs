using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samkorning.Data;
using Samkorning.Models;
using Samkorning.Models.ViewModels;

namespace Samkorning.Controllers;

[Authorize]
public class ActivitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public ActivitiesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Event(int eventId)
    {
        var userId = _userManager.GetUserId(User)!;

        var evt = await _db.Events
            .Include(e => e.Activities)
                .ThenInclude(a => a.Signups)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (evt == null) return NotFound();

        var mySignupIds = evt.Activities
            .SelectMany(a => a.Signups)
            .Where(s => s.UserId == userId)
            .ToDictionary(s => s.ActivityId, s => s.Id);

        var groups = evt.Activities
            .OrderBy(a => a.Name)
            .ThenBy(a => a.TimeSlot)
            .GroupBy(a => a.Name)
            .Select(g => new ActivityGroup
            {
                Name = g.Key,
                Activities = g.Select(a => new ActivityRow
                {
                    Activity = a,
                    SignedUp = mySignupIds.ContainsKey(a.Id),
                    MySignupId = mySignupIds.GetValueOrDefault(a.Id)
                }).ToList()
            })
            .ToList();

        var vm = new EventActivitiesViewModel
        {
            Event = evt,
            Groups = groups,
            CurrentUserId = userId
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SignUp(int activityId)
    {
        var userId = _userManager.GetUserId(User)!;

        var activity = await _db.Activities
            .Include(a => a.Signups)
            .FirstOrDefaultAsync(a => a.Id == activityId);

        if (activity == null) return NotFound();

        if (activity.IsFull)
        {
            TempData["Error"] = "Den här aktiviteten är redan full.";
            return RedirectToAction(nameof(Event), new { eventId = activity.EventId });
        }

        var already = activity.Signups.Any(s => s.UserId == userId);
        if (!already)
        {
            _db.ActivitySignups.Add(new ActivitySignup
            {
                ActivityId = activityId,
                UserId = userId
            });
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Du är anmäld till {activity.Name}!";
        }

        return RedirectToAction(nameof(Event), new { eventId = activity.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int signupId)
    {
        var userId = _userManager.GetUserId(User)!;

        var signup = await _db.ActivitySignups
            .Include(s => s.Activity)
            .FirstOrDefaultAsync(s => s.Id == signupId);

        if (signup == null) return NotFound();
        if (signup.UserId != userId && !User.IsInRole("Admin")) return Forbid();

        var eventId = signup.Activity.EventId;
        _db.ActivitySignups.Remove(signup);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Avanmälan genomförd.";
        return RedirectToAction(nameof(Event), new { eventId });
    }
}
