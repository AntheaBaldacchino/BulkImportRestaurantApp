using BulkImportRestaurantApp.Data;
using BulkImportRestaurantApp.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BulkImportRestaurantApp.Infrastructure;
using System.Diagnostics;
using BulkImportRestaurantApp.Factories;
using BulkImportRestaurantApp.Filters;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// caching for in-memory repo
builder.Services.AddMemoryCache();

builder.Services.AddScoped<ImportItemFactory>();
builder.Services.AddScoped<ApprovalAuthorizeFilter>();

builder.Services.AddKeyedScoped<ItemsInMemoryRepository>("memory");
builder.Services.AddKeyedScoped<IItemsRepository, ItemsInMemoryRepository>("memory");

builder.Services.AddKeyedScoped<ItemsDbRepository>("database");
builder.Services.AddKeyedScoped<IItemsRepository, ItemsDbRepository>("database");


builder.Services.AddScoped<ItemsDbRepository>();

AppSettings.SiteAdminEmail = builder.Configuration["SiteAdmin:Email"];

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();


app.Run();
