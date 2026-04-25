namespace FinanceTracker.Web.Domain.Entities;

public class ProjectType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
}


