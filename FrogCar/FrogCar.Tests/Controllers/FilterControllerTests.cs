using Xunit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FrogCar.Data;
using FrogCar.Models;

namespace FrogCar.Tests.Controllers;
public class FilterControllerTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly FilterController _controller;

    public FilterControllerTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        SeedDatabase();

        _controller = new FilterController(_context);
    }

    private void SeedDatabase()
    {
        _context.CarListing.RemoveRange(_context.CarListing);
        _context.SaveChanges();

        _context.CarListing.Add(new CarListing { Id = 1, Brand = "Toyota", EngineCapacity = 2.0, RentalPricePerDay = 50, Seats = 5, IsAvailable = true, IsApproved = true });
        _context.CarListing.Add(new CarListing { Id = 2, Brand = "Honda", EngineCapacity = 1.8, RentalPricePerDay = 40, Seats = 4, IsAvailable = true, IsApproved = true });
        _context.CarListing.Add(new CarListing { Id = 3, Brand = "Ford", EngineCapacity = 2.5, RentalPricePerDay = 60, Seats = 5, IsAvailable = true, IsApproved = true });
        _context.CarListing.Add(new CarListing { Id = 4, Brand = "BMW", EngineCapacity = 3.0, RentalPricePerDay = 70, Seats = 2, IsAvailable = true, IsApproved = true });
        _context.CarListing.Add(new CarListing { Id = 5, Brand = "Audi", EngineCapacity = 2.2, RentalPricePerDay = 55, Seats = 5, IsAvailable = true, IsApproved = true });

        _context.CarListing.Add(new CarListing { Id = 6, Brand = "Nissan", EngineCapacity = 1.6, RentalPricePerDay = 35, Seats = 5, IsAvailable = false, IsApproved = true });

        _context.CarListing.Add(new CarListing { Id = 7, Brand = "Mercedes", EngineCapacity = 3.5, RentalPricePerDay = 80, Seats = 7, IsAvailable = true, IsApproved = false });

        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task GetByPriceDesc_ReturnsCarsSortedByPriceDescending()
    {
        var result = await _controller.GetByPriceDesc();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("BMW", cars[0].Brand);
        Assert.Equal("Ford", cars[1].Brand);
        Assert.Equal("Audi", cars[2].Brand);
        Assert.Equal("Toyota", cars[3].Brand);
        Assert.Equal("Honda", cars[4].Brand);
    }

    [Fact]
    public async Task GetByPriceAsc_ReturnsCarsSortedByPriceAscending()
    {
        var result = await _controller.GetByPriceAsc();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Honda", cars[0].Brand);
        Assert.Equal("Toyota", cars[1].Brand);
        Assert.Equal("Audi", cars[2].Brand);
        Assert.Equal("Ford", cars[3].Brand);
        Assert.Equal("BMW", cars[4].Brand);
    }

    [Fact]
    public async Task GetByEngineCapacity_ReturnsCarsSortedByEngineCapacity()
    {
        var result = await _controller.GetByEngineCapacity();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Honda", cars[0].Brand);
        Assert.Equal("Toyota", cars[1].Brand);
        Assert.Equal("Audi", cars[2].Brand);
        Assert.Equal("Ford", cars[3].Brand);
        Assert.Equal("BMW", cars[4].Brand);
    }

    [Fact]
    public async Task GetByBrandAsc_ReturnsCarsSortedByBrandAscending()
    {
        var result = await _controller.GetByBrandAsc();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Audi", cars[0].Brand);
        Assert.Equal("BMW", cars[1].Brand);
    }

    [Fact]
    public async Task GetByBrandDesc_ReturnsCarsSortedByBrandDescending()
    {
        var result = await _controller.GetByBrandDesc();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Toyota", cars[0].Brand);
        Assert.Equal("Honda", cars[1].Brand);
    }

    [Fact]
    public async Task GetBySeats_ReturnsCarsSortedBySeats()
    {
        var result = await _controller.GetBySeats();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal("BMW", cars[0].Brand);
        Assert.Equal("Honda", cars[1].Brand);

        Assert.True(cars.Any(c => c.Id == 1 && c.Seats == 5));
        Assert.True(cars.Any(c => c.Id == 3 && c.Seats == 5));
        Assert.True(cars.Any(c => c.Id == 5 && c.Seats == 5));
    }

    [Fact]
    public async Task Filter_SortByPriceAsc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("price", true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Honda", cars[0].Brand);
        Assert.Equal("Toyota", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortByPriceDesc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("price", false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("BMW", cars[0].Brand);
        Assert.Equal("Ford", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortByBrandAsc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("brand", true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Audi", cars[0].Brand);
        Assert.Equal("BMW", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortByBrandDesc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("brand", false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Toyota", cars[0].Brand);
        Assert.Equal("Honda", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortByEngineAsc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("engine", true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("Honda", cars[0].Brand);
        Assert.Equal("Toyota", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortByEngineDesc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("engine", false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("BMW", cars[0].Brand);
        Assert.Equal("Ford", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortBySeatsAsc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("seats", true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.Equal(5, cars.Count);
        Assert.Equal("BMW", cars[0].Brand);
        Assert.Equal("Honda", cars[1].Brand);
    }

    [Fact]
    public async Task Filter_SortBySeatsDesc_ReturnsCorrectlySortedCars()
    {
        var result = await _controller.Filter("seats", false);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        Assert.True(cars.Any(c => c.Id == 1 && c.Seats == 5));
        Assert.True(cars.Any(c => c.Id == 3 && c.Seats == 5));
        Assert.True(cars.Any(c => c.Id == 5 && c.Seats == 5)); 

        Assert.Equal("Honda", cars[3].Brand);
        Assert.Equal("BMW", cars[4].Brand);
    }

    [Fact]
    public async Task Filter_InvalidSortByParameter_ReturnsDefaultSortedCarsById()
    {
        var result = await _controller.Filter("invalid_sort_key", true);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);

        var nonApprovedOrUnavailableCars = cars.Where(c => !c.IsApproved || !c.IsAvailable).ToList();
        Assert.Empty(nonApprovedOrUnavailableCars);

        Assert.Equal(5, cars.Count);
        Assert.Equal(1, cars[0].Id);
        Assert.Equal(2, cars[1].Id);
        Assert.Equal(3, cars[2].Id);
        Assert.Equal(4, cars[3].Id);
        Assert.Equal(5, cars[4].Id);
    }

    [Fact]
    public async Task Filter_EmptySortByParameter_ReturnsBadRequest()
    {
        var result = await _controller.Filter("", true);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Parametr sortBy jest wymagany.", badRequestResult.Value);
    }

    [Fact]
    public async Task Filter_NullSortByParameter_ReturnsBadRequest()
    {
        var result = await _controller.Filter(null, true);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Parametr sortBy jest wymagany.", badRequestResult.Value);
    }
}