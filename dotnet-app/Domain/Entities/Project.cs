namespace FinanceTracker.Web.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();

    public decimal TotalIncome() => Transactions
        .Where(t => t.Type == TransactionType.Income)
        .Sum(t => t.Amount);

    public decimal TotalExpense() => Transactions
        .Where(t => t.Type == TransactionType.Expense)
        .Sum(t => t.Amount);

    public decimal Balance() => TotalIncome() - TotalExpense();
}
