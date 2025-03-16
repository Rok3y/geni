using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using LaunchService.Model;

namespace LaunchService.Services
{
    public interface IMailservice 
    {
        Task<bool> SendMailToRecipients(List<Launch> launches);
        Task<bool> SendMailToRecipients(Dictionary<string, List<Launch>> modifiedLaunchWeek);
    }

    public class MailService : IMailservice
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _sender;
        private readonly ILogger<MailService> _logger;
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration, ILoggerFactory loggerFactory)
        {

            _configuration = configuration;
            _smtpServer = string.Empty;
            _smtpPort = 0;
            _smtpUser = string.Empty;
            _smtpPassword = string.Empty;
            _sender = string.Empty;
            _logger = loggerFactory.CreateLogger<MailService>();
        }

        public async Task<bool> SendMailToRecipients(List<Launch> launches)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    smtpClient.EnableSsl = true; // Ensure SSL/TLS is enabled

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_sender),
                        Subject = "🚀 Upcoming Rocket Launch",
                        Body = GenerateHtmlBody(launches),
                        IsBodyHtml = true
                    };

                    // Add recipients
                    foreach (var recipient in _configuration.Recipients)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email successfully sent!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMailToRecipients(Dictionary<string, List<Launch>> modifiedLaunchWeek)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    smtpClient.EnableSsl = true; // Ensure SSL/TLS is enabled

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_sender),
                        Subject = "🚀 Upcoming Rocket Launch",
                        Body = "",
                        IsBodyHtml = true
                    };

                    // Add recipients
                    foreach (var recipient in _configuration.Recipients)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email successfully sent!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                return false;
            }
        }

        private string GenerateHtmlBody(List<Launch> launches)
        {
            var listBuilder = new StringBuilder();
            foreach (var launch in launches)
            {
                listBuilder.AppendLine($"<li><b>Rocket:</b> {launch.RocketName}</li>");
                listBuilder.AppendLine($"<li><b>Launch Date:</b> {launch.T0:MMMM d, yyyy}</li>");
            }

            var htmlBody = $@"
    <html>
        <body style='font-family:Arial, sans-serif;'>
            <h2 style='color:#007BFF;'>🚀 Launch Update</h2>
            <p>Hello,</p>
            <p>A new rocket launch is scheduled!</p>
            <ul>
                {listBuilder}
            </ul>
        </body>
    </html>";

            return htmlBody;
        }
    }
}
