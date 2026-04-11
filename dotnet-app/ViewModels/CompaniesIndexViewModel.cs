using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.ViewModels;

public class CompaniesIndexViewModel
{
    public string CurrentUserId { get; set; } = string.Empty;
    public bool HasAnyCompany { get; set; }
    public IReadOnlyList<Company> Companies { get; set; } = [];
}

