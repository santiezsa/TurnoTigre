using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Servicios
{
    /// <summary>"Olvidé mi contraseña": responde siempre lo mismo, exista o no la cuenta.</summary>
    public static class RecuperacionHelper
    {
        public const string MensajeGeneral = "Si la cuenta está registrada, te enviamos un enlace al correo asociado para crear una contraseña nueva.";

        public static IActionResult Procesar(PageModel pagina, string? identificador, bool personal, CorreoService correo, IConfiguration config)
        {
            bool desdeModal = pagina.Request.Headers["X-Requested-With"] == "fetch";
            try
            {
                var solicitud = new UsuarioNegocio().SolicitarRecuperacion(identificador ?? "", personal);
                if (solicitud != null)
                {
                    // La dirección base sale de la configuración si está (evita enlaces armados con un Host falso)
                    string baseUrl = (config["Correo:UrlBase"] ?? "").Trim().TrimEnd('/');
                    if (baseUrl == "") baseUrl = $"{pagina.Request.Scheme}://{pagina.Request.Host}";
                    string enlace = $"{baseUrl}/RestablecerPassword?token={Uri.EscapeDataString(solicitud.Token)}";
                    // Sin esperar el envío: así la respuesta tarda lo mismo exista o no la cuenta
                    _ = Task.Run(() => correo.EnviarRecuperacionAsync(solicitud.Email, solicitud.NombreUsuario, enlace));
                }
                if (desdeModal) return new JsonResult(new { ok = true });
                pagina.TempData["Mensaje"] = MensajeGeneral;
                return pagina.RedirectToPage();
            }
            catch (Exception)
            {
                const string error = "No pudimos procesar el pedido en este momento. Probá de nuevo en unos minutos.";
                if (desdeModal) return new JsonResult(new { ok = false, mensaje = error });
                pagina.TempData["Error"] = error;
                return pagina.RedirectToPage();
            }
        }
    }
}
