using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface ICompanyRepository
{
    Task<List<Company>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsForUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<bool> IsUserLinkedAsync(int companyId, string userId, CancellationToken cancellationToken = default);
    Task<List<Company>> GetByUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Company?> GetByIdForUserAsync(int id, string userId, CancellationToken cancellationToken = default);
    Task AddAsync(Company company, CancellationToken cancellationToken = default);
    void Remove(Company company);
}

