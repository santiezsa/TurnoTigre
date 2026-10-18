using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using Web.Servicios;

namespace Web.Pages
{
    [Authorize(Roles = "Medico,SuperAdmin,Paciente")]
    public class DescargarArchivoModel : PageModel
    {
        public IActionResult OnGet(int id)
        {
            var archivo = new HistoriaClinicaNegocio().ObtenerArchivo(id);
            if (archivo == null) return NotFound();

            // Los pacientes solo pueden bajar archivos propios o de sus familiares a cargo
            if (User.IsInRole("Paciente") && !User.IsInRole("SuperAdmin"))
            {
                var pacientes = new PacienteNegocio();
                var titular = pacientes.DeUsuario(User.IdUsuario());
                var duenio = pacientes.Obtener(archivo.Value.IdPaciente);
                if (titular == null || duenio == null || (duenio.IdPaciente != titular.IdPaciente && duenio.IdTitular != titular.IdPaciente))
                    return Forbid();
            }
            return File(archivo.Value.Contenido, string.IsNullOrWhiteSpace(archivo.Value.Tipo) ? "application/octet-stream" : archivo.Value.Tipo, archivo.Value.Nombre);
        }
    }
}
