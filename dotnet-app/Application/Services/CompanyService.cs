using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using Microsoft.AspNetCore.Http;

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
    IUnitOfWork unitOfWork)
{
    public Task<List<Company>> GetDirectoryAsync(CancellationToken cancellationToken = default)
        => companyRepository.GetAllAsync(cancellationToken);

    public Task<bool> HasAnyCompanyAsync(string userId, CancellationToken cancellationToken = default)
        => companyRepository.ExistsForUserAsync(userId, cancellationToken);

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

        if (approve && request.Company.UserCompanyMaps.All(x => x.UserId != request.UserId))
        {
            request.Company.UserCompanyMaps.Add(new UserCompanyMap
            {
                CompanyId = request.CompanyId,
                UserId = request.UserId
            });
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
}

