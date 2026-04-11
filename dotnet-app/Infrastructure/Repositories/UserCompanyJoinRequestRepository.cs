using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using FinanceTracker.Web.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Repositories;

public class UserCompanyJoinRequestRepository(FinanceDbContext dbContext) : IUserCompanyJoinRequestRepository
{
    public Task<UserCompanyJoinRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => dbContext.UserCompanyJoinRequests
            .Include(x => x.Company)
            .ThenInclude(x => x.UserCompanyMaps)
            .Include(x => x.Company)
            .ThenInclude(x => x.Owner)
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<UserCompanyJoinRequest?> GetPendingAsync(int companyId, string userId, CancellationToken cancellationToken = default)
        => dbContext.UserCompanyJoinRequests
            .FirstOrDefaultAsync(
                x => x.CompanyId == companyId && x.UserId == userId && x.Status == CompanyJoinRequestStatus.Pending,
                cancellationToken);

    public Task AddAsync(UserCompanyJoinRequest request, CancellationToken cancellationToken = default)
        => dbContext.UserCompanyJoinRequests.AddAsync(request, cancellationToken).AsTask();
}



