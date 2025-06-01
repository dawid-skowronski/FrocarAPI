using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace FrogCar.Tests.Models
{
    public class LoginModelTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void LoginModel_MissingFields_FailsValidation()
        {
            var model = new LoginModel { Username = "", Password = "" };

            var results = ValidateModel(model);

            results.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void LoginModel_ValidData_PassesValidation()
        {
            var model = new LoginModel { Username = "login123", Password = "haslo123" };

            var results = ValidateModel(model);

            results.Should().BeEmpty();
        }
    }
}
