namespace FinanceTracker.Web.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; } = true;

    public Company? Company { get; set; }
    public int CompanyId { get; set; }

    public ProjectType? Type { get; set; }
    public int ProjectTypeId { get; set; }

    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();

    public decimal TotalCredit() => Transactions
        .Where(t => t.Type == TransactionType.Credit)
        .Sum(t => t.Amount);

    public decimal TotalDebit() => Transactions
        .Where(t => t.Type == TransactionType.Debit)
        .Sum(t => t.Amount);

    public decimal Balance() => TotalCredit() - TotalDebit();
}
