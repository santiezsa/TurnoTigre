using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using Web.Datos;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class TurnosDelDiaModel : PageModel
    {
        private readonly TurnoNegocio negocio = new TurnoNegocio();

        [BindProperty(SupportsGet = true)] public DateTime? Fecha { get; set; }

        public DateTime Dia => (Fecha ?? DateTime.Today).Date;
        public bool EsHoy => Dia == DateTime.Today;
        public List<TurnoVista> Turnos { get; set; } = new();
        public CalendarioVM Calendario { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            try
            {
                negocio.Actualizar();
                Turnos = negocio.DelDia(Dia, null, null, null, null);
                Calendario = new CalendarioVM { Seleccionada = Dia, Pagina = "/Administrativo/TurnosDelDia", ConAtencion = negocio.DiasConTurnos(Dia, null) };
            }
            catch (Exception)
            {
                Error = "No se pudieron cargar los turnos. Revisá la conexión con la base de datos.";
                Calendario = new CalendarioVM { Seleccionada = Dia, Pagina = "/Administrativo/TurnosDelDia" };
            }
        }

        public IActionResult OnPostMarcar(int id, string estado)
        {
            try
            {
                negocio.MarcarAsistencia(id, estado == "Presente");
                TempData["Mensaje"] = $"Listo: el paciente quedó como {estado}.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage(new { Fecha = Dia.ToString("yyyy-MM-dd") });
        }

        public IActionResult OnPostCancelar(int id)
        {
            try
            {
                negocio.Cancelar(id, null, "Cancelado por administración");
                TempData["Mensaje"] = "El turno se canceló y el horario quedó libre.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage(new { Fecha = Dia.ToString("yyyy-MM-dd") });
        }
    }
}
