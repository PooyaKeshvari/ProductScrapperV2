using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductScrapperV2.Domain.Entities;
using ProductScrapperV2.Infrastructure.Data;
using ProductScrapperV2.Application.Interfaces;
using ProductScrapperV2.Web.ViewModels;

namespace ProductScrapperV2.Web.Controllers;

public class CompetitorsController : Controller
{
    private readonly AppDbContext _dbContext;
    private readonly ICompetitorDiscoveryService _discoveryService;

    public CompetitorsController(AppDbContext dbContext, ICompetitorDiscoveryService discoveryService)
    {
        _dbContext = dbContext;
        _discoveryService = discoveryService;
    }

    public async Task<IActionResult> Index()
    {
        var competitors = await _dbContext.Competitors
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
        return View(competitors);
    }

    [HttpPost]
    public async Task<IActionResult> Create(CompetitorFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        _dbContext.Competitors.Add(new Competitor
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            WebsiteUrl = model.WebsiteUrl,
            IsAutoDiscovered = false
        });
        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Discover(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return RedirectToAction(nameof(Index));
        }

        await _discoveryService.DiscoverCompetitorsAsync(productName, HttpContext.RequestAborted);
        return RedirectToAction(nameof(Index));
    }
}
