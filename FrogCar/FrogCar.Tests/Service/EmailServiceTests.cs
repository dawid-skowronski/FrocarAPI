using Moq;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.Threading.Tasks;
using System.Net.Mail;

namespace FrogCar.Tests.Service;
public class EmailServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly EmailService _emailService;

    public EmailServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        var emailSectionMock = new Mock<IConfigurationSection>();
        emailSectionMock.Setup(s => s["SmtpServer"]).Returns("smtp.example.com");
        emailSectionMock.Setup(s => s["SmtpPort"]).Returns("587");
        emailSectionMock.Setup(s => s["SenderEmail"]).Returns("test@example.com");
        emailSectionMock.Setup(s => s["SenderName"]).Returns("Test Sender");
        emailSectionMock.Setup(s => s["Password"]).Returns("password");
        _configurationMock.Setup(c => c.GetSection("Email")).Returns(emailSectionMock.Object);
        _configurationMock.Setup(c => c["Email:TestMode"]).Returns("true");

        _emailService = new EmailService(_configurationMock.Object);
    }

    [Fact]
    public async Task SendEmailAsync_TestModeEnabled_DoesNotSendEmail()
    {

        var toEmail = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";


        await _emailService.SendEmailAsync(toEmail, subject, body);

    }

    [Fact]
    public async Task SendEmailAsync_TestModeDisabled_ThrowsSmtpException()
    {

        _configurationMock.Setup(c => c["Email:TestMode"]).Returns("false");
        var toEmail = "recipient@example.com";
        var subject = "Test Subject";
        var body = "Test Body";

        await Assert.ThrowsAsync<SmtpException>(() => _emailService.SendEmailAsync(toEmail, subject, body));
    }
}