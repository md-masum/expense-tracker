using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class TransactionRepository(FinanceDbContext dbContext) : ITransactionRepository
{
    public async Task<(IReadOnlyList<FinanceTransaction> Items, int TotalCount)> GetPagedByProjectAsync(
        int projectId,
        string? search,
        TransactionType? type,
        int? categoryId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Transactions
            .Include(x => x.Category)
            .Where(x => x.ProjectId == projectId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(x => x.Note != null && x.Note.ToLower().Contains(term));
        }

        if (type.HasValue)
        {
            query = query.Where(x => x.Type == type.Value);
        }

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.SeqNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<List<FinanceTransaction>> GetByProjectAsync(int projectId, CancellationToken cancellationToken = default)
        => dbContext.Transactions
            .Include(x => x.Category)
            .Where(x => x.ProjectId == projectId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.SeqNo)
            .ToListAsync(cancellationToken);

    public Task<List<FinanceTransaction>> GetAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Transactions.AsNoTracking().ToListAsync(cancellationToken);

    public Task<FinanceTransaction?> GetByIdAsync(int id, int companyId, CancellationToken cancellationToken = default)
        => dbContext.Transactions
            .Include(x => x.Category)
            .Include(x => x.Project)
            .FirstOrDefaultAsync(x => x.Id == id && x.Project != null && x.Project.CompanyId == companyId, cancellationToken);

    public Task AddAsync(FinanceTransaction transaction, CancellationToken cancellationToken = default)
        => dbContext.Transactions.AddAsync(transaction, cancellationToken).AsTask();

    public void Remove(FinanceTransaction transaction)
        => dbContext.Transactions.Remove(transaction);

    public async Task<int> GetNextSeqNoAsync(int projectId, CancellationToken cancellationToken = default)
    {
        var max = await dbContext.Transactions
            .Where(x => x.ProjectId == projectId)
            .MaxAsync(x => (int?)x.SeqNo, cancellationToken);
        return (max ?? 0) + 1;
    }

    public async Task<int> DeleteByProjectAsync(int projectId, CancellationToken cancellationToken = default)
    {
        const int chunkSize = 400;
        var deleted = 0;

        while (true)
        {
            var batch = await dbContext.Transactions
                .Where(x => x.ProjectId == projectId)
                .Take(chunkSize)
                .ToListAsync(cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            dbContext.Transactions.RemoveRange(batch);
            deleted += batch.Count;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return deleted;
    }

    public Task<bool> AnyByCategoryAsync(int categoryId, CancellationToken cancellationToken = default)
        => dbContext.Transactions.AnyAsync(x => x.CategoryId == categoryId, cancellationToken);
}
