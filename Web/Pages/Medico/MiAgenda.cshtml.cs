using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Datos;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Web.Servicios;

namespace Web.Pages.Medico
{
    [Authorize(Roles = "Medico,SuperAdmin")]
    public class MiAgendaModel : PaginaMedicoBase
    {
        [BindProperty(SupportsGet = true)] public DateTime? Fecha { get; set; }
        [BindProperty(SupportsGet = true)] public int? Medico { get; set; }

        public DateTime Dia => (Fecha ?? DateTime.Today).Date;
        public bool EsHoy => Dia == DateTime.Today;
        public List<TurnoVista> Turnos { get; set; } = new();
        public CalendarioVM Calendario { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            CargarProfesional(Medico);
            Calendario = new CalendarioVM { Seleccionada = Dia, Pagina = "/Medico/MiAgenda", Medico = EsSuper ? Profesional?.IdMedico : null };
            if (Profesional == null) return;
            try
            {
                turnos.Actualizar();
                Turnos = turnos.DelMedicoEnDia(Profesional.IdMedico, Dia);
                Calendario.ConAtencion = turnos.DiasConTurnos(Dia, Profesional.IdMedico);
            }
            catch (Exception) { Error = "No se pudo cargar tu agenda. Revisá la conexión con la base de datos."; }
        }

        public IActionResult OnPostAtender(int id, int? medico)
        {
            CargarProfesional(medico);
            try
            {
                var t = turnos.Obtener(id);
                if (t == null || Profesional == null || t.IdMedico != Profesional.IdMedico) throw new Exception("Ese turno no es de tu agenda.");
                turnos.IniciarAtencion(id);
                return RedirectToPage("HistoriaClinica", new { turno = id, medico });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage(new { Fecha = Dia.ToString("yyyy-MM-dd"), medico });
            }
        }
    }
}
