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

        // -------- IItemsRepository basic methods --------
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
                .Where(m => m.RestaurantId == restaurantId && m.Status == ItemStatus.Pending)
                .Include(m => m.Restaurant)
                .ToListAsync();
        }

        // Optional helper for the filter later
        public Task<List<Restaurant>> GetRestaurantsByIdsAsync(IEnumerable<int> ids)
        {
            var list = ids.ToList();
            return _db.Restaurants
                .Where(r => list.Contains(r.Id))
                .ToListAsync();
        }

        public Task<List<MenuItem>> GetMenuItemsByIdsAsync(IEnumerable<Guid> ids)
        {
            var list = ids.ToList();
            return _db.MenuItems
                .Where(m => list.Contains(m.Id))
                .Include(m => m.Restaurant)
                .ToListAsync();
        }

        // -------- Approve method (SE3.3) --------
        public async Task ApproveAsync(IEnumerable<int>? restaurantIds, IEnumerable<Guid>? menuItemIds)
        {
            if (restaurantIds != null)
            {
                var restList = restaurantIds.ToList();
                if (restList.Any())
                {
                    var restaurants = await _db.Restaurants
                        .Where(r => restList.Contains(r.Id))
                        .ToListAsync();

                    foreach (var r in restaurants)
                    {
                        r.Status = ItemStatus.Approved;
                    }
                }
            }

            if (menuItemIds != null)
            {
                var menuList = menuItemIds.ToList();
                if (menuList.Any())
                {
                    var menuItems = await _db.MenuItems
                        .Where(m => menuList.Contains(m.Id))
                        .ToListAsync();

                    foreach (var m in menuItems)
                    {
                        m.Status = ItemStatus.Approved;
                    }
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
