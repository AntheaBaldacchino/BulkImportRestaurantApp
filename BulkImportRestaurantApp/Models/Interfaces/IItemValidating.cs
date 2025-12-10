namespace BulkImportRestaurantApp.Models.Interfaces
{
    public interface IItemValidating
    {
        ItemStatus Status { get; set; }

        List<string> GetValidators();

        string GetCardPartial();

    }
}