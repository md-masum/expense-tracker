using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Services;

public class CategoryService(
    ICategoryRepository categoryRepository,
    ITransactionRepository transactionRepository,
    IUnitOfWork unitOfWork)
{
    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken = default)
        => categoryRepository.GetAllAsync(cancellationToken);

    public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => categoryRepository.GetByIdAsync(id, cancellationToken);

    public async Task CreateAsync(string name, TransactionType type, CancellationToken cancellationToken = default)
    {
        await categoryRepository.AddAsync(new Category
        {
            Name = name.Trim(),
            Type = type
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> UpdateAsync(int id, string name, TransactionType type, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        category.Name = name.Trim();
        category.Type = type;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var category = await categoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
        {
            return false;
        }

        if (await transactionRepository.AnyByCategoryAsync(id, cancellationToken))
        {
            throw new InvalidOperationException("This category is already used by transactions.");
        }

        categoryRepository.Remove(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
