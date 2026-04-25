using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public sealed record ActiveCompanyInfo(int Id, string Name, string? ImageUrl);

public class ActiveCompanyContext
{
    public ActiveCompanyInfo? Company { get; private set; }
    public int? CompanyId => Company?.Id;
    public bool HasCompany => Company is not null;

    public void Set(Company? company)
    {
        Company = company is null
            ? null
            : new ActiveCompanyInfo(company.Id, company.Name, company.ImageUrl);
    }
}

