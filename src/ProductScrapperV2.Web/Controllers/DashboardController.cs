using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Controllers;

public class DashboardController : Controller
{
    private readonly AppDbContext _dbContext;

    public DashboardController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var model = new DashboardViewModel
        {
            ProductCount = await _dbContext.Products.CountAsync(),
            CompetitorCount = await _dbContext.Competitors.CountAsync(),
            PriceRecordCount = await _dbContext.PriceRecords.CountAsync(),
            AverageMatchPercentage = await _dbContext.PriceRecords.AnyAsync()
                ? await _dbContext.PriceRecords.AverageAsync(p => p.MatchPercentage)
                : 0
        };

        return View(model);
    }
}
