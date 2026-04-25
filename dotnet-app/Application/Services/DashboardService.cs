using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Application.Models;

namespace FinanceTracker.Web.Application.Services;

public class DashboardService(
    IProjectRepository projectRepository,
    ActiveCompanyContext activeCompanyContext)
{
    private int GetRequiredCompanyId()
        => activeCompanyContext.CompanyId
           ?? throw new InvalidOperationException("No active company is selected.");

    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetAllAsync(GetRequiredCompanyId(), includeTransactions: true, cancellationToken);
         var cards = projects.Select(project =>
         {
             var credit = project.TotalCredit();
             var debit = project.TotalDebit();
             return new ProjectCardSummary
             {
                 ProjectId = project.Id,
                 ProjectName = project.Name,
                 ProjectType = project.Type?.Name ?? "Unknown",
                 Credit = credit,
                 Debit = debit,
                 Balance = credit - debit,
                 TransactionCount = project.Transactions.Count
             };
         }).ToList();

         return new DashboardSummary
         {
             TotalCredit = cards.Sum(x => x.Credit),
             TotalDebit = cards.Sum(x => x.Debit),
             TotalBalance = cards.Sum(x => x.Balance),
             Projects = cards
         };
    }
}
