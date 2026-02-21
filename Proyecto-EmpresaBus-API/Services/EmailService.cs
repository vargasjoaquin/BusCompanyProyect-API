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

        public async Task SendEmailAsync(string destinatario, string asunto, string mensaje, bool isHtml = false, byte[]? archivoAdjunto = null, string nombreArchivo = null)
        {
            var smtpServer = _configuration["EmailSettings:SmtpServer"];
            var smtpPort = int.Parse(_configuration["EmailSettings:Port"]);
            var smtpUser = _configuration["EmailSettings:SenderEmail"];
            var smtpPass = _configuration["EmailSettings:Password"];

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress("Bux App", smtpUser));
            email.To.Add(new MailboxAddress(destinatario, destinatario));
            email.Subject = asunto;

            var builder = new BodyBuilder();

            if (isHtml) builder.HtmlBody = mensaje;
            else builder.TextBody = mensaje;

            if (archivoAdjunto != null && !string.IsNullOrEmpty(nombreArchivo))
            {
                builder.Attachments.Add(nombreArchivo, archivoAdjunto);
            }

            email.Body = builder.ToMessageBody();

            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(smtpServer, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(smtpUser, smtpPass);
                await client.SendAsync(email);
                await client.DisconnectAsync(true);
            }
        }
    }

}
