using FrogCar.Controllers;
using FrogCar.Data;
using FrogCar.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FrogCar.Tests.Controllers
{
    public class FilterControllerTests
    {
        private readonly AppDbContext _context;
        private readonly FilterController _controller;

        public FilterControllerTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) 
                .Options;
            _context = new AppDbContext(options);
            _controller = new FilterController(_context);
        }

        private async Task SetupCarListings()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 40m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 3,
                    Brand = "BMW",
                    EngineCapacity = 3.0,
                    FuelType = "Diesel",
                    CarType = "SUV",
                    Seats = 7,
                    RentalPricePerDay = 100m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 4,
                    Brand = "Audi",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 80m,
                    IsApproved = false, 
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();
        }

        [Fact]
        public async Task GetByPriceDesc_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetByPriceDesc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count); 
            Assert.Equal(100m, cars[0].RentalPricePerDay); 
            Assert.Equal(50m, cars[1].RentalPricePerDay); 
            Assert.Equal(40m, cars[2].RentalPricePerDay); 
        }

        [Fact]
        public async Task GetByPriceDesc_NoApprovedAvailableCars_ReturnsEmptyList()
        {

            var result = await _controller.GetByPriceDesc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Empty(cars);
        }

        [Fact]
        public async Task GetByPriceAsc_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetByPriceAsc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count);
            Assert.Equal(40m, cars[0].RentalPricePerDay);
            Assert.Equal(50m, cars[1].RentalPricePerDay); 
            Assert.Equal(100m, cars[2].RentalPricePerDay);
        }

        [Fact]
        public async Task GetByEngineCapacity_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetByEngineCapacity();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count);
            Assert.Equal(1.8, cars[0].EngineCapacity);
            Assert.Equal(2.0, cars[1].EngineCapacity);
            Assert.Equal(3.0, cars[2].EngineCapacity);
        }

        [Fact]
        public async Task GetByBrandAsc_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetByBrandAsc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count);
            Assert.Equal("BMW", cars[0].Brand);
            Assert.Equal("Honda", cars[1].Brand);
            Assert.Equal("Toyota", cars[2].Brand);
        }

        [Fact]
        public async Task GetByBrandDesc_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetByBrandDesc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count);
            Assert.Equal("Toyota", cars[0].Brand);
            Assert.Equal("Honda", cars[1].Brand);
            Assert.Equal("BMW", cars[2].Brand);
        }

        [Fact]
        public async Task GetBySeats_HasApprovedAvailableCars_ReturnsOrderedList()
        {
            await SetupCarListings();

            var result = await _controller.GetBySeats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, cars.Count);
            Assert.Equal(4, cars[0].Seats);
            Assert.Equal(5, cars[1].Seats);
            Assert.Equal(7, cars[2].Seats);
        }

        [Fact]
        public async Task GetBySeats_NoApprovedAvailableCars_ReturnsEmptyList()
        {
            var car = new CarListing
            {
                Id = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Petrol",
                CarType = "Sedan",
                Seats = 5,
                RentalPricePerDay = 50m,
                IsApproved = false,
                IsAvailable = true
            };
            _context.CarListing.Add(car);
            await _context.SaveChangesAsync();

            var result = await _controller.GetBySeats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var cars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Empty(cars);
        }
        [Fact]
        public async Task GetByPriceDesc_DuplicatePrices_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 3,
                    Brand = "BMW",
                    EngineCapacity = 3.0,
                    FuelType = "Diesel",
                    CarType = "SUV",
                    Seats = 7,
                    RentalPricePerDay = 100m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByPriceDesc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(3, returnedCars.Count);
            Assert.Equal(100m, returnedCars[0].RentalPricePerDay);
            Assert.Equal(50m, returnedCars[1].RentalPricePerDay);
            Assert.Equal(50m, returnedCars[2].RentalPricePerDay);
        }

        [Fact]
        public async Task GetByEngineCapacity_SameCapacity_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Audi",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 80m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByEngineCapacity();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal(2.0, returnedCars[0].EngineCapacity);
            Assert.Equal(2.0, returnedCars[1].EngineCapacity);
        }

        [Fact]
        public async Task GetBySeats_SomeUnavailableCars_ReturnsOnlyAvailable()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 40m,
                    IsApproved = true,
                    IsAvailable = false 
                },
                new CarListing
                {
                    Id = 3,
                    Brand = "BMW",
                    EngineCapacity = 3.0,
                    FuelType = "Diesel",
                    CarType = "SUV",
                    Seats = 7,
                    RentalPricePerDay = 100m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetBySeats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal(5, returnedCars[0].Seats);
            Assert.Equal(7, returnedCars[1].Seats);
        }

        [Fact]
        public async Task GetByPriceAsc_ZeroPrice_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 0m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 40m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByPriceAsc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal(0m, returnedCars[0].RentalPricePerDay);
            Assert.Equal(40m, returnedCars[1].RentalPricePerDay);
        }

        [Fact]
        public async Task GetByBrandAsc_EmptyBrand_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 40m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByBrandAsc();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal("", returnedCars[0].Brand);
            Assert.Equal("Honda", returnedCars[1].Brand);
        }

        [Fact]
        public async Task GetByEngineCapacity_MinimalCapacity_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 0.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Honda",
                    EngineCapacity = 1.8,
                    FuelType = "Petrol",
                    CarType = "Hatchback",
                    Seats = 4,
                    RentalPricePerDay = 40m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetByEngineCapacity();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal(0.0, returnedCars[0].EngineCapacity);
            Assert.Equal(1.8, returnedCars[1].EngineCapacity);
        }

        [Fact]
        public async Task GetBySeats_DuplicateSeats_ReturnsOrderedList()
        {
            var cars = new List<CarListing>
            {
                new CarListing
                {
                    Id = 1,
                    Brand = "Toyota",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5,
                    RentalPricePerDay = 50m,
                    IsApproved = true,
                    IsAvailable = true
                },
                new CarListing
                {
                    Id = 2,
                    Brand = "Audi",
                    EngineCapacity = 2.0,
                    FuelType = "Petrol",
                    CarType = "Sedan",
                    Seats = 5, 
                    RentalPricePerDay = 80m,
                    IsApproved = true,
                    IsAvailable = true
                }
            };
            _context.CarListing.AddRange(cars);
            await _context.SaveChangesAsync();

            var result = await _controller.GetBySeats();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCars = Assert.IsAssignableFrom<List<CarListing>>(okResult.Value);
            Assert.Equal(2, returnedCars.Count);
            Assert.Equal(5, returnedCars[0].Seats);
            Assert.Equal(5, returnedCars[1].Seats);
        }
    }
}