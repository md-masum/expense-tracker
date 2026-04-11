using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class ProjectsController(ProjectService projectService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(cancellationToken);
        return View(projects);
    }

    public IActionResult Create() => View(new ProjectInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProjectInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        await projectService.CreateAsync(input.Name, input.Type, cancellationToken);
        TempData["Success"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return NotFound();
        }

        return View(new ProjectInputModel { Id = project.Id, Name = project.Name, Type = project.Type });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(ProjectInputModel input, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var updated = await projectService.UpdateAsync(input.Id, input.Name, input.Type, cancellationToken);
        if (!updated)
        {
            return NotFound();
        }

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
