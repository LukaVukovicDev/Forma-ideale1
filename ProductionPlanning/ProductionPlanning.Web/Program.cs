using Microsoft.EntityFrameworkCore;
using ProductionPlanning.Web.Data;
using ProductionPlanning.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<MachineStatisticsService>();
builder.Services.AddSingleton<DashboardStatisticsService>();
builder.Services.AddScoped<ProductStatisticsService>();
builder.Services.AddSingleton<RouteCalculationService>();
builder.Services.AddSingleton<RouteSequenceService>();
builder.Services.AddScoped<ProductRouteService>();
builder.Services.AddScoped<MachineAvailabilityService>();
builder.Services.AddSingleton<DemoOperationCatalogImporter>();

var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

if (args.Contains("--import-demo-operations", StringComparer.Ordinal))
{
    if (!app.Environment.IsDevelopment())
    {
        throw new InvalidOperationException(
            "Katalog demonstracionih operacija može se uvesti samo u Development okruženju.");
    }

    Directory.SetCurrentDirectory(app.Environment.ContentRootPath);
    var importer = app.Services.GetRequiredService<DemoOperationCatalogImporter>();
    await importer.RunAsync(
        app.Services,
        app.Environment.ContentRootPath,
        connectionString);
    return;
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
