using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,Medico,SuperAdmin")]
    public class PacientesModel : PageModel
    {
        public List<PacienteFicha> Pacientes { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            try { Pacientes = new PacienteNegocio().Buscar(null); }
            catch (Exception) { Error = "No se pudieron cargar los pacientes. Revisá la conexión con la base de datos."; }
        }
    }
}
