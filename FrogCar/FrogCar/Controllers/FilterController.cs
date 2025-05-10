using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FilterController : ControllerBase
{
    private readonly AppDbContext _context;

    public FilterController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("by-price-desc")]
    public async Task<IActionResult> GetByPriceDesc()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderByDescending(c => c.RentalPricePerDay)
            .ToListAsync();

        return Ok(cars);
    }

    [HttpGet("by-price-asc")]
    public async Task<IActionResult> GetByPriceAsc()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderBy(c => c.RentalPricePerDay)
            .ToListAsync();

        return Ok(cars);
    }

    [HttpGet("by-engine-capacity")]
    public async Task<IActionResult> GetByEngineCapacity()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderBy(c => c.EngineCapacity)
            .ToListAsync();

        return Ok(cars);
    }

    [HttpGet("by-brand-asc")]
    public async Task<IActionResult> GetByBrandAsc()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderBy(c => c.Brand)
            .ToListAsync();

        return Ok(cars);
    }

    [HttpGet("by-brand-desc")]
    public async Task<IActionResult> GetByBrandDesc()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderByDescending(c => c.Brand)
            .ToListAsync();

        return Ok(cars);
    }

    [HttpGet("by-seats")]
    public async Task<IActionResult> GetBySeats()
    {
        var cars = await _context.CarListing
            .Where(c => c.IsApproved && c.IsAvailable)
            .OrderBy(c => c.Seats)
            .ToListAsync();

        return Ok(cars);
    }
}
