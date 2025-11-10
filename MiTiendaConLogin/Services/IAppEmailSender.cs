using System.Threading.Tasks;

namespace MiTiendaConLogin.Services
{
    public interface IAppEmailSender
    {
        // Para correos con HTML simple (ej. Notificación al Admin)
        Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent);

        // Para correos con Plantillas (ej. Correo al Cliente)
        Task SendEmailWithTemplateAsync(string toEmail, string templateId, object templateData);
    }
}