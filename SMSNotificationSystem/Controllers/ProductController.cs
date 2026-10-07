using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.Inventory)]
    public class ProductController : AppController
    {
        private readonly IProductRepository _productRepository;

        public ProductController(IProductRepository productRepository) => _productRepository = productRepository;

        public async Task<IActionResult> Index() => View(await _productRepository.GetAllAsync());

        public IActionResult Create() => View("Form", new Product());

        [HttpPost]
        public async Task<IActionResult> Create(Product product)
        {
            if (!ModelState.IsValid) return View("Form", product);
            product.ProductName = product.ProductName.Trim();
            await _productRepository.CreateAsync(product);
            Toast("Product saved.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product == null ? NotFound() : View("Form", product);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Product product)
        {
            if (!ModelState.IsValid) return View("Form", product);
            product.ProductName = product.ProductName.Trim();
            await _productRepository.UpdateAsync(product);
            Toast("Product saved.");
            return RedirectToAction(nameof(Index));
        }

        /// <summary>Quick stock in / stock out from the product list.</summary>
        [HttpPost]
        public async Task<IActionResult> AdjustStock(int id, int quantity, string direction)
        {
            if (quantity <= 0)
            {
                Toast("Enter a quantity greater than zero.", "error");
                return RedirectToAction(nameof(Index));
            }

            var delta = direction == "out" ? -quantity : quantity;
            await _productRepository.AdjustStockAsync(id, delta);

            var product = await _productRepository.GetByIdAsync(id);
            if (product != null && product.IsLowStock)
                Toast($"{product.ProductName} is now at {product.StockQty}, at or below its reorder level. A low stock alert will be queued on the next scan.", "info");
            else
                Toast(delta > 0 ? $"Added {quantity} to stock." : $"Removed {quantity} from stock.");

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _productRepository.DeleteAsync(id);
            Toast("Product deleted.");
            return RedirectToAction(nameof(Index));
        }
    }
}
