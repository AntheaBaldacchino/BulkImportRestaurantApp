using System.Collections.Generic;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Models.Interfaces;
using BulkImportRestaurantApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BulkImportRestaurantApp.Controllers
{
    public class ItemsController : Controller
    {
        private readonly ItemsDbRepository _dbRepository;

        public ItemsController(
             ItemsDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        // /Items/Catalog?mode=restaurants
        // /Items/Catalog?mode=menuitems&restaurantId=1
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
                    ViewBag.Mode = "menuitems";
                    ViewBag.RestaurantId = restaurantId.Value;
                }
                else
                {
                    var menuItems = await _dbRepository.GetAllApprovedMenuItemsAsync();
                    items = menuItems;
             
                }
            }
            ViewBag.ViewMode = view;
            return View("Catalog", items);
        }
    }
}
