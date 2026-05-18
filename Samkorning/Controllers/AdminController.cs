using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samkorning.Data;
using Samkorning.Models;
using Samkorning.Models.ViewModels;

namespace Samkorning.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        int minObligation = _config.GetValue<int>("MinDriverObligation", 2);
        var events = await _db.Events
            .Include(e => e.Bookings)
            .OrderBy(e => e.Date)
            .ToListAsync();

        var allUsers = _userManager.Users.ToList().Where(u => !string.IsNullOrEmpty(u.FamilyName));

        var driverCounts = await _db.Bookings
            .Where(b => b.Role == BookingRole.Driver)
            .GroupBy(b => b.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var vm = new AdminDashboardViewModel
        {
            TotalEvents = events.Count,
            UpcomingEvents = events.Count(e => e.Date >= DateTime.Today),
            MinDriverObligation = minObligation,
            EventSummaries = events.Select(e => new EventSummary
            {
                Event = e,
                DriverCount = e.Bookings.Count(b => b.Role == BookingRole.Driver),
                RequiredDrivers = e.RequiredDrivers,
                TotalPassengers = e.Bookings.Where(b => b.Role == BookingRole.Passenger).Sum(b => b.FamilySize)
            }).ToList(),
            FamilyObligations = allUsers.Select(u => new FamilyObligation
            {
                UserId = u.Id,
                FamilyName = u.FamilyName,
                Email = u.Email ?? "",
                DriverCount = driverCounts.GetValueOrDefault(u.Id, 0),
                MetObligation = driverCounts.GetValueOrDefault(u.Id, 0) >= minObligation
            }).OrderBy(f => f.FamilyName).ToList()
        };

        return View(vm);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View(new CompetitionEvent { Date = DateTime.Today.AddDays(7), RequiredDrivers = 4 });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(CompetitionEvent model)
    {
        ModelState.Remove("Bookings");
        ModelState.Remove("Activities");
        if (!ModelState.IsValid) return View(model);
        _db.Events.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Match skapad!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var evt = await _db.Events.FindAsync(id);
        return evt == null ? NotFound() : View(evt);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(CompetitionEvent model)
    {
        ModelState.Remove("Bookings");
        ModelState.Remove("Activities");
        if (!ModelState.IsValid) return View(model);
        _db.Events.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Match uppdaterad!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var evt = await _db.Events.FindAsync(id);
        if (evt != null)
        {
            _db.Events.Remove(evt);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Match borttagen.";
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> EventBookings(int id)
    {
        var evt = await _db.Events
            .Include(e => e.Bookings)
                .ThenInclude(b => b.User)
            .Include(e => e.Bookings)
                .ThenInclude(b => b.AssignedPassengers)
                    .ThenInclude(p => p.User)
            .Include(e => e.Activities)
                .ThenInclude(a => a.Signups)
                    .ThenInclude(s => s.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null) return NotFound();

        var driverBookings = evt.Bookings.Where(b => b.Role == BookingRole.Driver).ToList();
        var vm = new EventDetailViewModel
        {
            Event = evt,
            Drivers = driverBookings.Select(d => new DriverWithPassengers
            {
                Driver = d,
                Passengers = d.AssignedPassengers.ToList()
            }).ToList(),
            UnassignedPassengers = evt.Bookings
                .Where(b => b.Role == BookingRole.Passenger && b.AssignedToDriverBookingId == null)
                .ToList()
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignPassenger(int passengerId, int driverId)
    {
        var passenger = await _db.Bookings.FindAsync(passengerId);
        if (passenger == null) return NotFound();

        passenger.AssignedToDriverBookingId = driverId == 0 ? null : driverId;
        await _db.SaveChangesAsync();

        return RedirectToAction(nameof(EventBookings), new { id = passenger.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AutoAssign(int eventId)
    {
        var bookings = await _db.Bookings
            .Where(b => b.EventId == eventId)
            .Include(b => b.AssignedPassengers)
            .ToListAsync();

        var drivers = bookings.Where(b => b.Role == BookingRole.Driver).ToList();
        var unassigned = bookings
            .Where(b => b.Role == BookingRole.Passenger && b.AssignedToDriverBookingId == null)
            .ToList();

        foreach (var passenger in unassigned)
        {
            var driver = drivers
                .Where(d => d.AvailableSeats >= passenger.FamilySize)
                .OrderByDescending(d => d.AvailableSeats)
                .FirstOrDefault();

            if (driver != null)
            {
                passenger.AssignedToDriverBookingId = driver.Id;
                driver.AssignedPassengers.Add(passenger);
            }
        }

        await _db.SaveChangesAsync();
        TempData["Success"] = "Passagerare tilldelade automatiskt!";
        return RedirectToAction(nameof(EventBookings), new { id = eventId });
    }

    [HttpGet]
    public async Task<IActionResult> CreateActivity(int eventId)
    {
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return NotFound();
        return View(new Activity { EventId = eventId, MaxParticipants = 1 });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateActivity(Activity model)
    {
        ModelState.Remove("Event");
        ModelState.Remove("Signups");
        if (!ModelState.IsValid) return View(model);
        _db.Activities.Add(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Aktivitet skapad!";
        return RedirectToAction(nameof(EventBookings), new { id = model.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> EditActivity(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        return activity == null ? NotFound() : View(activity);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditActivity(Activity model)
    {
        ModelState.Remove("Event");
        ModelState.Remove("Signups");
        if (!ModelState.IsValid) return View(model);
        _db.Activities.Update(model);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Aktivitet uppdaterad!";
        return RedirectToAction(nameof(EventBookings), new { id = model.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteActivity(int id)
    {
        var activity = await _db.Activities.FindAsync(id);
        if (activity != null)
        {
            var eventId = activity.EventId;
            _db.Activities.Remove(activity);
            await _db.SaveChangesAsync();
            TempData["Success"] = "Aktivitet borttagen.";
            return RedirectToAction(nameof(EventBookings), new { id = eventId });
        }
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Users()
    {
        var allUsers = _userManager.Users.OrderBy(u => u.FamilyName).ToList();
        var adminIds = (await _userManager.GetUsersInRoleAsync("Admin"))
            .Select(u => u.Id)
            .ToHashSet();

        var vm = allUsers.Select(u => new UserRoleRow
        {
            UserId = u.Id,
            FamilyName = u.FamilyName,
            Email = u.Email ?? "",
            IsAdmin = adminIds.Contains(u.Id)
        }).ToList();

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetAdmin(string userId, bool isAdmin)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        var currentUserId = _userManager.GetUserId(User)!;
        if (userId == currentUserId)
        {
            TempData["Error"] = "Du kan inte ändra din egen adminroll.";
            return RedirectToAction(nameof(Users));
        }

        if (isAdmin)
            await _userManager.AddToRoleAsync(user, "Admin");
        else
            await _userManager.RemoveFromRoleAsync(user, "Admin");

        TempData["Success"] = $"{user.FamilyName} är nu {(isAdmin ? "admin" : "vanlig användare")}.";
        return RedirectToAction(nameof(Users));
    }
}
