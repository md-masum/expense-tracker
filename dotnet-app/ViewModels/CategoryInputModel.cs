using System.ComponentModel.DataAnnotations;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.ViewModels;

public class CategoryInputModel
{
    public int Id { get; set; }

    [Required, StringLength(60)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public TransactionType Type { get; set; } = TransactionType.Expense;
}
