using FinanceTracker.Web.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Web.Infrastructure.Files;

public class LocalTransactionInvoiceStorage(IWebHostEnvironment environment) : ITransactionInvoiceStorage
{
    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public bool IsValidImage(IFormFile? file, out string errorMessage)
    {
        if (file is null || file.Length == 0)
        {
            errorMessage = "Please select a non-empty image file.";
            return false;
        }

        if (file.Length > MaxFileSizeBytes)
        {
            errorMessage = "Image size must be 5 MB or smaller.";
            return false;
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            errorMessage = "Only JPG, PNG, or WEBP images are allowed.";
            return false;
        }

        if (!AllowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
        {
            errorMessage = "Invalid image content type.";
            return false;
        }

        if (!HasSupportedHeader(file, extension))
        {
            errorMessage = "Invalid or corrupted image file.";
            return false;
        }

        errorMessage = string.Empty;
        return true;
    }

    public async Task<string> SaveAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"tx_invoice_{Guid.NewGuid():N}{extension}";

        var relativePath = Path.Combine("uploads", "transaction-invoices", fileName).Replace('\\', '/');
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var absolutePath = Path.Combine(webRootPath, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath)!);

        await using var stream = new FileStream(absolutePath, FileMode.Create);
        await file.CopyToAsync(stream, cancellationToken);

        return "/" + relativePath;
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return Task.CompletedTask;
        }

        var trimmed = relativePath.TrimStart('/');
        var webRootPath = environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            webRootPath = Path.Combine(environment.ContentRootPath, "wwwroot");
        }

        var absolutePath = Path.Combine(webRootPath, trimmed.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
        }

        return Task.CompletedTask;
    }

    private static bool HasSupportedHeader(IFormFile file, string extension)
    {
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = stream.Read(header, 0, header.Length);
        if (bytesRead < 4)
        {
            return false;
        }

        return extension switch
        {
            ".jpg" or ".jpeg" => header[0] == 0xFF && header[1] == 0xD8,
            ".png" => header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            ".webp" => bytesRead >= 12 &&
                       header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46 &&
                       header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50,
            _ => false
        };
    }
}

