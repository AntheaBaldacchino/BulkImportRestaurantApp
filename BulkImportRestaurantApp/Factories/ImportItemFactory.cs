using BulkImportRestaurantApp.Models;
using BulkImportRestaurantApp.Models.Interfaces;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.Json;

namespace BulkImportRestaurantApp.Factories
{
    public class ImportItemFactory
    {
        public List<IItemValidating> Create(string json)
        {
            var items = new List<IItemValidating>();
            
            //returns a list of instances of classes inheriting itemvalidating
            using var document = JsonDocument.Parse(json);

            var root = document.RootElement;

            // iterate over each element and deserializes based on "type" property
            
            foreach(var element in root.EnumerateArray())
            {
                string type = element.GetProperty("type").GetString()?.ToLower();

                if (type == "restaurant") {

                    var restaurant = new Restaurant
                    {
                        Name = element.GetProperty("name").GetString()!,
                        OwnerEmailAddress = element.GetProperty("ownerEmailAddress").GetString()!,
                        Status = ItemStatus.Pending
                    };
                    items.Add(restaurant);
                    //name
                    //owneremailaddress
                    //status
                }
                else if (type == "menuitem")
                {
                    var menuItem = new MenuItem
                    {
                        Id = Guid.NewGuid(),
                        Title = element.GetProperty("title").GetString()!,
                        Price = element.GetProperty("price").GetDecimal()!,
                        Status = ItemStatus.Pending


                    };
                    items.Add(menuItem);
                    //id 
                    //title
                    //price
                    //status
                    
                }
            }
            return items;
        }

    }
}
