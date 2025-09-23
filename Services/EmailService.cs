using System.Net;
using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace UrlShortener.Services
{
    public class EmailService
    {
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPass;
        private readonly string _fromAddress;

        public EmailService(string smtpHost, int smtpPort, string smtpUser, string smtpPass, string fromAddress)
        {
            _smtpHost = smtpHost;
            _smtpPort = smtpPort;
            _smtpUser = smtpUser;
            _smtpPass = smtpPass;
            _fromAddress = fromAddress;
        }

        public async Task SendEmailAsync(string subject, string body, string fromAddress = null)
        {
            // grab email from enviroment variable EMAIL_TO_ADDRESS
            string? to = Environment.GetEnvironmentVariable("EMAIL_TO_ADDRESS");
            subject = "CONTACT FORM: " + subject;

            using (var client = new SmtpClient(_smtpHost, _smtpPort))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPass);

                var mail = new MailMessage(_fromAddress, to, subject, body)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mail);
            }
        }
    }
}