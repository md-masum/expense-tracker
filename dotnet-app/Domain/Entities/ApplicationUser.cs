using Microsoft.AspNetCore.Identity;

namespace FinanceTracker.Web.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public ICollection<Company> OwnedCompanies { get; set; } = new List<Company>();
    public ICollection<UserCompanyMap> UserCompanyMaps { get; set; } = new List<UserCompanyMap>();
    public ICollection<UserCompanyJoinRequest> CompanyJoinRequests { get; set; } = new List<UserCompanyJoinRequest>();
}
