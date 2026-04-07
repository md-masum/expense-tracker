using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Web.ViewModels;

public class ProjectInputModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40)]
    public string Type { get; set; } = string.Empty;
}
