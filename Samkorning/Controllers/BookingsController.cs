using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samkorning.Data;
using Samkorning.Models;
using Samkorning.Models.ViewModels;

namespace Samkorning.Controllers;

[Authorize]
public class BookingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;

    public BookingsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IConfiguration config)
    {
        _db = db;
        _userManager = userManager;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Create(int eventId)
    {
        var evt = await _db.Events.FindAsync(eventId);
        if (evt == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        var existing = await _db.Bookings
            .FirstOrDefaultAsync(b => b.EventId == eventId && b.UserId == userId);

        if (existing != null)
            return RedirectToAction("Carpooling", "Events", new { id = eventId });

        var vm = await BuildFormViewModel(eventId, null);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingFormViewModel vm)
    {
        if (vm.Role == BookingRole.Driver && vm.SeatsAvailable < 2)
            ModelState.AddModelError("SeatsAvailable", "Förare måste ha minst 2 platser.");

        if (!ModelState.IsValid)
        {
            vm = await BuildFormViewModel(vm.EventId, vm);
            return View(vm);
        }

        var userId = _userManager.GetUserId(User)!;

        var booking = new RideBooking
        {
            EventId = vm.EventId,
            UserId = userId,
            Role = vm.Role,
            SeatsAvailable = vm.Role == BookingRole.Driver ? vm.SeatsAvailable : 0,
            FamilySize = vm.FamilySize,
            Notes = vm.Notes,
            AssignedToDriverBookingId = vm.Role == BookingRole.Passenger ? vm.PreferredDriverBookingId : null
        };

        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Din anmälan är sparad!";
        return RedirectToAction("Details", "Events", new { id = vm.EventId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var booking = await _db.Bookings
            .Include(b => b.Event)
            .FirstOrDefaultAsync(b => b.Id == id);

        if (booking == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (booking.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var vm = await BuildFormViewModel(booking.EventId, null);
        vm.Id = booking.Id;
        vm.Role = booking.Role;
        vm.SeatsAvailable = booking.SeatsAvailable;
        vm.FamilySize = booking.FamilySize;
        vm.Notes = booking.Notes;
        vm.PreferredDriverBookingId = booking.AssignedToDriverBookingId;

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(BookingFormViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            vm = await BuildFormViewModel(vm.EventId, vm);
            return View(vm);
        }

        var booking = await _db.Bookings.FindAsync(vm.Id);
        if (booking == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (booking.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        booking.Role = vm.Role;
        booking.SeatsAvailable = vm.Role == BookingRole.Driver ? vm.SeatsAvailable : 0;
        booking.FamilySize = vm.FamilySize;
        booking.Notes = vm.Notes;
        booking.AssignedToDriverBookingId = vm.Role == BookingRole.Passenger ? vm.PreferredDriverBookingId : null;

        await _db.SaveChangesAsync();

        TempData["Success"] = "Anmälan uppdaterad!";
        return RedirectToAction("Details", "Events", new { id = booking.EventId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var booking = await _db.Bookings.FindAsync(id);
        if (booking == null) return NotFound();

        var userId = _userManager.GetUserId(User)!;
        if (booking.UserId != userId && !User.IsInRole("Admin"))
            return Forbid();

        var eventId = booking.EventId;

        // Clear passenger assignments manually since FK uses NoAction
        if (booking.Role == BookingRole.Driver)
        {
            var assigned = await _db.Bookings
                .Where(b => b.AssignedToDriverBookingId == booking.Id)
                .ToListAsync();
            foreach (var p in assigned)
                p.AssignedToDriverBookingId = null;
        }

        _db.Bookings.Remove(booking);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Anmälan borttagen.";
        return RedirectToAction("Carpooling", "Events", new { id = eventId });
    }

    private async Task<BookingFormViewModel> BuildFormViewModel(int eventId, BookingFormViewModel? existing)
    {
        var evt = await _db.Events.FindAsync(eventId);
        var drivers = await _db.Bookings
            .Where(b => b.EventId == eventId && b.Role == BookingRole.Driver)
            .Include(b => b.User)
            .Include(b => b.AssignedPassengers)
            .ToListAsync();

        var vm = existing ?? new BookingFormViewModel();
        vm.EventId = eventId;
        vm.EventName = evt?.Name ?? "";
        vm.EventDate = evt?.Date ?? DateTime.Today;
        vm.AvailableDrivers = drivers
            .Where(d => d.AvailableSeats > 0)
            .Select(d => new DriverOption
            {
                BookingId = d.Id,
                FamilyName = d.User.FamilyName,
                AvailableSeats = d.AvailableSeats,
                Notes = d.Notes
            })
            .ToList();

        return vm;
    }
}
