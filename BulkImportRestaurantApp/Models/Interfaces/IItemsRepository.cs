using BulkImportRestaurantApp.Models.Interfaces;

public interface IItemsRepository
{
    Task SaveAsync(IEnumerable<IItemValidating> items);
    Task<IReadOnlyList<IItemValidating>> GetAsync();
}
