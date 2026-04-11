using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Application.Abstractions;

public interface IUserCompanyJoinRequestRepository
{
    Task<UserCompanyJoinRequest?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserCompanyJoinRequest?> GetPendingAsync(int companyId, string userId, CancellationToken cancellationToken = default);
    Task AddAsync(UserCompanyJoinRequest request, CancellationToken cancellationToken = default);
}

