using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Models;

public class BackupPayload
{
    public string AppVersion { get; set; } = "2.0-dotnet";
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    public List<Project> Projects { get; set; } = [];
    public List<Category> Categories { get; set; } = [];
    public List<FinanceTransaction> Transactions { get; set; } = [];
}
