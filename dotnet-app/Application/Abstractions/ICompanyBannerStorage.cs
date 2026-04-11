using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Web.Application.Abstractions;

public interface ICompanyBannerStorage
{
    bool IsValidImage(IFormFile? file, out string errorMessage);
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}


