using System.ComponentModel.DataAnnotations;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.ViewModels;

public class TransactionInputModel
{
    public int Id { get; set; }
    public int ProjectId { get; set; }

    [Required]
    public int CategoryId { get; set; }

    [Required]
    public TransactionType Type { get; set; } = TransactionType.Expense;

    [Range(0.01, 999999999)]
    public decimal Amount { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [StringLength(500)]
    public string? Note { get; set; }
}
