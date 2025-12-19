using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Models.Interfaces;
using BulkImportRestaurantApp.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace BulkImportRestaurantApp.Filters
{
    public class ApprovalAuthorizationFilter : IAsyncActionFilter
    {
        private readonly ItemsDbRepository _dbRepository;

        public ApprovalAuthorizationFilter(ItemsDbRepository dbRepository)
        {
            _dbRepository = dbRepository;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;
            if (!user.Identity?.IsAuthenticated ?? true)
            {
                context.Result = new ForbidResult();
                return;
            }

            var userEmail =
                user.FindFirst(ClaimTypes.Email)?.Value ??
                user.Identity?.Name ??
                string.Empty;

            context.ActionArguments.TryGetValue("restaurantIds", out var rObj);
            context.ActionArguments.TryGetValue("menuItemIds", out var mObj);

            var restaurantIds = (rObj as int[]) ?? Array.Empty<int>();
            var menuItemIds = (mObj as Guid[]) ?? Array.Empty<Guid>();

            var items = await _dbRepository.GetItemsByIdsAsync(restaurantIds, menuItemIds);

            var notAllowed = items.Any(item =>
                !item.GetValidators()
                    .Any(v => string.Equals(v, userEmail, StringComparison.OrdinalIgnoreCase)));

            if (notAllowed)
            {
                context.Result = new ForbidResult();
                return;
            }

            await next();
        }
    }
}
