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
            Type = TransactionType.Expense
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TransactionInputModel input, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, input.Type);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await transactionService.CreateAsync(
            input.ProjectId,
            input.CategoryId,
            input.Type,
            input.Amount,
            input.Date,
            input.Note,
            cancellationToken);

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
            CategoryId = tx.CategoryId,
            Type = tx.Type,
            Amount = tx.Amount,
            Date = tx.Date,
            Note = tx.Note
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TransactionInputModel input, CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        ViewBag.Categories = BuildCategories(categories, input.Type);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var updated = await transactionService.UpdateAsync(
            input.Id,
            input.CategoryId,
            input.Type,
            input.Amount,
            input.Date,
            input.Note,
            cancellationToken);

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
