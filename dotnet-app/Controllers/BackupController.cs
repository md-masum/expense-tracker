using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class BackupController(
    BackupService backupService,
    IProjectRepository projectRepository) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewBag.Projects = await projectRepository.GetAllAsync(cancellationToken: cancellationToken);
        return View();
    }

    public async Task<IActionResult> ExportJson(CancellationToken cancellationToken)
    {
        var payload = await backupService.ExportJsonAsync(cancellationToken);
        var fileName = $"FinanceTracker_Backup_{DateTime.UtcNow:yyyyMMdd}.json";
        return File(System.Text.Encoding.UTF8.GetBytes(payload), "application/json", fileName);
    }

    public async Task<IActionResult> ExportCsv(int projectId, CancellationToken cancellationToken)
    {
        var (fileName, content) = await backupService.ExportCsvAsync(projectId, cancellationToken);
        return File(System.Text.Encoding.UTF8.GetBytes(content), "text/csv", fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ImportJson(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
        {
            TempData["Error"] = "Please select a non-empty JSON file.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync(cancellationToken);

        await backupService.ImportJsonAsync(json, cancellationToken);
        TempData["Success"] = "Backup restored.";
        return RedirectToAction(nameof(Index));
    }
}
