using System.ComponentModel.DataAnnotations;

namespace SMSNotificationSystem.Models
{
    public class Product
    {
        public int ProductId { get; set; }

        [Required, StringLength(120), Display(Name = "Product name")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(40), Display(Name = "SKU")]
        public string? Sku { get; set; }

        [Range(0, int.MaxValue), Display(Name = "Stock quantity")]
        public int StockQty { get; set; }

        [Range(0, int.MaxValue), Display(Name = "Reorder level")]
        public int ReorderLevel { get; set; } = 10;

        [Range(0, 999999999), Display(Name = "Unit price")]
        public decimal UnitPrice { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.Now;

        public bool IsLowStock => StockQty <= ReorderLevel;
    }
}
