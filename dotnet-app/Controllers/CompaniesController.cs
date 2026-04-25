using System.Security.Claims;
using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Web.Controllers;

[Authorize]
public class CompaniesController(CompanyService companyService) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var model = new CompaniesIndexViewModel
        {
            CurrentUserId = userId,
            HasAnyCompany = await companyService.HasAnyCompanyAsync(userId, cancellationToken),
            Companies = await companyService.GetDirectoryAsync(cancellationToken)
        };

        return View(model);
    }

    public IActionResult Create() => View(new CompanyInputModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CompanyInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["ModalError"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                TempData["ModalTarget"] = "createCompanyOffcanvas";
                return RedirectToAction(nameof(Index));
            }
            return View(input);
        }

        var result = await companyService.CreateAsync(userId, input.Name, input.Description, input.BannerImage, cancellationToken);
        if (result.Status == CompanyMutationStatus.ValidationFailed)
        {
            if (fromModal)
            {
                TempData["ModalError"] = result.Error ?? "Invalid banner image.";
                TempData["ModalTarget"] = "createCompanyOffcanvas";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(nameof(input.BannerImage), result.Error ?? "Invalid banner image.");
            return View(input);
        }

        TempData["Success"] = "Company created.";
        return RedirectToAction("Index", "Dashboard");
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var company = await companyService.GetByIdForUserAsync(id, userId, cancellationToken);
        if (company is null)
        {
            return NotFound();
        }

        if (company.OwnerId != userId)
        {
            return Forbid();
        }

        return View(new CompanyInputModel
        {
            Id = company.Id,
            Name = company.Name,
            Description = company.Description,
            ExistingImageUrl = company.ImageUrl
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(CompanyInputModel input, CancellationToken cancellationToken)
    {
        var fromModal = Request.Form["fromModal"] == "true";
        var userId = GetCurrentUserId();
        if (userId is null) return Challenge();

        if (!ModelState.IsValid)
        {
            if (fromModal)
            {
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage ?? "Validation failed.";
                return RedirectToAction(nameof(Index));
            }
            return View(input);
        }

        var result = await companyService.UpdateAsync(input.Id, userId, input.Name, input.Description, input.BannerImage, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound) return NotFound();
        if (result.Status == CompanyMutationStatus.Forbidden) return Forbid();
        if (result.Status == CompanyMutationStatus.ValidationFailed)
        {
            if (fromModal)
            {
                TempData["Error"] = result.Error ?? "Invalid banner image.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(nameof(input.BannerImage), result.Error ?? "Invalid banner image.");
            return View(input);
        }

        TempData["Success"] = "Company updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await companyService.DeleteAsync(id, userId, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == CompanyMutationStatus.Forbidden)
        {
            return Forbid();
        }

        TempData["Success"] = "Company deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetDefault(int companyId, string? returnUrl, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await companyService.SetDefaultAsync(companyId, userId, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == CompanyMutationStatus.Forbidden)
        {
            return Forbid();
        }

        TempData["Success"] = "Active company updated.";
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RequestJoin(int companyId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await companyService.RequestJoinAsync(companyId, userId, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound)
        {
            return NotFound();
        }

        TempData[result.Status == CompanyMutationStatus.Success ? "Success" : "Error"] =
            result.Status == CompanyMutationStatus.Success
                ? "Join request submitted."
                : result.Error ?? "Unable to submit join request.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveUser(int companyId, string userId, CancellationToken cancellationToken)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Challenge();
        }

        var result = await companyService.RemoveUserAsync(companyId, userId, currentUserId, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == CompanyMutationStatus.Forbidden)
        {
            return Forbid();
        }

        TempData[result.Status == CompanyMutationStatus.Success ? "Success" : "Error"] =
            result.Status == CompanyMutationStatus.Success
                ? "User removed from company."
                : result.Error ?? "Unable to remove user from company.";

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveJoinRequest(int requestId, CancellationToken cancellationToken)
    {
        return await ReviewJoinRequestAsync(requestId, approve: true, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectJoinRequest(int requestId, CancellationToken cancellationToken)
    {
        return await ReviewJoinRequestAsync(requestId, approve: false, cancellationToken);
    }

    private string? GetCurrentUserId()
        => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private async Task<IActionResult> ReviewJoinRequestAsync(int requestId, bool approve, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Challenge();
        }

        var result = await companyService.ReviewJoinRequestAsync(requestId, userId, approve, cancellationToken);
        if (result.Status == CompanyMutationStatus.NotFound)
        {
            return NotFound();
        }

        if (result.Status == CompanyMutationStatus.Forbidden)
        {
            return Forbid();
        }

        TempData[result.Status == CompanyMutationStatus.Success ? "Success" : "Error"] =
            result.Status == CompanyMutationStatus.Success
                ? (approve ? "Join request approved." : "Join request rejected.")
                : result.Error ?? "Unable to review join request.";

        return RedirectToAction(nameof(Index));
    }
}

