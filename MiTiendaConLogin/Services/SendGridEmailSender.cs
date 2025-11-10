using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace MiTiendaConLogin.Services
{
    public class SendGridEmailSender : IAppEmailSender
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly IConfiguration _configuration;

        // Estos valores los leeremos de la configuración
        private string _fromEmail;
        private string _fromName;

        public SendGridEmailSender(ISendGridClient sendGridClient, IConfiguration configuration)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;

            // Lee el email y nombre "De" (From) del appsettings.json
            _fromEmail = _configuration["SendGrid:FromEmail"];
            _fromName = _configuration["SendGrid:FromName"];
        }

        public async Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent)
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

            await _sendGridClient.SendEmailAsync(msg);
        }


        public async Task SendEmailWithTemplateAsync(string toEmail, string templateId, object templateData)
        {
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(toEmail);

            // Crea el mensaje usando una plantilla dinámica
            var msg = MailHelper.CreateSingleTemplateEmail(from, to, templateId, templateData);

            // Envía el correo
            await _sendGridClient.SendEmailAsync(msg);
        }
    }
}