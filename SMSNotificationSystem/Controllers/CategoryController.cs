using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrManager)]
    public class CategoryController : AppController
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(ICategoryRepository categoryRepository) => _categoryRepository = categoryRepository;

        public async Task<IActionResult> Index() => View(await _categoryRepository.GetAllAsync());

        [HttpPost]
        public async Task<IActionResult> Create(string name, string? description)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 80)
            {
                Toast("Enter a category name of up to 80 characters.", "error");
            }
            else if (await _categoryRepository.NameExistsAsync(name))
            {
                Toast($"A category called \"{name.Trim()}\" already exists.", "error");
            }
            else
            {
                await _categoryRepository.CreateAsync(new TemplateCategory
                {
                    Name = name.Trim(),
                    Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
                });
                Toast("Category added.");
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var deleted = await _categoryRepository.DeleteAsync(id);
            Toast(deleted ? "Category deleted." : "Move or delete this category's templates first.", deleted ? "success" : "error");
            return RedirectToAction(nameof(Index));
        }
    }
}
