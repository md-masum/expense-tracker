using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FinanceTracker.Web.Application.Services;

public enum CompanyMutationStatus
{
    Success,
    NotFound,
    Forbidden,
    ValidationFailed
}

public sealed record CompanyMutationResult(CompanyMutationStatus Status, string? Error = null);

public class CompanyService(
    ICompanyRepository companyRepository,
    IUserCompanyJoinRequestRepository userCompanyJoinRequestRepository,
    ICompanyBannerStorage companyBannerStorage,
    IUnitOfWork unitOfWork,
    UserManager<ApplicationUser> userManager,
    IHttpContextAccessor httpContextAccessor)
{
    public Task<List<Company>> GetDirectoryAsync(CancellationToken cancellationToken = default)
        => companyRepository.GetAllAsync(cancellationToken);

    public async Task<List<Company>?> GetAllForCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var context = httpContextAccessor.HttpContext;
        if (context?.User is null)
        {
            return null;
        }

        var user = await userManager.GetUserAsync(context.User);
        if (user is null)
        {
            return null;
        }

        return await companyRepository.GetByUserAsync(user.Id, cancellationToken);
    }

    public Task<bool> HasAnyCompanyAsync(string userId, CancellationToken cancellationToken = default)
        => companyRepository.ExistsForUserAsync(userId, cancellationToken);

    public async Task<Company?> GetActiveForUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetDefaultForUserAsync(userId, cancellationToken);
        if (company is not null)
        {
            return company;
        }

        var linkedCompanies = await companyRepository.GetByUserAsync(userId, cancellationToken);
        var fallback = linkedCompanies.OrderBy(x => x.Name).FirstOrDefault();
        if (fallback is null)
        {
            return null;
        }

        await ApplyDefaultCompanyAsync(linkedCompanies, userId, fallback.Id, cancellationToken);
        return await companyRepository.GetDefaultForUserAsync(userId, cancellationToken) ?? fallback;
    }

    public Task<List<Company>> GetByUserAsync(string userId, CancellationToken cancellationToken = default)
        => companyRepository.GetByUserAsync(userId, cancellationToken);

    public Task<Company?> GetByIdForUserAsync(int id, string userId, CancellationToken cancellationToken = default)
        => companyRepository.GetByIdForUserAsync(id, userId, cancellationToken);

    public Task<Company?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => companyRepository.GetByIdAsync(id, cancellationToken);

    public async Task<CompanyMutationResult> CreateAsync(
        string ownerUserId,
        string name,
        string? description,
        IFormFile? bannerImage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "Company name is required.");
        }

        var bannerResult = await TrySaveBannerAsync(bannerImage, cancellationToken);
        if (bannerResult.Error is not null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, bannerResult.Error);
        }

        var linkedCompanies = await companyRepository.GetByUserAsync(ownerUserId, cancellationToken);
        ClearDefaultFlags(linkedCompanies, ownerUserId);

        var company = new Company
        {
            Name = name.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            ImageUrl = bannerResult.Path,
            OwnerId = ownerUserId
        };

        company.UserCompanyMaps.Add(new UserCompanyMap
        {
            UserId = ownerUserId,
            IsDefault = true,
            Company = company
        });

        await companyRepository.AddAsync(company, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> UpdateAsync(
        int id,
        string currentUserId,
        string name,
        string? description,
        IFormFile? bannerImage,
        CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        if (company.OwnerId != currentUserId)
        {
            return new CompanyMutationResult(CompanyMutationStatus.Forbidden);
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "Company name is required.");
        }

        var bannerResult = await TrySaveBannerAsync(bannerImage, cancellationToken);
        if (bannerResult.Error is not null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, bannerResult.Error);
        }

        company.Name = name.Trim();
        company.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        if (bannerResult.Path is not null)
        {
            company.ImageUrl = bannerResult.Path;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> DeleteAsync(int id, string currentUserId, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdAsync(id, cancellationToken);
        if (company is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        if (company.OwnerId != currentUserId)
        {
            return new CompanyMutationResult(CompanyMutationStatus.Forbidden);
        }

        companyRepository.Remove(company);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> SetDefaultAsync(int companyId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var linkedCompanies = await companyRepository.GetByUserAsync(currentUserId, cancellationToken);
        var targetCompany = linkedCompanies.FirstOrDefault(x => x.Id == companyId);
        if (targetCompany is null)
        {
            var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
            return company is null
                ? new CompanyMutationResult(CompanyMutationStatus.NotFound)
                : new CompanyMutationResult(CompanyMutationStatus.Forbidden);
        }

        await ApplyDefaultCompanyAsync(linkedCompanies, currentUserId, companyId, cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> RemoveUserAsync(
        int companyId,
        string targetUserId,
        string currentUserId,
        CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        if (company.OwnerId != currentUserId)
        {
            return new CompanyMutationResult(CompanyMutationStatus.Forbidden);
        }

        if (targetUserId == company.OwnerId)
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "Owner cannot be removed from the company.");
        }

        var userMap = await companyRepository.GetUserMapAsync(companyId, targetUserId, cancellationToken);
        if (userMap is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        companyRepository.RemoveUserMap(userMap);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> RequestJoinAsync(int companyId, string currentUserId, CancellationToken cancellationToken = default)
    {
        var company = await companyRepository.GetByIdAsync(companyId, cancellationToken);
        if (company is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        if (company.OwnerId == currentUserId || company.UserCompanyMaps.Any(x => x.UserId == currentUserId))
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "You are already linked with this company.");
        }

        var existingPending = await userCompanyJoinRequestRepository.GetPendingAsync(companyId, currentUserId, cancellationToken);
        if (existingPending is not null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "A join request is already pending for this company.");
        }

        await userCompanyJoinRequestRepository.AddAsync(new UserCompanyJoinRequest
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Status = CompanyJoinRequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    public async Task<CompanyMutationResult> ReviewJoinRequestAsync(
        int requestId,
        string currentUserId,
        bool approve,
        CancellationToken cancellationToken = default)
    {
        var request = await userCompanyJoinRequestRepository.GetByIdAsync(requestId, cancellationToken);
        if (request is null)
        {
            return new CompanyMutationResult(CompanyMutationStatus.NotFound);
        }

        if (request.Company.OwnerId != currentUserId)
        {
            return new CompanyMutationResult(CompanyMutationStatus.Forbidden);
        }

        if (request.Status != CompanyJoinRequestStatus.Pending)
        {
            return new CompanyMutationResult(CompanyMutationStatus.ValidationFailed, "This join request has already been processed.");
        }

        if (approve)
        {
            var existingMap = request.Company.UserCompanyMaps.FirstOrDefault(x => x.UserId == request.UserId);
            if (existingMap is null)
            {
                var linkedCompanies = await companyRepository.GetByUserAsync(request.UserId, cancellationToken);
                var hasDefaultCompany = linkedCompanies.Any(x => x.UserCompanyMaps.Any(m => m.UserId == request.UserId && m.IsDefault));

                request.Company.UserCompanyMaps.Add(new UserCompanyMap
                {
                    CompanyId = request.CompanyId,
                    UserId = request.UserId,
                    IsDefault = !hasDefaultCompany
                });
            }
        }

        request.Status = approve ? CompanyJoinRequestStatus.Approved : CompanyJoinRequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new CompanyMutationResult(CompanyMutationStatus.Success);
    }

    private async Task<(string? Path, string? Error)> TrySaveBannerAsync(IFormFile? bannerImage, CancellationToken cancellationToken)
    {
        if (bannerImage is null)
        {
            return (null, null);
        }

        if (!companyBannerStorage.IsValidImage(bannerImage, out var error))
        {
            return (null, error);
        }

        var path = await companyBannerStorage.SaveAsync(bannerImage, cancellationToken);
        return (path, null);
    }

    private async Task ApplyDefaultCompanyAsync(
        IEnumerable<Company> linkedCompanies,
        string userId,
        int defaultCompanyId,
        CancellationToken cancellationToken)
    {
        foreach (var company in linkedCompanies)
        {
            var userMap = EnsureUserMap(company, userId);
            var isDefault = company.Id == defaultCompanyId;

            userMap.IsDefault = isDefault;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void ClearDefaultFlags(IEnumerable<Company> linkedCompanies, string userId)
    {
        foreach (var company in linkedCompanies)
        {
            var userMap = EnsureUserMap(company, userId);
            userMap.IsDefault = false;
        }
    }

    private static UserCompanyMap EnsureUserMap(Company company, string userId)
    {
        var userMap = company.UserCompanyMaps.FirstOrDefault(x => x.UserId == userId);
        if (userMap is not null)
        {
            return userMap;
        }

        userMap = new UserCompanyMap
        {
            CompanyId = company.Id,
            UserId = userId
        };

        company.UserCompanyMaps.Add(userMap);
        return userMap;
    }
}

