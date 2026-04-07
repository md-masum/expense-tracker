namespace FinanceTracker.Web.Application.Models;

public class DashboardSummary
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal TotalBalance { get; set; }
    public List<ProjectCardSummary> Projects { get; set; } = [];
}

public class ProjectCardSummary
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}
