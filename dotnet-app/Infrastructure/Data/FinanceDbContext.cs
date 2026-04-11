using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Data;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : IdentityDbContext<ApplicationUser>(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinanceTransaction> Transactions => Set<FinanceTransaction>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<UserCompanyMap> UserCompanyMaps => Set<UserCompanyMap>();
    public DbSet<UserCompanyJoinRequest> UserCompanyJoinRequests => Set<UserCompanyJoinRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(60).IsRequired();
            entity.HasIndex(x => new { x.Name, x.Type }).IsUnique();

            entity.HasData(
                new Category { Id = 1, Name = "Sand", Type = TransactionType.Expense },
                new Category { Id = 2, Name = "Labour", Type = TransactionType.Expense },
                new Category { Id = 3, Name = "Brick", Type = TransactionType.Expense },
                new Category { Id = 4, Name = "Materials", Type = TransactionType.Expense },
                new Category { Id = 5, Name = "Investment", Type = TransactionType.Income },
                new Category { Id = 6, Name = "Sale", Type = TransactionType.Income }
            );
        });

        modelBuilder.Entity<FinanceTransaction>(entity =>
        {
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.Note).HasMaxLength(500);

            entity.HasOne(x => x.Project)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Category)
                .WithMany(x => x.Transactions)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => new { x.ProjectId, x.Date });
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(1000);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.HasIndex(x => x.Name);
            entity.HasIndex(x => x.OwnerId);

            entity.HasOne(x => x.Owner)
                .WithMany(x => x.OwnedCompanies)
                .HasForeignKey(x => x.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<UserCompanyMap>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.UserId }).IsUnique();
            entity.HasIndex(x => x.UserId);

            entity.HasOne(x => x.Company)
                .WithMany(x => x.UserCompanyMaps)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.UserCompanyMaps)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserCompanyJoinRequest>(entity =>
        {
            entity.HasIndex(x => new { x.CompanyId, x.UserId, x.Status });
            entity.HasIndex(x => new { x.UserId, x.Status });

            entity.HasOne(x => x.Company)
                .WithMany(x => x.JoinRequests)
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany(x => x.CompanyJoinRequests)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
