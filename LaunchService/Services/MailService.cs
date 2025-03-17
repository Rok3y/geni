using Microsoft.Extensions.Logging;
using System.Net.Mail;
using System.Net;
using System.Text;
using LaunchService.Model;

namespace LaunchService.Services
{
    public interface IMailservice 
    {
        Task<bool> SendMailToRecipients(Week week);
        Task<bool> SendMailToRecipients(Dictionary<string, List<Launch>> modifiedLaunchWeek, Week week);
    }

    public class MailService : IMailservice
    {
        private readonly string _smtpServer;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _sender;
        private readonly string _senderEmail;
        private readonly ILogger<MailService> _logger;
        private readonly IConfiguration _configuration;

        public MailService(IConfiguration configuration, ILoggerFactory loggerFactory)
        {

            _configuration = configuration;
            _smtpServer = "smtp.gmail.com";
            _smtpPort = 587;
            _smtpUser = "vozi002@gmail.com";
            _smtpPassword = "jwqb bjpa kyol lhia";
            _senderEmail = "vozi002@gmail.com";
            _sender = string.Empty;
            _logger = loggerFactory.CreateLogger<MailService>();
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
                    foreach (var recipient in _configuration.Recipients)
                    {
                        mailMessage.To.Add(recipient);
                    }

                    await smtpClient.SendMailAsync(mailMessage);
                    File.WriteAllText(@"C:\_code\geni_naloga\geni\test.html", GenerateHtmlBody(week));
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
