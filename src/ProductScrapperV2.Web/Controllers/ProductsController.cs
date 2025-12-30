using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Domain.Entities;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Controllers;

public class ProductsController : Controller
{
    private readonly AppDbContext _dbContext;

    public ProductsController(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _dbContext.Products
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        return View(products);
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Products.Add(new Product
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Sku = model.Sku,
            OwnPrice = model.OwnPrice
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> QuickSearch(QuickSearchViewModel model)
    {
        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Name))
        {
            return RedirectToAction(nameof(Index));
        }

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            OwnPrice = model.OwnPrice
        };

        _dbContext.Products.Add(product);
        _dbContext.ScrapeJobs.Add(new ScrapeJob
        {
            Id = Guid.NewGuid(),
            ProductId = product.Id,
            Status = JobStatus.Pending,
            AttemptCount = 0
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> TriggerScrape(Guid id)
    {
        var exists = await _dbContext.Products.AnyAsync(p => p.Id == id);
        if (!exists)
        {
            return RedirectToAction(nameof(Index));
        }

        _dbContext.ScrapeJobs.Add(new ScrapeJob
        {
            Id = Guid.NewGuid(),
            ProductId = id,
            Status = JobStatus.Pending,
            AttemptCount = 0
        });

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> TriggerAll()
    {
        var productIds = await _dbContext.Products.Select(p => p.Id).ToListAsync();
        foreach (var productId in productIds)
        {
            _dbContext.ScrapeJobs.Add(new ScrapeJob
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                Status = JobStatus.Pending,
                AttemptCount = 0
            });
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
