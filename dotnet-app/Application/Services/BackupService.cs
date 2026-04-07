using System.Text;
using System.Text.Json;
using FinanceTracker.Web.Application.Models;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Application.Services;

public class BackupService(FinanceDbContext dbContext)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<string> ExportJsonAsync(CancellationToken cancellationToken = default)
    {
        var payload = new BackupPayload
        {
            ExportedAt = DateTime.UtcNow,
            Projects = await dbContext.Projects.AsNoTracking().ToListAsync(cancellationToken),
            Categories = await dbContext.Categories.AsNoTracking().ToListAsync(cancellationToken),
            Transactions = await dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken)
        };

        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    public async Task<(string FileName, string Content)> ExportCsvAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var project = await dbContext.Projects.AsNoTracking().FirstOrDefaultAsync(x => x.Id == projectId, cancellationToken)
            ?? throw new InvalidOperationException("Project not found.");

        var transactions = await dbContext.Transactions
            .AsNoTracking()
            .Where(x => x.ProjectId == projectId)
            .OrderBy(x => x.Date)
            .ThenBy(x => x.SeqNo)
            .Select(x => new
            {
                x.Date,
                x.Type,
                Category = x.Category!.Name,
                x.Amount,
                x.Note
            })
            .ToListAsync(cancellationToken);

        var lines = new List<string>
        {
            "Date,Type,Category,Amount,Note"
        };

        lines.AddRange(transactions.Select(x =>
            string.Join(",",
                CsvCell(x.Date.ToString("yyyy-MM-dd")),
                CsvCell(x.Type.ToString()),
                CsvCell(x.Category),
                CsvCell(x.Amount.ToString("0.##")),
                CsvCell(x.Note ?? string.Empty)
            )));

        var fileName = $"{Sanitize(project.Name)}_Report_{DateTime.UtcNow:yyyyMMdd}.csv";
        return (fileName, string.Join("\r\n", lines));
    }

    public async Task ImportJsonAsync(string json, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<BackupPayload>(json, JsonOptions)
            ?? throw new InvalidOperationException("Invalid JSON payload.");

        await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        dbContext.Transactions.RemoveRange(dbContext.Transactions);
        dbContext.Projects.RemoveRange(dbContext.Projects);
        dbContext.Categories.RemoveRange(dbContext.Categories);
        await dbContext.SaveChangesAsync(cancellationToken);

        await dbContext.Categories.AddRangeAsync(payload.Categories, cancellationToken);
        await dbContext.Projects.AddRangeAsync(payload.Projects, cancellationToken);
        await dbContext.Transactions.AddRangeAsync(payload.Transactions, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    private static string CsvCell(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value)
        {
            sb.Append(invalid.Contains(ch) ? '_' : ch);
        }

        return sb.ToString().Replace(' ', '_');
    }
}
