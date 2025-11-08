using System.Threading.Tasks;

namespace MiTiendaConLogin.Services
{
    // Renombrado de IEmailSender a IAppEmailSender
    public interface IAppEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string plainTextContent, string htmlContent);
    }
}