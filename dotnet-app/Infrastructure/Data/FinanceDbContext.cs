using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinanceTracker.Web.Infrastructure.Data;

public class FinanceDbContext(DbContextOptions<FinanceDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<FinanceTransaction> Transactions => Set<FinanceTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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

        base.OnModelCreating(modelBuilder);
    }
}
