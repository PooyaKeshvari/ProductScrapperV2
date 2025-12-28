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
}
