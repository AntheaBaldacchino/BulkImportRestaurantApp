using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Data;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BulkImportRestaurantApp.Repositories
{
    public class ItemsDbRepository : IItemsRepository
    {
        private readonly ApplicationDbContext _db;

        public ItemsDbRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case Restaurant restaurant:
                        _db.Restaurants.Add(restaurant);
                        break;

                    case MenuItem menuItem:
                        _db.MenuItems.Add(menuItem);
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported item type {item.GetType().Name} passed to ItemsDbRepository.");
                }
            }

            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            var result = new List<IItemValidating>();

            result.AddRange(await _db.Restaurants.ToListAsync());
            result.AddRange(await _db.MenuItems.ToListAsync());

            return result;
        }

        public Task<List<Restaurant>> GetApprovedRestaurantsAsync()
        {
            return _db.Restaurants
                .Where(r => r.Status == ItemStatus.Approved)
                .ToListAsync();
        }

        public Task<List<MenuItem>> GetApprovedMenuItemsForRestaurantAsync(int restaurantId)
        {
            return _db.MenuItems
                .Where(m => m.Status == ItemStatus.Approved && m.RestaurantId == restaurantId)
                .Include(m => m.Restaurant)
                .ToListAsync();
        }

        public Task<List<MenuItem>> GetAllApprovedMenuItemsAsync()
        {
            return _db.MenuItems
                .Where(m => m.Status == ItemStatus.Approved)
                .Include(m => m.Restaurant)
                .ToListAsync();
        }

        public Task<List<Restaurant>> GetPendingRestaurantsAsync()
        {
            return _db.Restaurants
                .Where(r => r.Status == ItemStatus.Pending)
                .ToListAsync();
        }

        public Task<List<Restaurant>> GetOwnedRestaurantsAsync(string ownerEmail)
        {
            return _db.Restaurants
                .Where(r => r.OwnerEmailAddress == ownerEmail)
                .ToListAsync();
        }

        public Task<List<MenuItem>> GetPendingMenuItemsForRestaurantAsync(int restaurantId)
        {
            return _db.MenuItems
                .Where(m => m.Status == ItemStatus.Pending && m.RestaurantId == restaurantId)
                .Include(m => m.Restaurant)
                .ToListAsync();
        }

        public Task<List<MenuItem>> GetPendingMenuItemsForOwnerAsync(string ownerEmail)
        {
            return _db.MenuItems
                .Where(m => m.Status == ItemStatus.Pending &&
                            m.Restaurant.OwnerEmailAddress == ownerEmail)
                .Include(m => m.Restaurant)
                .ToListAsync();
        }
        public async Task ApproveAsync(
         IEnumerable<int> restaurantIds,
         IEnumerable<Guid> menuItemIds) 
        {
            var rIds = restaurantIds?.ToList() ?? new List<int>();
            var mIds = menuItemIds?.ToList() ?? new List<Guid>();

            if (rIds.Count > 0)
            {
                var restaurants = await _db.Restaurants
                    .Where(r => rIds.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in restaurants)
                    r.Status = ItemStatus.Approved;
            }

            if (mIds.Count > 0)
            {
                var menuItems = await _db.MenuItems
                    .Where(m => mIds.Contains(m.Id))
                    .ToListAsync();

                foreach (var m in menuItems)
                    m.Status = ItemStatus.Approved;
            }

            await _db.SaveChangesAsync();
        
        }

        public async Task<List<IItemValidating>> GetItemsByIdsAsync(
            IEnumerable<int> restaurantIds,
            IEnumerable<Guid> menuItemIds)
        {
            var result = new List<IItemValidating>();

            var rList = restaurantIds?.ToList() ?? new List<int>();
            var mList = menuItemIds?.ToList() ?? new List<Guid>();

            if (rList.Any())
            {
                result.AddRange(await _db.Restaurants
                    .Where(r => rList.Contains(r.Id))
                    .ToListAsync());
            }

            if (mList.Any())
            {
                result.AddRange(await _db.MenuItems
                    .Where(m => mList.Contains(m.Id))
                    .Include(m => m.Restaurant)
                    .ToListAsync());
            }

            return result;
        }

        public Task<List<Restaurant>> GetPendingRestaurantsForAdminAsync()
        {
            // Only admin (SiteAdminEmail) should see this in the controller
            return _db.Restaurants
                .Where(r => r.Status == ItemStatus.Pending)
                .ToListAsync();
        }

        public async Task<bool> ApproveRestaurantAsync(int id)
        {
            var restaurant = await _db.Restaurants.FindAsync(id);
            if (restaurant == null)
                return false;

            restaurant.Status = ItemStatus.Approved;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ApproveMenuItemAsync(Guid id, string currentUserEmail, bool isAdmin)
        {
            var menuItem = await _db.MenuItems
                .Include(m => m.Restaurant)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (menuItem == null)
                return false;

       
            var ownerEmail = menuItem.Restaurant.OwnerEmailAddress;

            if (!isAdmin &&
                !string.Equals(ownerEmail, currentUserEmail, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            menuItem.Status = ItemStatus.Approved;
            await _db.SaveChangesAsync();
            return true;
        }

    }
}
