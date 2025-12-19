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

        public async Task ApproveAsync(IEnumerable<int> restaurantIds, IEnumerable<Guid> menuItemIds)
        {
            var restaurantIdList = restaurantIds?.ToList() ?? new List<int>();
            var menuItemIdList = menuItemIds?.ToList() ?? new List<Guid>();

            if (restaurantIdList.Any())
            {
                var restaurants = await _db.Restaurants
                    .Where(r => restaurantIdList.Contains(r.Id))
                    .ToListAsync();

                foreach (var r in restaurants)
                {
                    r.Status = ItemStatus.Approved;
                }
            }

            if (menuItemIdList.Any())
            {
                var menuItems = await _db.MenuItems
                    .Where(m => menuItemIdList.Contains(m.Id))
                    .ToListAsync();

                foreach (var m in menuItems)
                {
                    m.Status = ItemStatus.Approved;
                }
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
    }
}
