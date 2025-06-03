using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Configuration;
using System;

public class EmailService
{
    private readonly IConfiguration _configuration;

    public EmailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string body)
    {
        try
        {

            var testMode = _configuration["Email:TestMode"];
            if (testMode == "true")
            {
                return;
            }

            var emailConfig = _configuration.GetSection("Email");

            var smtpClient = new SmtpClient(emailConfig["SmtpServer"], int.Parse(emailConfig["SmtpPort"]))
            {
                Credentials = new NetworkCredential(emailConfig["SenderEmail"], emailConfig["Password"]),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(emailConfig["SenderEmail"], emailConfig["SenderName"]),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage);
        }
        catch (SmtpException smtpEx)
        {
            Console.WriteLine($"Błąd SMTP: {smtpEx.Message}");
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd ogólny podczas wysyłki maila: {ex.Message}");
            throw;
        }
    }
}
