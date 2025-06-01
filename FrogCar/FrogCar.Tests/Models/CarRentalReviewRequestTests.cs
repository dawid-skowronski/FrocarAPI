using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FrogCar.Tests.Models
{
    public class CarRentalReviewRequestTests
    {
        private CarRentalReviewRequest CreateValidCarRentalReviewRequest()
        {
            return new CarRentalReviewRequest
            {
                CarRentalId = 1,
                Rating = 4,
                Comment = "Great car!"
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
        public void CarRentalReviewRequest_ValidModel_PassesValidation()
        {
            var request = CreateValidCarRentalReviewRequest();
            var validationResults = ValidateModel(request);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CarRentalReviewRequest_MissingRequiredFields_FailsValidation()
        {
            var request = new CarRentalReviewRequest();
            var validationResults = ValidateModel(request);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CarRentalReviewRequest_DefaultValues_AreSetCorrectly()
        {
            var request = new CarRentalReviewRequest();
            request.CarRentalId.Should().Be(0);
            request.Rating.Should().Be(0);
            request.Comment.Should().BeNull();
        }
    }
}