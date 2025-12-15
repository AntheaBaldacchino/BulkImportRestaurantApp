using BulkImportRestaurantApp.Models.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BulkImportRestaurantApp.Models
{
    public class MenuItem : IItemValidating
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(150)]
        public string Title { get; set; } = string.Empty;
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }

        [Required]
        public int RestaurantId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public Restaurant Restaurant { get; set; } = null!;

        [Required]
        public ItemStatus Status { get; set; } = ItemStatus.Pending;

        public string GetCardPartial()
        {
            return "_MenuItemRow";
        }

        public List<string> GetValidators()
        {
            return new List<string>
            {
                Restaurant.OwnerEmailAddress
            };
        }
    }
}
