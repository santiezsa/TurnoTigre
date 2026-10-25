using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using Web.Servicios;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,Medico,SuperAdmin")]
    public class PacienteDetalleModel : PageModel
    {
        private readonly PacienteNegocio pacientes = new PacienteNegocio();
        private readonly TurnoNegocio turnos = new TurnoNegocio();

        [BindProperty(SupportsGet = true)] public int Id { get; set; }

        public PacienteFicha? Paciente { get; set; }
        public PacienteFicha? Titular { get; set; }
        public HistoriaClinicaVista? Historia { get; set; }
        public bool HistoriaRestringida { get; set; }
        public List<TurnoVista> Citas { get; set; } = new();
        public string? Error { get; set; }
        private bool EsPersonal => User.IsInRole("Administrativo") || User.IsInRole("SuperAdmin");

        public IActionResult OnGet()
        {
            try
            {
                turnos.Actualizar();
                Paciente = pacientes.Obtener(Id);
                if (Paciente == null) return RedirectToPage("Pacientes");
                if (Paciente.IdTitular.HasValue) Titular = pacientes.Obtener(Paciente.IdTitular.Value);
                if (User.IsInRole("Medico") || User.IsInRole("SuperAdmin")) CargarHistoria();
                if (User.IsInRole("Administrativo") || User.IsInRole("SuperAdmin")) Citas = turnos.DePaciente(Id);
                return Page();
            }
            catch (Exception)
            {
                Error = "No se pudieron cargar los datos del paciente.";
                return Page();
            }
        }

        // Un médico ve la historia solo si atiende (o atendió) a este paciente; cada acceso queda registrado, también los denegados.
        private void CargarHistoria()
        {
            bool esSuper = User.IsInRole("SuperAdmin");
            int? idMedico = esSuper ? null : new ProfesionalNegocio().ObtenerPorUsuario(User.IdUsuario())?.IdMedico;
            var historias = new HistoriaClinicaNegocio();
            if (historias.AutorizarAcceso(User.IdUsuario(), esSuper ? Roles.SuperAdmin : Roles.Medico, idMedico, Id, HistoriaClinicaNegocio.AccionVer))
                Historia = historias.Obtener(Id);
            else
                HistoriaRestringida = true;
        }

        public IActionResult OnPostCancelar(int turno)
        {
            if (!EsPersonal) return Forbid();
            try { turnos.Cancelar(turno, null, "Cancelado por administración"); TempData["Mensaje"] = "El turno se canceló y el horario quedó libre."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostConfirmar(int turno)
        {
            if (!EsPersonal) return Forbid();
            try { turnos.Confirmar(turno, null); TempData["Mensaje"] = "El turno quedó confirmado."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage(new { Id });
        }
    }
}
