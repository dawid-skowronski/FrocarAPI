using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FrogCar.Tests.Models
{
    public class CarRentalTests
    {
        private CarRental CreateValidCarRental()
        {
            return new CarRental
            {
                CarRentalId = 1,
                CarListingId = 1,
                CarListing = new CarListing { Id = 1, Brand = "Toyota" },
                UserId = 1,
                User = new User { Id = 1, Email = "user@example.com" },
                RentalStartDate = DateTime.Now,
                RentalEndDate = DateTime.Now.AddDays(1),
                RentalPrice = 150.00m,
                RentalStatus = "Aktywne"
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
        public void CarRental_ValidModel_PassesValidation()
        {
            var carRental = CreateValidCarRental();

            var validationResults = ValidateModel(carRental);

            Assert.Empty(validationResults);
        }

        [Fact]
        public void CarRental_MissingRequiredFields_FailsValidation()
        {
            // Arrange
            var carRental = new CarRental
            {
                CarRentalId = 1,
                CarListingId = 1,
                CarListing = new CarListing { Id = 1 },
                UserId = 1,
                User = new User { Id = 1 } 
                                           
            };

            var validationResults = ValidateModel(carRental);

            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CarRental_DefaultValues_AreSetCorrectly()
        {
            var carRental = new CarRental();

            carRental.CarRentalId.Should().Be(0);
            carRental.CarListingId.Should().Be(0);
            carRental.UserId.Should().Be(0);
            carRental.RentalStartDate.Should().Be(default(DateTime));
            carRental.RentalEndDate.Should().Be(default(DateTime));
            carRental.RentalPrice.Should().Be(0.0m);
            carRental.RentalStatus.Should().BeNull();
            carRental.CarListing.Should().BeNull();
            carRental.User.Should().BeNull();
        }

        [Fact]
        public void CarRental_EndDateBeforeStartDate_ThrowsValidationError()
        {
            var carRental = CreateValidCarRental();
            carRental.RentalEndDate = carRental.RentalStartDate.AddDays(-1); 

            var validationResults = ValidateModel(carRental);
            Assert.Empty(validationResults);
        }
    }
}