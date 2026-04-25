using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class CategoriesController(CategoryService categoryService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(cancellationToken);
        return View(categories);
    }

    public IActionResult Create() => View(new CategoryInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CategoryInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["ModalError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                TempData["ModalTarget"] = "createCategoryModal";
                return RedirectToAction(nameof(Index));
            }
            return View(input);
        }

        await categoryService.CreateAsync(input.Name, input.Type, cancellationToken);
        TempData["Success"] = "Category created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetByIdAsync(id, cancellationToken);
        if (category is null) return NotFound();
        return View(new CategoryInputModel { Id = category.Id, Name = category.Name, Type = category.Type });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CategoryInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                return RedirectToAction(nameof(Index));
            }
            return View(input);
        }

        var updated = await categoryService.UpdateAsync(input.Id, input.Name, input.Type, cancellationToken);
        if (!updated) return NotFound();

        TempData["Success"] = "Category updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await categoryService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "Category deleted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}
