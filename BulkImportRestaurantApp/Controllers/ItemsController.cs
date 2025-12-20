using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Infrastructure;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;
using BulkImportRestaurantApp.Repositories;
using BulkImportRestaurantApp.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkImportRestaurantApp.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ItemsDbRepository _dbRepository;

        public ItemsController(ItemsDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

       
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Catalog(string view = "restaurants", int? restaurantId = null)
        {
            IEnumerable<IItemValidating> items;

            if (view == "restaurants")
            {
                var restaurants = await _dbRepository.GetApprovedRestaurantsAsync();
                items = restaurants;
            }
            else
            {
                if (restaurantId.HasValue)
                {
                    var menuItems = await _dbRepository
                        .GetApprovedMenuItemsForRestaurantAsync(restaurantId.Value);
                    items = menuItems;
                    ViewBag.RestaurantId = restaurantId.Value;
                }
                else
                {
                    var menuItems = await _dbRepository.GetAllApprovedMenuItemsAsync();
                    items = menuItems;
                }
            }

            ViewBag.ViewMode = view;
            ViewBag.ApproveMode = false;
            return View("Catalog", items);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Verification()
        {
            var email = User.FindFirstValue(ClaimTypes.Email)
                        ?? User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
                return Challenge(); 

            var isSiteAdmin = string.Equals(
                email,
                AppSettings.SiteAdminEmail,
                StringComparison.OrdinalIgnoreCase);

            IEnumerable<IItemValidating> items;
            string viewMode;

            if (isSiteAdmin)
            {
                
                var pendingRestaurants = await _dbRepository.GetPendingRestaurantsAsync();
                items = pendingRestaurants;
                viewMode = "verify-restaurants";
            }
            else
            {
                
                var pendingMenuItems = await _dbRepository.GetPendingMenuItemsForOwnerAsync(email);
                items = pendingMenuItems;
                viewMode = "verify-menu";
            }

            ViewBag.ViewMode = viewMode;
            return View("Catalog", items); 
        }

        [HttpPost]
        [Authorize]
        [ServiceFilter(typeof(ApprovalAuthorizeFilter))]
        public async Task<IActionResult> Approve(
            int[] restaurantIds,
            Guid[] menuItemIds)
        {
            await _dbRepository.ApproveAsync(restaurantIds, menuItemIds);
            return RedirectToAction(nameof(Verification));
        }
    }
}
