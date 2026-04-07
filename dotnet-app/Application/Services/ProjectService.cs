using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public class ProjectService(
    IProjectRepository projectRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
{
    public Task<List<Project>> GetAllAsync(CancellationToken cancellationToken = default)
        => projectRepository.GetAllAsync(cancellationToken: cancellationToken);

    public Task<Project?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => projectRepository.GetByIdAsync(id, cancellationToken: cancellationToken);

    public async Task CreateAsync(string name, string type, CancellationToken cancellationToken = default)
    {
        var project = new Project
        {
            Name = name.Trim(),
            Type = type.Trim(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await projectRepository.AddAsync(project, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, string name, string type, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
        if (project is null)
        {
            return false;
        }

        project.Name = name.Trim();
        project.Type = type.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int?> DeleteWithTransactionsAsync(int id, CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(id, cancellationToken: cancellationToken);
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
