using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FrogCar.Tests.Models
{
    public class CarRentalRequestTests
    {
        private CarRentalRequest CreateValidCarRentalRequest()
        {
            return new CarRentalRequest
            {
                CarListingId = 1,
                RentalStartDate = DateTime.Now,
                RentalEndDate = DateTime.Now.AddDays(1)
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
        public void CarRentalRequest_ValidModel_PassesValidation()
        {
            var request = CreateValidCarRentalRequest();
            var validationResults = ValidateModel(request);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CarRentalRequest_MissingRequiredFields_FailsValidation()
        {
            var request = new CarRentalRequest();
            var validationResults = ValidateModel(request);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CarRentalRequest_EndDateBeforeStartDate_FailsBusinessRule()
        {
            var request = CreateValidCarRentalRequest();
            request.RentalEndDate = request.RentalStartDate.AddDays(1); 
            request.RentalEndDate.Should().BeAfter(request.RentalStartDate, "RentalEndDate must be after RentalStartDate");
        }

        [Fact]
        public void CarRentalRequest_DefaultValues_AreSetCorrectly()
        {
            var request = new CarRentalRequest();
            request.CarListingId.Should().Be(0);
            request.RentalStartDate.Should().Be(default(DateTime));
            request.RentalEndDate.Should().Be(default(DateTime));
        }
    }
}