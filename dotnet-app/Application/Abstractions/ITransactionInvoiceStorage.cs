using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Web.Application.Abstractions;

public interface ITransactionInvoiceStorage
{
    bool IsValidImage(IFormFile? file, out string errorMessage);
    Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default);
}

