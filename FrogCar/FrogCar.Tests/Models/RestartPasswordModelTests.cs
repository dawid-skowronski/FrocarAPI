using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace FrogCar.Tests.Models
{
    public class ResetPasswordModelTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void ResetPasswordModel_MissingFields_FailsValidation()
        {
            var model = new ResetPasswordModel
            {
                Token = null,
                NewPassword = null
            };

            var results = ValidateModel(model);

            results.Should().NotBeEmpty();
        }

        [Fact]
        public void ResetPasswordModel_ValidFields_PassesValidation()
        {
            var model = new ResetPasswordModel
            {
                Token = "abc123",
                NewPassword = "noweHaslo123"
            };

            var results = ValidateModel(model);

            results.Should().BeEmpty();
        }
    }
}
