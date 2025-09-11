using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using manage_coffee_shop_web.Models;

[Authorize]
public class UserController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager; // Added UserManager injection

    public UserController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager; // Inject UserManager
    }

    public async Task<IActionResult> History() // Changed to async for UserManager
    {
        var user = await _userManager.GetUserAsync(User); // Get the current user
        if (user == null)
        {
            return RedirectToAction("Login", "Account"); // Redirect to login if user not found
        }

        var orders = _context.Orders
            .Include(o => o.OrderDetails)
            .ThenInclude(od => od.Product)
            .Where(o => o.ApplicationUserId == user.Id) // Use user's Id instead of User.Identity.Name
            .OrderByDescending(o => o.OrderDate)
            .ToList();

        return View(orders);
    }
}