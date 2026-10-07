using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SMSNotificationSystem.Helpers;
using SMSNotificationSystem.Models;
using SMSNotificationSystem.Repositories.Interfaces;

namespace SMSNotificationSystem.Controllers
{
    [Authorize(Roles = AppRoles.AdminOrAccountant)]
    public class InvoiceController : AppController
    {
        private readonly IInvoiceRepository _invoiceRepository;

        public InvoiceController(IInvoiceRepository invoiceRepository) => _invoiceRepository = invoiceRepository;

        public async Task<IActionResult> Index() => View(await _invoiceRepository.GetAllAsync());

        public async Task<IActionResult> Create() =>
            View("Form", new Invoice { InvoiceNo = await _invoiceRepository.GetNextInvoiceNoAsync() });

        [HttpPost]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            if (await _invoiceRepository.InvoiceNoExistsAsync(invoice.InvoiceNo ?? string.Empty))
                ModelState.AddModelError(nameof(invoice.InvoiceNo), "This invoice number is already used");
            if (!ModelState.IsValid) return View("Form", invoice);

            await _invoiceRepository.CreateAsync(Clean(invoice));
            Toast($"Invoice {invoice.InvoiceNo} saved. A reminder goes out automatically when it's close to due.");
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id);
            return invoice == null ? NotFound() : View("Form", invoice);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(Invoice invoice)
        {
            if (await _invoiceRepository.InvoiceNoExistsAsync(invoice.InvoiceNo ?? string.Empty, invoice.InvoiceId))
                ModelState.AddModelError(nameof(invoice.InvoiceNo), "This invoice number is already used");
            if (!ModelState.IsValid) return View("Form", invoice);

            await _invoiceRepository.UpdateAsync(Clean(invoice));
            Toast("Invoice saved.");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var ok = await _invoiceRepository.MarkAsPaidAsync(id);
            Toast(ok ? "Invoice marked as paid. Reminders stop for it." : "That invoice was already paid.", ok ? "success" : "info");
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            await _invoiceRepository.DeleteAsync(id);
            Toast("Invoice deleted.");
            return RedirectToAction(nameof(Index));
        }

        private static Invoice Clean(Invoice i)
        {
            i.InvoiceNo = i.InvoiceNo.Trim();
            i.CustomerName = i.CustomerName.Trim();
            i.CustomerPhone = i.CustomerPhone.Trim();
            i.CustomerEmail = string.IsNullOrWhiteSpace(i.CustomerEmail) ? null : i.CustomerEmail.Trim();
            return i;
        }
    }
}
