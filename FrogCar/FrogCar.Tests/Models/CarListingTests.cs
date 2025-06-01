using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FrogCar.Tests.Models
{
    public class CarListingTests
    {
        private CarListing CreateValidCarListing()
        {
            return new CarListing
            {
                Id = 1,
                Brand = "Toyota",
                EngineCapacity = 2.0,
                FuelType = "Benzyna",
                Seats = 5,
                CarType = "Sedan",
                Features = new List<string> { "Klimatyzacja", "GPS" },
                Latitude = 52.2297,
                Longitude = 21.0122,
                UserId = 1,
                IsAvailable = true,
                IsApproved = false,
                RentalPricePerDay = 150.00m,
                AverageRating = 4.5
            };
        }

        private IList<ValidationResult> ValidateModel(object model)
        {
            var validationResults = new List<ValidationResult>();
            var context = new ValidationContext(model, null, null);
            Validator.TryValidateObject(model, context, validationResults, true);
            return validationResults;
        }

        [Fact]
        public void CarListing_ValidModel_PassesValidation()
        {
            var carListing = CreateValidCarListing();

            var validationResults = ValidateModel(carListing);

            Assert.Empty(validationResults);
        }

        [Fact]
        public void CarListing_MissingRequiredFields_FailsValidation()
        {
            var carListing = new CarListing
            {
                Id = 1,
                UserId = 1,
                Latitude = 52.2297,
                Longitude = 21.0122,
                EngineCapacity = 0.0,
                Seats = 0,
                RentalPricePerDay = 0.0m
            };

            var validationResults = ValidateModel(carListing);

            validationResults.Should().HaveCount(3); 
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("Brand"));
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("FuelType"));
            validationResults.Should().Contain(v => v.ErrorMessage.Contains("CarType"));
        }

        [Fact]
        public void CarListing_DefaultValues_AreSetCorrectly()
        {
            var carListing = new CarListing();

            carListing.IsAvailable.Should().BeTrue();
            carListing.IsApproved.Should().BeFalse();
            carListing.AverageRating.Should().Be(0.0);
            carListing.Features.Should().NotBeNull();
            carListing.Features.Should().BeEmpty();
            carListing.Brand.Should().BeEmpty();
            carListing.FuelType.Should().BeEmpty();
            carListing.CarType.Should().BeEmpty();
        }

        [Fact]
        public void CarListing_JsonSerialization_IgnoresUserProperty()
        {
            var carListing = CreateValidCarListing();
            carListing.User = new User { Id = 1, Email = "jan.kowalski@example.com" }; 

            var json = JsonSerializer.Serialize(carListing);
            var deserialized = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            deserialized.Should().NotContainKey("User");
        }
    }
}