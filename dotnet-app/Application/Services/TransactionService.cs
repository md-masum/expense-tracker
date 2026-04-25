using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Application.Models;
using FinanceTracker.Web.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Web.Application.Services;

public class TransactionService(
    IProjectRepository projectRepository,
    ITransactionRepository transactionRepository,
    ICategoryRepository categoryRepository,
    ITransactionInvoiceStorage transactionInvoiceStorage,
    ActiveCompanyContext activeCompanyContext,
    IUnitOfWork unitOfWork)
{
    private const int DefaultPageSize = 25;

    private int GetRequiredCompanyId()
        => activeCompanyContext.CompanyId
           ?? throw new InvalidOperationException("No active company is selected.");

    public async Task<TransactionListResult?> GetListAsync(
        int projectId,
        string? search,
        TransactionType? type,
        int? categoryId,
        int page,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetRequiredCompanyId();
        var project = await projectRepository.GetByIdAsync(projectId, companyId, cancellationToken: cancellationToken);
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
         var credit = allProjectTransactions.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount);
         var debit = allProjectTransactions.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount);

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
             Credit = credit,
             Debit = debit,
             Balance = credit - debit
         };
    }

    public Task<FinanceTransaction?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => transactionRepository.GetByIdAsync(id, GetRequiredCompanyId(), cancellationToken);

    public async Task CreateAsync(
        int projectId,
        int categoryId,
        TransactionType type,
        decimal amount,
        DateTime date,
        string? note,
        IFormFile? invoiceImage,
        CancellationToken cancellationToken = default)
    {
        var companyId = GetRequiredCompanyId();
        var project = await projectRepository.GetByIdAsync(projectId, companyId, cancellationToken: cancellationToken);
        if (project is null)
        {
            throw new InvalidOperationException("Project not found in active company.");
        }

        var seqNo = await transactionRepository.GetNextSeqNoAsync(projectId, cancellationToken);

        string? invoiceImageUrl = null;
        if (invoiceImage is not null)
        {
            if (!transactionInvoiceStorage.IsValidImage(invoiceImage, out var errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(invoiceImage));
            }

            invoiceImageUrl = await transactionInvoiceStorage.SaveAsync(invoiceImage, cancellationToken);
        }

        await transactionRepository.AddAsync(new FinanceTransaction
        {
            ProjectId = projectId,
            CategoryId = categoryId,
            Type = type,
            Amount = amount,
            Date = date,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            InvoiceImageUrl = invoiceImageUrl,
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
        IFormFile? invoiceImage,
        bool removeInvoiceImage,
        CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, GetRequiredCompanyId(), cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        transaction.CategoryId = categoryId;
        transaction.Type = type;
        transaction.Amount = amount;
        transaction.Date = date;
        transaction.Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        string? oldImagePathToDelete = null;

        if (removeInvoiceImage && !string.IsNullOrWhiteSpace(transaction.InvoiceImageUrl))
        {
            oldImagePathToDelete = transaction.InvoiceImageUrl;
            transaction.InvoiceImageUrl = null;
        }

        if (invoiceImage is not null)
        {
            if (!transactionInvoiceStorage.IsValidImage(invoiceImage, out var errorMessage))
            {
                throw new ArgumentException(errorMessage, nameof(invoiceImage));
            }

            var newImageUrl = await transactionInvoiceStorage.SaveAsync(invoiceImage, cancellationToken);
            if (!string.IsNullOrWhiteSpace(transaction.InvoiceImageUrl))
            {
                oldImagePathToDelete = transaction.InvoiceImageUrl;
            }

            transaction.InvoiceImageUrl = newImageUrl;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(oldImagePathToDelete))
        {
            await transactionInvoiceStorage.DeleteAsync(oldImagePathToDelete, cancellationToken);
        }

        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var transaction = await transactionRepository.GetByIdAsync(id, GetRequiredCompanyId(), cancellationToken);
        if (transaction is null)
        {
            return false;
        }

        var imageToDelete = transaction.InvoiceImageUrl;
        transactionRepository.Remove(transaction);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(imageToDelete))
        {
            await transactionInvoiceStorage.DeleteAsync(imageToDelete, cancellationToken);
        }

        return true;
    }
}
