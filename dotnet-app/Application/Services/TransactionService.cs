using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Application.Models;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public class TransactionService(
    IProjectRepository projectRepository,
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork)
{
    private const int DefaultPageSize = 25;

    public async Task<TransactionListResult?> GetListAsync(
        int projectId,
        string? search,
        TransactionType? type,
        int? categoryId,
        int page,
        CancellationToken cancellationToken = default)
    {
        var project = await projectRepository.GetByIdAsync(projectId, cancellationToken: cancellationToken);
        if (project is null)
        {
            return null;
        }

        var categories = await categoryRepository.GetAllAsync(cancellationToken);
        var (items, totalCount) = await transactionRepository.GetPagedByProjectAsync(
            projectId,
            search,
            type,
            categoryId,
            page,
            DefaultPageSize,
            cancellationToken);

        var allProjectTransactions = await transactionRepository.GetByProjectAsync(projectId, cancellationToken);
        var income = allProjectTransactions.Where(x => x.Type == TransactionType.Income).Sum(x => x.Amount);
        var expense = allProjectTransactions.Where(x => x.Type == TransactionType.Expense).Sum(x => x.Amount);

        return new TransactionListResult
        {
            Project = project,
            Items = items,
            Categories = categories,
            TotalCount = totalCount,
            Page = page,
            PageSize = DefaultPageSize,
            Search = search,
            Type = type,
            CategoryId = categoryId,
            Income = income,
            Expense = expense,
            Balance = income - expense
        };
    }

    public Task<FinanceTransaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => transactionRepository.GetByIdAsync(id, cancellationToken);

    public async Task CreateAsync(
        int projectId,
        int categoryId,
        TransactionType type,
        decimal amount,
        DateTime date,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var seqNo = await transactionRepository.GetNextSeqNoAsync(projectId, cancellationToken);

        await transactionRepository.AddAsync(new FinanceTransaction
        {
            ProjectId = projectId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Date = date,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            SeqNo = seqNo,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(
        int id,
        int categoryId,
        TransactionType type,
        decimal amount,
        DateTime date,
        string? note,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transaction.CategoryId = categoryId;
        transaction.Type = type;
        transaction.Amount = amount;
        transaction.Date = date;
        transaction.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transactionRepository.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
