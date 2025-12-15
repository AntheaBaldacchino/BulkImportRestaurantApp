using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BulkImportRestaurantApp.Models.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BulkImportRestaurantApp.Repositories
{
    public class ItemsInMemoryRepository : IItemsRepository
    {
        private readonly IMemoryCache _cache;
        private const string CacheKey = "PendingItems";

        public ItemsInMemoryRepository(IMemoryCache cache)
        {
            _cache = cache;
        }

        public Task SaveAsync(IEnumerable<IItemValidating> items)
        {
            // store ordered list so we can align with images later
            _cache.Set(CacheKey, items.ToList());
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<IItemValidating>> GetAsync()
        {
            if (_cache.TryGetValue(CacheKey, out List<IItemValidating>? items) && items is not null)
            {
                return Task.FromResult((IReadOnlyList<IItemValidating>)items);
            }

            return Task.FromResult((IReadOnlyList<IItemValidating>)new List<IItemValidating>());
        }

        public Task ClearAsync()
        {
            _cache.Remove(CacheKey);
            return Task.CompletedTask;
        }
    }
}
