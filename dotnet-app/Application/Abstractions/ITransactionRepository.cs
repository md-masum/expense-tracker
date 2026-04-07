using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface ITransactionRepository
{
    Task<(IReadOnlyList<FinanceTransaction> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        string? search,
        TransactionType? type,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<List<FinanceTransaction>> GetByProjectAsync(int projectId, CancellationToken cancellationToken = default);
    Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<FinanceTransaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default);
    void Remove(FinanceTransaction transaction);
    Task<int> GetNextSeqNoAsync(int projectId, CancellationToken cancellationToken = default);
    Task<int> DeleteByProjectAsync(int projectId, CancellationToken cancellationToken = default);
    Task<bool> AnyByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
}
