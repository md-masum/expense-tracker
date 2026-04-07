using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface ICategoryRepository
{
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Category category, CancellationToken cancellationToken = default);
    void Remove(Category category);
}
