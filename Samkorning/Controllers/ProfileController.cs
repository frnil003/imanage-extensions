using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Samkorning.Models;

namespace Samkorning.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ProfileController(UserManager<ApplicationUser> userManager)
        => _userManager = userManager;

    [HttpGet]
    public async Task<IActionResult> Edit()
    {
        var user = await _userManager.GetUserAsync(User);
        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string familyName, string phoneNumber)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return NotFound();

        user.FamilyName = familyName?.Trim() ?? "";
        user.PhoneNumber = phoneNumber?.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (result.Succeeded)
            TempData["Success"] = "Profilen uppdaterad!";
        else
            TempData["Error"] = "Kunde inte spara ändringar.";

        return RedirectToAction(nameof(Edit));
    }
}
