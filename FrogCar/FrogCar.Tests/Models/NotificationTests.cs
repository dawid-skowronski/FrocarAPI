using FrogCar.Models;
using Xunit;
using FluentAssertions;
using System;

namespace FrogCar.Tests.Models
{
    public class NotificationTests
    {
        [Fact]
        public void Notification_DefaultValues_AreSetCorrectly()
        {
            var notification = new Notification
            {
                UserId = 1,
                Message = "Powiadomienie testowe"
            };

            notification.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            notification.IsRead.Should().BeFalse();
        }
    }
}
