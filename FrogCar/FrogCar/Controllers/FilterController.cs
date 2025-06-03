using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]

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

    [HttpGet("filter")]
    public async Task<IActionResult> Filter(string sortBy, bool ascending = true)
    {
        if (string.IsNullOrEmpty(sortBy))
            return BadRequest("Parametr sortBy jest wymagany.");

        var query = _context.CarListing.Where(c => c.IsApproved && c.IsAvailable);
        query = sortBy.ToLower() switch
        {
            "price" => ascending ? query.OrderBy(c => c.RentalPricePerDay) : query.OrderByDescending(c => c.RentalPricePerDay),
            "brand" => ascending ? query.OrderBy(c => c.Brand) : query.OrderByDescending(c => c.Brand),
            "engine" => ascending ? query.OrderBy(c => c.EngineCapacity) : query.OrderByDescending(c => c.EngineCapacity),
            "seats" => ascending ? query.OrderBy(c => c.Seats) : query.OrderByDescending(c => c.Seats),
            _ => query.OrderBy(c => c.Id) 
        };
        return Ok(await query.ToListAsync());
    }

}
