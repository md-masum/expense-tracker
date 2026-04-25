using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class ProjectTypeRepository(FinanceDbContext dbContext) : IProjectTypeRepository
{
    public Task<List<ProjectType>> GetAllAsync(CancellationToken cancellationToken = default)
        => dbContext.ProjectTypes
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<ProjectType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.ProjectTypes.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(ProjectType projectType, CancellationToken cancellationToken = default)
        => dbContext.ProjectTypes.AddAsync(projectType, cancellationToken).AsTask();

    public void Update(ProjectType projectType)
        => dbContext.ProjectTypes.Update(projectType);

    public void Remove(ProjectType projectType)
        => dbContext.ProjectTypes.Remove(projectType);
}

