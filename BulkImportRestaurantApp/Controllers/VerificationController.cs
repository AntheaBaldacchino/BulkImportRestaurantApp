using System;
using System.Linq;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Infrastructure;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BulkImportRestaurantApp.Controllers
{
    [Authorize]
    public class VerificationController : Controller
    {
        private readonly ItemsDbRepository _dbRepository;
        private readonly UserManager<IdentityUser> _userManager;

        public VerificationController(
            ItemsDbRepository dbRepository,
            UserManager<IdentityUser> userManager)
        {
            _dbRepository = dbRepository;
            _userManager = userManager;
        }

        private async Task<(string Email, bool IsAdmin)> GetCurrentUserInfoAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            var email = user?.Email ?? string.Empty;

            var isAdmin = string.Equals(email, AppSettings.SiteAdminEmail,
                StringComparison.OrdinalIgnoreCase);

            return (email, isAdmin);
        }

        [HttpGet]
        public async Task<IActionResult> Pending()
        {
            var (email, isAdmin) = await GetCurrentUserInfoAsync();

            var pendingRestaurants = Enumerable.Empty<Restaurant>().ToList();
            if (isAdmin)
            {
                pendingRestaurants = await _dbRepository.GetPendingRestaurantsForAdminAsync();
            }

            var pendingMenuItems = await _dbRepository.GetPendingMenuItemsForOwnerAsync(email);

            var model = new PendingItemsViewModel
            {
                PendingRestaurants = pendingRestaurants,
                PendingMenuItems = pendingMenuItems
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveRestaurant(int id)
        {
            var (_, isAdmin) = await GetCurrentUserInfoAsync();

            if (!isAdmin)
                return Forbid();

            var ok = await _dbRepository.ApproveRestaurantAsync(id);
            if (!ok)
                return NotFound();

            return RedirectToAction(nameof(Pending));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveMenuItem(Guid id)
        {
            var (email, isAdmin) = await GetCurrentUserInfoAsync();

            var ok = await _dbRepository.ApproveMenuItemAsync(id, email, isAdmin);
            if (!ok)
                return Forbid();

            return RedirectToAction(nameof(Pending));
        }
    }

    public class PendingItemsViewModel
    {
        public List<Restaurant> PendingRestaurants { get; set; } = new();
        public List<MenuItem> PendingMenuItems { get; set; } = new();
    }
}
