using FinanceTracker.Web.Application.Abstractions;
using FinanceTracker.Web.Application.Services;
using FinanceTracker.Web.Infrastructure.Data;
using FinanceTracker.Web.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<FinanceDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<FinanceDbContext>());
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();

builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ProjectService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<BackupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Dashboard/Index");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
