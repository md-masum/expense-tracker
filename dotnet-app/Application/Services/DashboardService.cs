using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Application.Models;

namespace FinanceTracker.Web.Application.Services;

public class DashboardService(IProjectRepository projectRepository)
{
    public async Task<DashboardSummary> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var projects = await projectRepository.GetAllAsync(includeTransactions: true, cancellationToken);
        var cards = projects.Select(project =>
        {
            var income = project.TotalIncome();
            var expense = project.TotalExpense();
            return new ProjectCardSummary
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                ProjectType = project.Type,
                Income = income,
                Expense = expense,
                Balance = income - expense,
                TransactionCount = project.Transactions.Count
            };
        }).ToList();

        return new DashboardSummary
        {
            TotalIncome = cards.Sum(x => x.Income),
            TotalExpense = cards.Sum(x => x.Expense),
            TotalBalance = cards.Sum(x => x.Balance),
            Projects = cards
        };
    }
}
