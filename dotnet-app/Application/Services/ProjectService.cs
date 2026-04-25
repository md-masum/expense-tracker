using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    ITransactionRepository transactionRepository,
    ActiveCompanyContext activeCompanyContext,
    IUnitOfWork unitOfWork)
{
    private int GetRequiredCompanyId()
        => activeCompanyContext.CompanyId
           ?? throw new InvalidOperationException("No active company is selected.");

    public Task<List<Project>> GetAllAsync(CancellationToken cancellationToken = default)
        => projectRepository.GetAllAsync(GetRequiredCompanyId(), cancellationToken: cancellationToken);

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => projectRepository.GetByIdAsync(id, GetRequiredCompanyId(), cancellationToken: cancellationToken);

    public async Task CreateAsync(string name, int projectTypeId, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            CompanyId = GetRequiredCompanyId(),
            Name = name.Trim(),
            ProjectTypeId = projectTypeId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, string name, int projectTypeId, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, GetRequiredCompanyId(), cancellationToken: cancellationToken);
        if (project is null)
        {
            return false;
        }

        project.Name = name.Trim();
        project.ProjectTypeId = projectTypeId;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int?> DeleteWithTransactionsAsync(int id, CancellationToken cancellationToken = default)
    {
        var companyId = GetRequiredCompanyId();
        var project = await projectRepository.GetByIdAsync(id, companyId, cancellationToken: cancellationToken);
        if (project is null)
        {
            return null;
        }

        var deletedTransactions = await transactionRepository.DeleteByProjectAsync(id, cancellationToken);
        projectRepository.Remove(project);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return deletedTransactions;
    }
}
