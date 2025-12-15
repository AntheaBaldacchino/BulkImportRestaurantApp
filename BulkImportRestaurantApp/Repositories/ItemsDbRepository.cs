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


    }
}
