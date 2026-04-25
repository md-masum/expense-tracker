using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class TransactionsController(
    TransactionService transactionService,
    CategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index(
        int projectId,
        string? search,
        TransactionType? type,
        int? categoryId,
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        var model = await transactionService.GetListAsync(projectId, search, type, categoryId, page, cancellationToken);
        if (model is null)
        {
            return NotFound();
        }

        return View(model);
    }

    public async Task<IActionResult> Create(int projectId, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, null);

        return View(new TransactionInputModel
        {
            ProjectId = projectId,
            Date = DateTime.Today,
            Type = TransactionType.Debit
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, input.Type);

        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["Error"] = "Please fix validation errors and try again.";
                return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
            }

            return View(input);
        }

        try
        {
            await transactionService.CreateAsync(
                input.ProjectId,
                input.Category,
                input.Type,
                input.Amount,
                input.Date,
                input.Note,
                input.InvoiceImage,
                cancellationToken);
        }
        catch (ArgumentException ex)
        {
            if (fromModal)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
            }

            ModelState.AddModelError(nameof(input.InvoiceImage), ex.Message);
            return View(input);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }

        TempData["Success"] = "Transaction created.";
        return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var tx = await transactionService.GetByIdAsync(id, cancellationToken);
        if (tx is null)
        {
            return NotFound();
        }

        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, tx.Type);

        return View(new TransactionInputModel
        {
            Id = tx.Id,
            ProjectId = tx.ProjectId,
            Category = tx.CategoryId,
            Type = tx.Type,
            Amount = tx.Amount,
            Date = tx.Date,
            Note = tx.Note,
            ExistingInvoiceImageUrl = tx.InvoiceImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TransactionInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        var existingTransaction = await transactionService.GetByIdAsync(input.Id, cancellationToken);
        if (existingTransaction is null)
        {
            return NotFound();
        }

        input.ExistingInvoiceImageUrl = existingTransaction.InvoiceImageUrl;

        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, input.Type);

        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["Error"] = "Please fix validation errors and try again.";
                return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
            }

            return View(input);
        }

        bool updated;
        try
        {
            updated = await transactionService.UpdateAsync(
                input.Id,
                input.Category,
                input.Type,
                input.Amount,
                input.Date,
                input.Note,
                input.InvoiceImage,
                input.RemoveInvoiceImage,
                cancellationToken);
        }
        catch (ArgumentException ex)
        {
            if (fromModal)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
            }

            ModelState.AddModelError(nameof(input.InvoiceImage), ex.Message);
            return View(input);
        }

        if (!updated)
        {
            return NotFound();
        }

        TempData["Success"] = "Transaction updated.";
        return RedirectToAction(nameof(Index), new { projectId = input.ProjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int projectId, CancellationToken cancellationToken)
    {
        var deleted = await transactionService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["Success"] = "Transaction deleted.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    private static IEnumerable<SelectListItem> BuildCategories(IEnumerable<Category> categories, TransactionType? type)
    {
        var filtered = type.HasValue
            ? categories.Where(x => x.Type == type.Value)
            : categories;

        return filtered.Select(x => new SelectListItem(x.Name, x.Id.ToString())).ToList();
    }
}
