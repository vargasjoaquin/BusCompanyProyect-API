namespace Proyecto_EmpresaBus_API.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string destinatario, string asunto, string mensaje, byte[]? archivoAdjunto = null, string nombreArchivo = null);
    }
}
