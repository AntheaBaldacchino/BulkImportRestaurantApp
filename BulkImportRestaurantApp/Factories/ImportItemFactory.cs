using System;
using System.Collections.Generic;
using System.Text.Json;
using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;

namespace BulkImportRestaurantApp.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new ArgumentException("JSON cannot be empty.", nameof(json));

            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            if (root.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Root JSON element must be an array.");

            var items = new List<IItemValidating>();

            var restaurantMap = new Dictionary<string, Restaurant>(StringComparer.OrdinalIgnoreCase);

            foreach (var element in root.EnumerateArray())
            {
                var type = element.GetProperty("type").GetString()?.Trim().ToLowerInvariant();
                if (type != "restaurant")
                    continue;

                var externalId = element.GetProperty("id").GetString();
                if (string.IsNullOrWhiteSpace(externalId))
                    throw new InvalidOperationException("Restaurant missing id in JSON.");

                var restaurant = new Restaurant
                {
                    Name = element.GetProperty("name").GetString() ?? string.Empty,
                    OwnerEmailAddress = element.GetProperty("ownerEmailAddress").GetString() ?? string.Empty,
                    Status = ItemStatus.Pending
                };

                restaurantMap[externalId] = restaurant;
                items.Add(restaurant);
            }

          
            static string? ReadRestaurantId(JsonElement element)
            {
                if (element.TryGetProperty("restaurantId", out var directProp))
                    return directProp.GetString();

                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Name.Trim().Equals("restaurantId", StringComparison.OrdinalIgnoreCase))
                    {
                        return prop.Value.GetString();
                    }
                }

                return null;
            }

            foreach (var element in root.EnumerateArray())
            {
                var type = element.GetProperty("type").GetString()?.Trim().ToLowerInvariant();
                if (type != "menuitem")
                    continue;

                var restaurantJsonId = ReadRestaurantId(element);
                if (string.IsNullOrWhiteSpace(restaurantJsonId) ||
                    !restaurantMap.TryGetValue(restaurantJsonId, out var parentRestaurant))
                {
                  
                    continue;
                }

                var menuItem = new MenuItem
                {
                    Title = element.GetProperty("title").GetString() ?? string.Empty,
                    Price = element.GetProperty("price").GetDecimal(),
                    Status = ItemStatus.Pending,
                    Restaurant = parentRestaurant
                };

                items.Add(menuItem);
            }

            return items;
        }
    }
}
