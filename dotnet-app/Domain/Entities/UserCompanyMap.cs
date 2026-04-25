namespace FinanceTracker.Web.Domain.Entities;

public class UserCompanyMap
{
    public int Id { get; set; }

    public bool IsDefault { get; set; }

    public int CompanyId { get; set; }
    public Company Company { get; set; } = null!;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
}