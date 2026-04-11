using System.Security.Claims;
using FinanceTracker.Web.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace FinanceTracker.Web.Infrastructure.Filters;

public class RequireCompanyOnboardingFilter(CompanyService companyService) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            await next();
            return;
        }

        var controller = (string?)context.RouteData.Values["controller"];
        var action = (string?)context.RouteData.Values["action"];

        if (string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        var isAllowedCompanyOnboardingAction = string.Equals(controller, "Companies", StringComparison.OrdinalIgnoreCase)
            && (string.Equals(action, "Index", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "Create", StringComparison.OrdinalIgnoreCase)
                || string.Equals(action, "RequestJoin", StringComparison.OrdinalIgnoreCase));

        if (isAllowedCompanyOnboardingAction)
        {
            await next();
            return;
        }

        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            context.Result = new ChallengeResult();
            return;
        }

        var hasCompany = await companyService.HasAnyCompanyAsync(userId, context.HttpContext.RequestAborted);
        if (!hasCompany)
        {
            context.Result = new RedirectToActionResult("Index", "Companies", null);
            return;
        }

        await next();
    }
}

