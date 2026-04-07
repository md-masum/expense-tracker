namespace FinanceTracker.Web.Domain.Entities;

public class FinanceTransaction
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public int CategoryId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Note { get; set; }
    public int SeqNo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Project? Project { get; set; }
    public Category? Category { get; set; }
}
