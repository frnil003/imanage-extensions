using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Samkorning.Data;

namespace Samkorning.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var upcomingEvents = await _db.Events
            .Where(e => e.Date >= DateTime.Today)
            .OrderBy(e => e.Date)
            .Include(e => e.Bookings)
            .Take(5)
            .ToListAsync();

        return View(upcomingEvents);
    }
}
