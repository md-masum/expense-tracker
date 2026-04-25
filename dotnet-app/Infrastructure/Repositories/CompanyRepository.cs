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
            .ThenInclude(x => x.User)
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
            .ThenInclude(x => x.User)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .Where(x => x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId))
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Company?> GetDefaultForUserAsync(string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Where(x => x.UserCompanyMaps.Any(m => m.UserId == userId && m.IsDefault))
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<UserCompanyMap?> GetUserMapAsync(int companyId, string userId, CancellationToken cancellationToken = default)
        => dbContext.UserCompanyMaps
            .Include(x => x.Company)
            .ThenInclude(x => x.Owner)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.UserId == userId, cancellationToken);

    public Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .ThenInclude(x => x.User)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<Company?> GetByIdForUserAsync(int id, string userId, CancellationToken cancellationToken = default)
        => dbContext.Companies
            .Include(x => x.Owner)
            .Include(x => x.UserCompanyMaps)
            .ThenInclude(x => x.User)
            .Include(x => x.JoinRequests)
            .ThenInclude(x => x.User)
            .FirstOrDefaultAsync(
                x => x.Id == id && (x.OwnerId == userId || x.UserCompanyMaps.Any(m => m.UserId == userId)),
                cancellationToken);

    public Task AddAsync(Company company, CancellationToken cancellationToken = default)
        => dbContext.Companies.AddAsync(company, cancellationToken).AsTask();

    public void RemoveUserMap(UserCompanyMap userCompanyMap)
        => dbContext.UserCompanyMaps.Remove(userCompanyMap);

    public void Remove(Company company)
        => dbContext.Companies.Remove(company);
}
