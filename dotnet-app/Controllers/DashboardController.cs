using FinanceTracker.Web.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

public class DashboardController(DashboardService dashboardService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = await dashboardService.GetSummaryAsync(cancellationToken);
        return View(model);
    }
}
