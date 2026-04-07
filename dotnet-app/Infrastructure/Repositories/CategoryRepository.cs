using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class CategoryRepository(FinanceDbContext dbContext) : ICategoryRepository
{
    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Categories
            .OrderBy(x => x.Type)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Categories.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        => dbContext.Categories.AddAsync(category, cancellationToken).AsTask();

    public void Remove(Category category)
        => dbContext.Categories.Remove(category);
}
