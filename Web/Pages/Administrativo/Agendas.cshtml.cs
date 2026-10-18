using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class AgendasModel : PageModel
    {
        private readonly AgendaNegocio negocio = new AgendaNegocio();

        public List<AgendaVista> Agendas { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            try
            {
                new TurnoNegocio().Actualizar();
                Agendas = negocio.Listar();
                Especialidades = new EspecialidadNegocio().Listar();
            }
            catch (Exception) { Error = "No se pudieron cargar las agendas. Revisá la conexión con la base de datos."; }
        }

        public IActionResult OnPostEliminar(int id)
        {
            try { negocio.Eliminar(id); TempData["Mensaje"] = "La agenda se eliminó junto con sus turnos libres."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }
    }
}
