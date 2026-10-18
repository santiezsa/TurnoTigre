using System.Net;
using System.Net.Mail;

namespace Web.Servicios
{
    // Envío de mails por SMTP. Se configura en appsettings (sección "Correo").
    // Si no hay Host configurado no se envía nada: se deja el mensaje en el log (útil en desarrollo).
    public class CorreoService
    {
        private readonly IConfiguration config;
        private readonly ILogger<CorreoService> logger;

        public CorreoService(IConfiguration config, ILogger<CorreoService> logger)
        {
            this.config = config;
            this.logger = logger;
        }

        public bool EstaConfigurado => !string.IsNullOrWhiteSpace(config["Correo:Host"]);

        // Devuelve true si el mail salió, false si no hay SMTP configurado o falló el envío.
        public async Task<bool> EnviarAsync(string destino, string asunto, string cuerpo)
        {
            if (!EstaConfigurado)
            {
                logger.LogWarning("SMTP no configurado. Mail NO enviado a {Destino}: {Asunto}\n{Cuerpo}", destino, asunto, cuerpo);
                return false;
            }

            try
            {
                string remitente = config["Correo:Remitente"] ?? config["Correo:Usuario"] ?? "no-responder@tigre.gob.ar";
                using var smtp = new SmtpClient(config["Correo:Host"], int.TryParse(config["Correo:Puerto"], out int p) ? p : 587)
                {
                    EnableSsl = !string.Equals(config["Correo:Ssl"], "false", StringComparison.OrdinalIgnoreCase),
                    Credentials = string.IsNullOrWhiteSpace(config["Correo:Usuario"])
                        ? null
                        : new NetworkCredential(config["Correo:Usuario"], config["Correo:Clave"])
                };
                using var mensaje = new MailMessage(remitente, destino, asunto, cuerpo);
                await smtp.SendMailAsync(mensaje);
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "No se pudo enviar el mail a {Destino}", destino);
                return false;
            }
        }

        public Task<bool> EnviarCredencialesAsync(string destino, string nombreUsuario, string passwordProvisoria, bool esReset)
        {
            string asunto = esReset ? "TurnoTigre - Nueva contraseña provisoria" : "TurnoTigre - Tu cuenta fue creada";
            string cuerpo =
                (esReset ? "Se restableció tu contraseña.\n\n" : "Se creó tu cuenta en TurnoTigre.\n\n") +
                $"Usuario: {nombreUsuario}\n" +
                $"Ingresá con tu correo ({destino}) y esta contraseña provisoria: {passwordProvisoria}\n\n" +
                "Por seguridad, el sistema te pedirá cambiarla en tu primer ingreso.\n\n" +
                "Municipio de Tigre";
            return EnviarAsync(destino, asunto, cuerpo);
        }
    
        public Task<bool> EnviarRecuperacionAsync(string destino, string nombreUsuario, string enlace)
        {
            string cuerpo =
                "Recibimos un pedido para crear una contraseña nueva en TurnoTigre.\n\n" +
                $"Para elegirla, entrá a este enlace (sirve una sola vez y vence en 1 hora):\n{enlace}\n\n" +
                "Si no fuiste vos, ignorá este mensaje: tu contraseña actual sigue siendo la misma.\n\n" +
                "Municipio de Tigre";
            return EnviarAsync(destino, "TurnoTigre - Crear una contraseña nueva", cuerpo);
        }
}
}
