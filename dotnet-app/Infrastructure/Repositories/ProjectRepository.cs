using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class ProjectRepository(FinanceDbContext dbContext) : IProjectRepository
{
    public Task<List<Project>> GetAllAsync(bool includeTransactions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = dbContext.Projects;

        if (includeTransactions)
        {
            query = query.Include(x => x.Transactions);
        }

        return query.OrderByDescending(x => x.CreatedAt).ToListAsync(cancellationToken);
    }

    public Task<Project?> GetByIdAsync(int id, bool includeTransactions = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Project> query = dbContext.Projects;

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
}
