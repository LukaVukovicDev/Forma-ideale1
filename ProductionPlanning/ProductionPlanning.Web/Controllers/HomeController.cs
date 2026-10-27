using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ProductionPlanning.Web.Models;
using ProductionPlanning.Web.Services;
using ProductionPlanning.Web.ViewModels;

namespace ProductionPlanning.Web.Controllers;

public class HomeController : Controller
{
    private static readonly TimeZoneInfo BelgradeTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows()
                ? "Central European Standard Time"
                : "Europe/Belgrade");

    private readonly DashboardStatisticsService _statisticsService;

    public HomeController(DashboardStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    public IActionResult Index()
    {
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, BelgradeTimeZone).DateTime);

        return View(new HomeDashboardViewModel
        {
            Statistics = _statisticsService.GetStatistics(today)
        });
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
