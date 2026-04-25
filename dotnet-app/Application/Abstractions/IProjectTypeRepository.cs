using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface IProjectTypeRepository
{
    Task<List<ProjectType>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ProjectType?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(ProjectType projectType, CancellationToken cancellationToken = default);
    void Update(ProjectType projectType);
    void Remove(ProjectType projectType);
}

