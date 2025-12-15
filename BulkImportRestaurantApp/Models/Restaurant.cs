using BulkImportRestaurantApp.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkImportRestaurantApp.Models
{
    public class Restaurant : IItemValidating 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(150)]
        public string OwnerEmailAddress { get; set; } = string.Empty;
        [Required]
        public ItemStatus Status { get; set; } = ItemStatus.Pending;

        public string?ImagePath { get; set; }

        public string GetCardPartial()
        {
            return "_RestaurantCard";
        }

        public List<string> GetValidators()
        {
            return new List<string>
            {
                Infrastructure.AppSettings.SiteAdminEmail
            };
        }
    }
}
