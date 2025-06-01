using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;

namespace FrogCar.Tests.Models
{
    public class CarRentalReviewTests
    {
        private CarRentalReview CreateValidCarRentalReview()
        {
            return new CarRentalReview
            {
                ReviewId = 1,
                CarRentalId = 1,
                CarRental = new CarRental { CarRentalId = 1 },
                UserId = 1,
                User = new User { Id = 1, Email = "user@example.com" },
                Rating = 4,
                Comment = "Great car!",
                CreatedAt = DateTime.UtcNow
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
        public void CarRentalReview_ValidModel_PassesValidation()
        {
            var review = CreateValidCarRentalReview();
            var validationResults = ValidateModel(review);
            Assert.Empty(validationResults);
        }

        [Fact]
        public void CarRentalReview_MissingRequiredFields_FailsValidation()
        {
            var review = new CarRentalReview();
            var validationResults = ValidateModel(review);
            validationResults.Should().BeEmpty();
        }

        [Fact]
        public void CarRentalReview_DefaultValues_AreSetCorrectly()
        {
            var review = new CarRentalReview();
            review.ReviewId.Should().Be(0);
            review.CarRentalId.Should().Be(0);
            review.UserId.Should().Be(0);
            review.Rating.Should().Be(0);
            review.Comment.Should().BeNull();
            review.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            review.CarRental.Should().BeNull();
            review.User.Should().BeNull();
        }
    }
}