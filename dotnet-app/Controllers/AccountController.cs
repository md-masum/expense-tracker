using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.Web.Domain.Entities;

namespace FinanceTracker.Web.Controllers;

[AllowAnonymous]
public class AccountController(
    SignInManager<ApplicationUser> signInManager,
    UserManager<ApplicationUser> userManager,
    CompanyService companyService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Login(string? returnUrl = null)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            return await RedirectToLocalOrOnboardingAsync(returnUrl);
        }

        return View(new LoginInputModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginInputModel input)
    {
        input.ReturnUrl = NormalizeReturnUrl(input.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = await userManager.FindByEmailAsync(input.Email);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(input);
        }

        var result = await signInManager.PasswordSignInAsync(user, input.Password, isPersistent: input.RememberMe, lockoutOnFailure: false);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Invalid email or password.");
            return View(input);
        }

        TempData["Success"] = "Signed in successfully.";
        return await RedirectToLocalOrOnboardingAsync(input.ReturnUrl, user.Id);
    }

    [HttpGet]
    public async Task<IActionResult> Register(string? returnUrl = null)
    {
        returnUrl = NormalizeReturnUrl(returnUrl);

        if (User.Identity?.IsAuthenticated == true)
        {
            return await RedirectToLocalOrOnboardingAsync(returnUrl);
        }

        return View(new RegisterInputModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterInputModel input)
    {
        input.ReturnUrl = NormalizeReturnUrl(input.ReturnUrl);

        if (!ModelState.IsValid)
        {
            return View(input);
        }

        var user = new ApplicationUser
        {
            UserName = input.Email,
            Email = input.Email
        };

        var result = await userManager.CreateAsync(user, input.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(input);
        }

        await signInManager.SignInAsync(user, isPersistent: true);
        TempData["Success"] = "Account created.";
        return await RedirectToLocalOrOnboardingAsync(input.ReturnUrl, user.Id);
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await signInManager.SignOutAsync();
        TempData["Success"] = "Signed out.";
        return RedirectToAction(nameof(Login));
    }

    private async Task<IActionResult> RedirectToLocalOrOnboardingAsync(string? returnUrl, string? userId = null)
    {
        var resolvedUserId = userId ?? userManager.GetUserId(User);
        if (!string.IsNullOrWhiteSpace(resolvedUserId))
        {
            var hasCompany = await companyService.HasAnyCompanyAsync(resolvedUserId, HttpContext.RequestAborted);
            if (!hasCompany)
            {
                return RedirectToAction("Index", "Companies");
            }
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }
}


