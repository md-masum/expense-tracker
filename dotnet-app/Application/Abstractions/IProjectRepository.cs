using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface IProjectRepository
{
    Task<List<Project>> GetAllAsync(int companyId, bool includeTransactions = false, CancellationToken cancellationToken = default);
    Task<Project?> GetByIdAsync(int id, int companyId, bool includeTransactions = false, CancellationToken cancellationToken = default);
    Task AddAsync(Project project, CancellationToken cancellationToken = default);
    void Remove(Project project);
    Task<bool> AnyByTypeAsync(int projectTypeId, CancellationToken cancellationToken = default);
}
