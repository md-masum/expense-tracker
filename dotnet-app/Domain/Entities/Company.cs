namespace FinanceTracker.Web.Domain.Entities;

public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public ApplicationUser Owner { get; set; } = null!;
    public string OwnerId { get; set; } = string.Empty;

    public ICollection<UserCompanyMap> UserCompanyMaps { get; set; } = new List<UserCompanyMap>();
    public ICollection<UserCompanyJoinRequest> JoinRequests { get; set; } = new List<UserCompanyJoinRequest>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}