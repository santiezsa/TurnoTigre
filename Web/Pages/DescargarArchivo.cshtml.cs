using Dominio;
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
            var historias = new HistoriaClinicaNegocio();
            var archivo = historias.ObtenerArchivo(id);
            if (archivo == null) return NotFound();

            int idUsuario = User.IdUsuario();
            int idPaciente = archivo.Value.IdPaciente;
            string detalle = $"archivo {id}";

            if (User.IsInRole(Roles.SuperAdmin) || User.IsInRole(Roles.Medico))
            {
                // Un médico solo baja archivos de pacientes que atiende (o atendió); queda registrado, también si se deniega
                bool esSuper = User.IsInRole(Roles.SuperAdmin);
                int? idMedico = esSuper ? null : new ProfesionalNegocio().ObtenerPorUsuario(idUsuario)?.IdMedico;
                if (!historias.AutorizarAcceso(idUsuario, esSuper ? Roles.SuperAdmin : Roles.Medico, idMedico, idPaciente, HistoriaClinicaNegocio.AccionDescargar, detalle))
                    return Forbid();
            }
            else
            {
                // Los pacientes solo pueden bajar archivos propios o de sus familiares a cargo
                var pacientes = new PacienteNegocio();
                var titular = pacientes.DeUsuario(idUsuario);
                var duenio = pacientes.Obtener(idPaciente);
                if (titular == null || duenio == null || (duenio.IdPaciente != titular.IdPaciente && duenio.IdTitular != titular.IdPaciente))
                {
                    historias.RegistrarAcceso(idUsuario, Roles.Paciente, idPaciente, HistoriaClinicaNegocio.AccionDenegado, $"{HistoriaClinicaNegocio.AccionDescargar} {detalle}");
                    return Forbid();
                }
                historias.RegistrarAcceso(idUsuario, Roles.Paciente, idPaciente, HistoriaClinicaNegocio.AccionDescargar, detalle);
            }
            return File(archivo.Value.Contenido, string.IsNullOrWhiteSpace(archivo.Value.Tipo) ? "application/octet-stream" : archivo.Value.Tipo, archivo.Value.Nombre);
        }
    }
}
