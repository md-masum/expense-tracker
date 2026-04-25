using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Web.ViewModels;

public class ProjectTypeInputModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

