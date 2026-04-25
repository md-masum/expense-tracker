using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class ProjectRepository(FinanceDbContext dbContext) : IProjectRepository
{
    public Task<List<Project>> GetAllAsync(int companyId, bool includeTransactions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = dbContext.Projects
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.Type);

        if (includeTransactions)
        {
            query = query.Include(x => x.Transactions);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(int id, int companyId, bool includeTransactions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = dbContext.Projects
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.Type);

        if (includeTransactions)
        {
            query = query.Include(x => x.Transactions);
        }

        return query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task AddAsync(Project project, CancellationToken cancellationToken = default)
        => dbContext.Projects.AddAsync(project, cancellationToken).AsTask();

    public void Remove(Project project)
        => dbContext.Projects.Remove(project);

    public Task<bool> AnyByTypeAsync(int projectTypeId, CancellationToken cancellationToken = default)
        => dbContext.Projects.AnyAsync(x => x.ProjectTypeId == projectTypeId, cancellationToken);
}
