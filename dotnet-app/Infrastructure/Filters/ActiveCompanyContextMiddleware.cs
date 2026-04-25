using System.Security.Claims;
using FinanceTracker.Web.Application.Services;

namespace FinanceTracker.Web.Infrastructure.Filters;

public class ActiveCompanyContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        CompanyService companyService,
        ActiveCompanyContext activeCompanyContext)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrWhiteSpace(userId))
            {
                var company = await companyService.GetActiveForUserAsync(userId, context.RequestAborted);
                activeCompanyContext.Set(company);
            }
        }

        await next(context);
    }
}

