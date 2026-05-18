using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samkorning.Data;
using Samkorning.Models;
using Samkorning.Models.ViewModels;

namespace Samkorning.Controllers;

[Authorize]
public class EventsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public EventsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var events = await _db.Events
            .Include(e => e.Bookings)
            .Include(e => e.Activities)
                .ThenInclude(a => a.Signups)
            .OrderBy(e => e.Date)
            .ToListAsync();

        return View(events);
    }

    public async Task<IActionResult> Summary(int? year)
    {
        var currentUserId = _userManager.GetUserId(User)!;
        int minObligation = _config.GetValue<int>("MinDriverObligation", 2);

        var allYears = await _db.Events
            .Select(e => e.Date.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync();

        int selectedYear = year ?? DateTime.Today.Year;
        if (!allYears.Contains(selectedYear) && allYears.Any())
            selectedYear = allYears.First();

        var driverBookings = await _db.Bookings
            .Where(b => b.Role == BookingRole.Driver && b.Event.Date.Year == selectedYear)
            .Include(b => b.User)
            .Include(b => b.Event)
            .ToListAsync();

        // Exkludera bara det seedade systemkontot, inte familjer som råkar vara admin
        var allUsers = _userManager.Users
            .ToList()
            .Where(u => !string.IsNullOrEmpty(u.FamilyName));

        var grouped = driverBookings
            .GroupBy(b => b.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = allUsers.Select(u => new FamilySummaryRow
        {
            UserId = u.Id,
            FamilyName = u.FamilyName,
            Email = u.Email ?? "",
            DriverCount = grouped.GetValueOrDefault(u.Id)?.Count ?? 0,
            MetObligation = (grouped.GetValueOrDefault(u.Id)?.Count ?? 0) >= minObligation,
            DrivenEvents = grouped.GetValueOrDefault(u.Id)?
                .OrderBy(b => b.Event.Date)
                .Select(b => new DrivenEvent { EventName = b.Event.Name, Date = b.Event.Date })
                .ToList() ?? new()
        })
        .OrderByDescending(r => r.DriverCount)
        .ThenBy(r => r.FamilyName)
        .ToList();

        var vm = new SummaryViewModel
        {
            Year = selectedYear,
            AvailableYears = allYears.Any() ? allYears : new List<int> { DateTime.Today.Year },
            MinDriverObligation = minObligation,
            CurrentUserId = currentUserId,
            Rows = rows
        };

        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var evt = await _db.Events
            .Include(e => e.Bookings)
                .ThenInclude(b => b.User)
            .Include(e => e.Bookings)
                .ThenInclude(b => b.AssignedPassengers)
                    .ThenInclude(p => p.User)
            .Include(e => e.Activities)
                .ThenInclude(a => a.Signups)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null) return NotFound();

        var driverBookings = evt.Bookings.Where(b => b.Role == BookingRole.Driver).ToList();

        var vm = new EventDetailViewModel
        {
            Event = evt,
            MyBooking = evt.Bookings.FirstOrDefault(b => b.UserId == userId),
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

    public async Task<IActionResult> Carpooling(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        var evt = await _db.Events
            .Include(e => e.Bookings)
                .ThenInclude(b => b.User)
            .Include(e => e.Bookings)
                .ThenInclude(b => b.AssignedPassengers)
                    .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (evt == null) return NotFound();

        var driverBookings = evt.Bookings.Where(b => b.Role == BookingRole.Driver).ToList();

        var vm = new EventDetailViewModel
        {
            Event = evt,
            MyBooking = evt.Bookings.FirstOrDefault(b => b.UserId == userId),
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
}
