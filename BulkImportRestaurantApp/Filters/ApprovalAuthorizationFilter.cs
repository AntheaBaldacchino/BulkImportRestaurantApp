using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Data;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace BulkImportRestaurantApp.Filters
{
    public class ApprovalAuthorizeFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _db;

        public ApprovalAuthorizeFilter(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task OnActionExecutionAsync(
            ActionExecutingContext context,
            ActionExecutionDelegate next)
        {
            // get logged-in email (Identity)
            var email = context.HttpContext.User.FindFirstValue(ClaimTypes.Email)
                        ?? context.HttpContext.User.Identity?.Name;

            if (string.IsNullOrWhiteSpace(email))
            {
                context.Result = new ForbidResult();
                return;
            }

            // Read IDs from action parameters
            context.ActionArguments.TryGetValue("restaurantIds", out var rObj);
            context.ActionArguments.TryGetValue("menuItemIds", out var mObj);

            var restaurantIds = rObj as int[] ?? Array.Empty<int>();
            var menuItemIds = mObj as Guid[] ?? Array.Empty<Guid>();

            // Load restaurants and check validators
            if (restaurantIds.Length > 0)
            {
                var restaurants = await _db.Restaurants
                    .Where(r => restaurantIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in restaurants)
                {
                    if (!r.GetValidators()
                          .Any(v => string.Equals(v, email, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }
            }

            // Load menu items and check validators (uses Restaurant.OwnerEmailAddress)
            if (menuItemIds.Length > 0)
            {
                var menuItems = await _db.MenuItems
                    .Where(m => menuItemIds.Contains(m.Id))
                    .Include(m => m.Restaurant)
                    .ToListAsync();

                foreach (var m in menuItems)
                {
                    if (!m.GetValidators()
                          .Any(v => string.Equals(v, email, StringComparison.OrdinalIgnoreCase)))
                    {
                        context.Result = new ForbidResult();
                        return;
                    }
                }
            }

            // All good → continue to action
            await next();
        }
    }
}
