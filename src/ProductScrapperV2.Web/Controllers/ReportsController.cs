using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Infrastructure.Data;

namespace ProductScrapperV2.Web.Controllers;

public class ReportsController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly IPriceComparisonService _comparisonService;
    private readonly IExcelExportService _excelExportService;

    public ReportsController(
        AppDbContext dbContext,
        IPriceComparisonService comparisonService,
        IExcelExportService excelExportService)
    {
        _dbContext = dbContext;
        _comparisonService = comparisonService;
        _excelExportService = excelExportService;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products.ToListAsync();
        var comparisons = await _comparisonService.CompareBulkAsync(products.Select(p => p.Id).ToList(), HttpContext.RequestAborted);
        return View(comparisons);
    }

    public async Task<IActionResult> Export()
    {
        var products = await _dbContext.Products.ToListAsync();
        var comparisons = await _comparisonService.CompareBulkAsync(products.Select(p => p.Id).ToList(), HttpContext.RequestAborted);
        var content = await _excelExportService.ExportPriceComparisonAsync(comparisons, HttpContext.RequestAborted);
        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "price-report.xlsx");
    }
}
