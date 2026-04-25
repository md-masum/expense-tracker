using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class ProjectTypesController(ProjectTypeService projectTypeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var projectTypes = await projectTypeService.GetAllAsync(cancellationToken);
        return View(projectTypes);
    }

    public IActionResult Create() => View(new ProjectTypeInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectTypeInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["ModalError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                TempData["ModalTarget"] = "createProjectTypeModal";
                return RedirectToAction(nameof(Index));
            }
            return View(input);
        }

        await projectTypeService.CreateAsync(input.Name, input.Description, cancellationToken);
        TempData["Success"] = "Project type created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var projectType = await projectTypeService.GetByIdAsync(id, cancellationToken);
        if (projectType is null) return NotFound();
        return View(new ProjectTypeInputModel { Id = projectType.Id, Name = projectType.Name, Description = projectType.Description });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectTypeInputModel input, CancellationToken cancellationToken)
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

        var updated = await projectTypeService.UpdateAsync(input.Id, input.Name, input.Description, cancellationToken);
        if (!updated) return NotFound();

        TempData["Success"] = "Project type updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await projectTypeService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }

            TempData["Success"] = "Project type deleted.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

