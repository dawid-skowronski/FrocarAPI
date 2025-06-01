using FrogCar.Models;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using Xunit;
using FluentAssertions;

namespace FrogCar.Tests.Models
{
    public class ChangeUsernameModelTests
    {
        private IList<ValidationResult> ValidateModel(object model)
        {
            var results = new List<ValidationResult>();
            var context = new ValidationContext(model);
            Validator.TryValidateObject(model, context, results, true);
            return results;
        }

        [Fact]
        public void ChangeUsernameModel_EmptyUsername_FailsValidation()
        {
            var model = new ChangeUsernameModel { NewUsername = " " };

            var results = ValidateModel(model);

            results.Should().NotBeEmpty();
        }

        [Fact]
        public void ChangeUsernameModel_ValidUsername_PassesValidation()
        {
            var model = new ChangeUsernameModel { NewUsername = "NowyNick" };

            var results = ValidateModel(model);

            results.Should().BeEmpty();
        }
    }
}
