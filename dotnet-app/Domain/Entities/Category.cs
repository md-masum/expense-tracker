namespace FinanceTracker.Web.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TransactionType Type { get; set; }

    public ICollection<FinanceTransaction> Transactions { get; set; } = new List<FinanceTransaction>();
}
