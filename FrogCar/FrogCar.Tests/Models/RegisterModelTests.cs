using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace FrogCar.Tests.Models
{
    public class RegisterModelTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void RegisterModel_InvalidFields_FailsValidation()
        {
            var model = new RegisterModel
            {
                Username = "",
                Email = "niepoprawnyEmail",
                Password = "123",
                ConfirmPassword = "456"
            };

            var results = ValidateModel(model);

            results.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void RegisterModel_ValidFields_PassesValidation()
        {
            var model = new RegisterModel
            {
                Username = "uzytkownik",
                Email = "email@example.com",
                Password = "haslo123",
                ConfirmPassword = "haslo123"
            };

            var results = ValidateModel(model);

            results.Should().BeEmpty();
        }
    }
}
