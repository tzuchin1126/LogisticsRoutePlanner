using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using LogisticsRoutePlanner.Models;

namespace LogisticsRoutePlanner.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    // 注入記錄器，用來寫入 Log
    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View(); 
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
