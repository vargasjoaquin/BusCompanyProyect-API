using MailKit.Net.Smtp;
using MimeKit;
using Proyecto_EmpresaBus_API.Interfaces;

namespace Proyecto_EmpresaBus_API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string destinatario, string asunto, string cuerpo)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            var smtpUser = _configuration["EmailSettings:SenderEmail"]; 
            var smtpPass = _configuration["EmailSettings:Password"];

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("Bux App", smtpUser));
            message.To.Add(new MailboxAddress(destinatario, destinatario));
            message.Subject = asunto;

            message.Body = new TextPart("plain")
            {
                Text = cuerpo
            };

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);

                await client.AuthenticateAsync(smtpUser, smtpPass);

                await client.SendAsync(message);

                await client.DisconnectAsync(true);
            }
        }
    }
}
