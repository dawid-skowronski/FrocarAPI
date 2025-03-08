using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FrogCar.Data;
using FrogCar.Models;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class MapPointsController : ControllerBase
{
    private readonly AppDbContext _context;

    public MapPointsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> AddMapPoint([FromBody] MapPoint mapPoint)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        mapPoint.UserId = userId; // Pobranie ID z JWT

        _context.MapPoints.Add(mapPoint);
        await _context.SaveChangesAsync();

        return Ok(mapPoint);
    }

    [HttpGet]
    public async Task<IActionResult> GetUserMapPoints()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var points = await _context.MapPoints.Where(p => p.UserId == userId).ToListAsync();

        return Ok(points);
    }
}
