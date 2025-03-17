using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;
using LaunchService.Model;
using System.Configuration;
using Microsoft.Extensions.Configuration;

namespace LaunchService.Services
{
    public interface IMailservice 
    {
        Task<bool> SendMailToRecipients(Week week);
        Task<bool> SendMailToRecipients(Dictionary<string, List<Launch>> modifiedLaunchWeek, Week week);
    }

    public class MailService : IMailservice
    {
        public string ConfigurationMailFile { get; } = "emails.json";
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _senderEmail;
        private readonly ILogger<MailService> _logger;
        public List<string> Recipients { get; set; } = new List<string>();

        public MailService(ILoggerFactory loggerFactory)
        {
            _smtpServer = "smtp.gmail.com";
            _smtpPort = 587;
            _smtpUser = Environment.GetEnvironmentVariable("test_email");
            _smtpPassword = Environment.GetEnvironmentVariable("test_email_password");
            _senderEmail = Environment.GetEnvironmentVariable("test_email");
            _logger = loggerFactory.CreateLogger<MailService>();
            if (!File.Exists(ConfigurationMailFile))
                throw new FileNotFoundException($"Configuration mail file '{ConfigurationMailFile}' not found.");

            var mailConfig = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(ConfigurationMailFile, optional: false, reloadOnChange: true)
                .Build();

            Recipients = mailConfig.GetSection("emails").Get<List<string>>();
            if (Recipients == null || Recipients.Count == 0)
            {
                _logger.LogWarning("No recipients defined in the configuration file!");
            }
        }

        public async Task<bool> SendMailToRecipients(Week week)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                {
                    smtpClient.UseDefaultCredentials = false;
                    smtpClient.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    smtpClient.EnableSsl = true; // Ensure SSL/TLS is enabled
                    smtpClient.Port = _smtpPort;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_senderEmail),
                        Subject = "🚀 Upcoming Rocket Launch",
                        Body = GenerateHtmlBody(week),
                        IsBodyHtml = true
                    };

                    // Add recipients
                    foreach (var recipient in Recipients)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                    _logger.LogInformation($"Email successfully sent!");

                    _logger.LogInformation($"Email should be successfully sent!");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendMailToRecipients(Dictionary<string, List<Launch>> modifiedLaunchWeek, Week week)
        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpServer, _smtpPort))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);
                    smtpClient.EnableSsl = true; // Ensure SSL/TLS is enabled

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_senderEmail),
                        Subject = $"🚀 Updates for week {week.WeekNumber}: {week.WeekStart} - {week.WeekEnd}",
                        Body = GenerateHtmlBody_NewWeekLaunches(modifiedLaunchWeek, week),
                        IsBodyHtml = true
                    };

                    // Add recipients
                    foreach (var recipient in Recipients)
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

        private static string GenerateHtmlBody_NewWeekLaunches(Dictionary<string, List<Launch>> modifiedLaunchWeek, Week week)
        {
            var newLaunchesBuilder = new StringBuilder();
            foreach (var launch in modifiedLaunchWeek["added"])
            {
                newLaunchesBuilder.AppendLine($"<li><b>Rocket:</b> {launch.RocketName}</br>");
                newLaunchesBuilder.AppendLine($"<b>&emsp;Launch Date:</b> {launch.T0:MMMM d, yyyy}</br>");
                newLaunchesBuilder.AppendLine($"<b>&emsp;Status:</b> {GetFullStatusText(launch.Status)}</br></br></li>");
            }

            var modifiedLaunchesBuilder = new StringBuilder();
            foreach (var launch in modifiedLaunchWeek["modified"])
            {
                modifiedLaunchesBuilder.AppendLine($"<li><b>Rocket:</b> {launch.RocketName}</br>");
                modifiedLaunchesBuilder.AppendLine($"<b>&emsp;Launch Date:</b> {launch.T0:MMMM d, yyyy}</br>");
                modifiedLaunchesBuilder.AppendLine($"<b>&emsp;Status:</b> {GetFullStatusText(launch.Status)}</br></br></li>");
            }

            var htmlBody = $@"
    <html>
        <body style='font-family:Arial, sans-serif;'>
            <h2 style='color:#007BFF;'>🚀 Launch Update</h2>
            <p>Hello,</p>
            <p>A new rocket launch is scheduled for the weekend {week.WeekNumber}: {week.WeekStart} - {week.WeekEnd}:</p>
            <ul>
                {newLaunchesBuilder}
            </ul>
            <br/>
            <p>Launch updates for the week {week.WeekNumber}: {week.WeekStart} - {week.WeekEnd}</p>
            <ul>
                {modifiedLaunchesBuilder}
            </ul>
        </body>
    </html>";

            return htmlBody;
        }

        private static string GenerateHtmlBody(Week week)
        {
            var listBuilder = new StringBuilder();
            foreach (var launch in week.Launches)
            {
                listBuilder.AppendLine($"<li><b>Rocket:</b> {launch.RocketName}</br>");
                listBuilder.AppendLine($"<b>&emsp;Launch Date:</b> {launch.T0:MMMM d, yyyy}</br>");
                listBuilder.AppendLine($"<b>&emsp;Status:</b> {GetFullStatusText(launch.Status)}</br></br></li>");
            }

            var htmlBody = $@"
    <html>
        <body style='font-family:Arial, sans-serif;'>
            <h2 style='color:#007BFF;'>🚀 Launch Update</h2>
            <p>Hello,</p>
            <p>A new rocket launch is scheduled for the weekend {week.WeekNumber}: {week.WeekStart} - {week.WeekEnd}!</p>
            <ul>
                {listBuilder}
            </ul>
        </body>
    </html>";

            return htmlBody;
        }

        private static string GetFullStatusText(LaunchStatus launchStatus)
        {
            string status = "Unkown status";
            switch (launchStatus)
            {
                case LaunchStatus.LaunchInFlight:
                    status = "Launch in Flight";
                    break;
                case LaunchStatus.PayloadDeployed:
                    status = "Payload Deployed";
                    break;
                case LaunchStatus.ToBeConfirmed:
                    status = "To Be Confirmed";
                    break;
                case LaunchStatus.GoForLaunch:
                    status = "Go for Launch";
                    break;
                case LaunchStatus.LaunchSuccessful:
                    status = "Launch Successful";
                    break;
                case LaunchStatus.LaunchFailure:
                    status = "Launch Failure";
                    break;
                case LaunchStatus.OnHold:
                    status = "On Hold";
                    break;
                case LaunchStatus.PartialFailure:
                    status = "Launch was a Partial Failure";
                    break;
                case LaunchStatus.ToBeDetermined:
                    status = "To Be Determined";
                    break;
                default:
                    break;
            }

            return status;
        }
    }
}
