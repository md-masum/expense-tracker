namespace FinanceTracker.Web.Application.Models;

public class DashboardSummary
{
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalBalance { get; set; }
    public List<ProjectCardSummary> Projects { get; set; } = [];
}

public class ProjectCardSummary
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public decimal Credit { get; set; }
    public decimal Debit { get; set; }
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
}
