using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FinanceTracker.Web.ViewModels;

public class CompanyInputModel
{
    public int Id { get; set; }

    [Required, StringLength(150)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    public IFormFile? BannerImage { get; set; }

    public string? ExistingImageUrl { get; set; }
}

