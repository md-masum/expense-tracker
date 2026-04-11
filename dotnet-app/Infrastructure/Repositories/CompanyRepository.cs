using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class CompanyRepository(FinanceDbContext dbContext) : ICompanyRepository
{
    public Task<List<Company>> GetAllAsync(CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsForUserAsync(string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .AnyAsync(x => x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId), cancellationToken);

    public Task<bool> IsUserLinkedAsync(int companyId, string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .AnyAsync(x => x.Id == companyId && (x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId)), cancellationToken);

    public Task<List<Company>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .Include(x => x.JoinRequests)
            .Where(x => x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Company?> GetByIdForUserAsync(int id, string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Id == id && (x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId)),
                cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        => dbContext.Companies.AddAsync(company, cancellationToken).AsTask();

    public void Remove(Company company)
        => dbContext.Companies.Remove(company);
}

