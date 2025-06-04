using FrogCar.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class FilterController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly SortStrategyContext _sortStrategyContext;

    public FilterController(AppDbContext context)
    {
        _context = context;
        _sortStrategyContext = new SortStrategyContext();
    }

    [HttpGet("filter")]
    public async Task<IActionResult> Filter(string sortBy, bool ascending = true)
    {
        if (string.IsNullOrEmpty(sortBy))
            return BadRequest("Parametr sortBy jest wymagany.");

        var query = _context.CarListing.Where(c => c.IsApproved && c.IsAvailable);
        var sortStrategy = _sortStrategyContext.GetStrategy(sortBy);
        query = sortStrategy.ApplySort(query, ascending);

        return Ok(await query.ToListAsync());
    }
    [HttpGet("by-price-desc")]
    public async Task<IActionResult> GetByPriceDesc()
    {
        return await Filter("price", false);
    }

    [HttpGet("by-price-asc")]
    public async Task<IActionResult> GetByPriceAsc()
    {
        return await Filter("price", true);
    }

    [HttpGet("by-engine-capacity")]
    public async Task<IActionResult> GetByEngineCapacity()
    {
        return await Filter("engine", true);
    }

    [HttpGet("by-brand-asc")]
    public async Task<IActionResult> GetByBrandAsc()
    {
        return await Filter("brand", true);
    }

    [HttpGet("by-brand-desc")]
    public async Task<IActionResult> GetByBrandDesc()
    {
        return await Filter("brand", false);
    }

    [HttpGet("by-seats")]
    public async Task<IActionResult> GetBySeats()
    {
        return await Filter("seats", true);
    }
}