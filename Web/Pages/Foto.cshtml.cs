using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using Web.Servicios;

namespace Web.Pages
{
    /// <summary>Entrega la foto de perfil de un paciente. La ve el propio paciente (y su titular) y el personal del centro.</summary>
    [Authorize]
    public class FotoModel : PageModel
    {
        public IActionResult OnGet(int id)
        {
            var pacientes = new PacienteNegocio();
            if (User.IsInRole("Paciente") && !User.IsInRole("SuperAdmin"))
            {
                var titular = pacientes.DeUsuario(User.IdUsuario());
                var duenio = pacientes.Obtener(id);
                if (titular == null || duenio == null || (duenio.IdPaciente != titular.IdPaciente && duenio.IdTitular != titular.IdPaciente))
                    return Forbid();
            }
            var foto = pacientes.ObtenerFoto(id);
            if (foto == null) return NotFound();

            Response.Headers["Cache-Control"] = "private, no-cache";
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(foto.Value.Contenido, foto.Value.Tipo);
        }
    }
}
