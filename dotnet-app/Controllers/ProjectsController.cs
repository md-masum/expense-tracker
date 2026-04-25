using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class ProjectsController(ProjectService projectService, ProjectTypeService projectTypeService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(cancellationToken);
        ViewBag.ProjectTypes = await projectTypeService.GetAllAsync(cancellationToken);
        return View(projects);
    }

    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var projectTypes = await projectTypeService.GetAllAsync(cancellationToken);
        ViewBag.ProjectTypes = projectTypes;
        return View(new ProjectInputModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["ModalError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                TempData["ModalTarget"] = "createProjectModal";
                return RedirectToAction(nameof(Index));
            }
            var projectTypes = await projectTypeService.GetAllAsync(cancellationToken);
            ViewBag.ProjectTypes = projectTypes;
            return View(input);
        }

        await projectService.CreateAsync(input.Name, input.ProjectTypeId, cancellationToken);
        TempData["Success"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(id, cancellationToken);
        if (project is null) return NotFound();

        var projectTypes = await projectTypeService.GetAllAsync(cancellationToken);
        ViewBag.ProjectTypes = projectTypes;
        return View(new ProjectInputModel { Id = project.Id, Name = project.Name, ProjectTypeId = project.ProjectTypeId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                return RedirectToAction(nameof(Index));
            }
            var projectTypes = await projectTypeService.GetAllAsync(cancellationToken);
            ViewBag.ProjectTypes = projectTypes;
            return View(input);
        }

        var updated = await projectService.UpdateAsync(input.Id, input.Name, input.ProjectTypeId, cancellationToken);
        if (!updated) return NotFound();

        TempData["Success"] = "Project updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var removed = await projectService.DeleteWithTransactionsAsync(id, cancellationToken);
        if (!removed.HasValue)
        {
            return NotFound();
        }

        TempData["Success"] = removed.Value > 0
            ? $"Project deleted with {removed.Value} transaction(s)."
            : "Project deleted.";

        return RedirectToAction(nameof(Index));
    }
}
