namespace BulkImportRestaurantApp.Models.Interfaces
{
    public interface IItemsRepository
    {
        List<IItemValidating> Get();
        void Save(List<IItemValidating> items);
        void Clear();
    }
}
