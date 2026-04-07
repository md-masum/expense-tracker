using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Models;

public class TransactionListResult
{
    public required Project Project { get; set; }
    public required IReadOnlyList<FinanceTransaction> Items { get; set; }
    public required IReadOnlyList<Category> Categories { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Search { get; set; }
    public TransactionType? Type { get; set; }
    public int? CategoryId { get; set; }
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance { get; set; }
}
