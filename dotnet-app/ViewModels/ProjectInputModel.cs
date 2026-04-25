using System.ComponentModel.DataAnnotations;

namespace FinanceTracker.Web.ViewModels;

public class ProjectInputModel
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public int ProjectTypeId { get; set; }
}
