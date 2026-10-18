using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio;
using Web.Servicios;

namespace Web.Pages.Pacientes
{
    [Authorize(Roles = "Paciente,SuperAdmin")]
    public class MisTurnosModel : PaginaPacienteBase
    {
        private readonly ListaEsperaNegocio espera = new ListaEsperaNegocio();

        public List<TurnoVista> Proximos { get; set; } = new();
        public List<TurnoVista> Historial { get; set; } = new();
        public List<EsperaVista> Esperas { get; set; } = new();
        public List<NotificacionVista> Avisos { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            if (!CargarTitular()) return;
            try
            {
                turnos.Actualizar();
                var todos = turnos.DeGrupoFamiliar(Titular!.IdPaciente);
                Proximos = todos.Where(t => t.Futuro && (t.Estado == "PendienteConfirmacion" || t.Estado == "Confirmado")).OrderBy(t => t.FechaHora).ToList();
                Historial = todos.Except(Proximos).OrderByDescending(t => t.FechaHora).ToList();
                Esperas = espera.DeGrupoFamiliar(Titular.IdPaciente);
                Avisos = espera.Notificaciones(IdUsuarioActual).Where(n => !n.Leida).ToList();
            }
            catch (Exception)
            {
                Error = "No se pudieron cargar tus turnos. Probá de nuevo en unos minutos.";
            }
        }

        public IActionResult OnPostConfirmar(int id)
        {
            if (!CargarTitular()) return RedirectToPage();
            try { turnos.Confirmar(id, Titular!.IdPaciente); TempData["Mensaje"] = "¡Listo! Confirmaste tu asistencia."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostCancelar(int id)
        {
            if (!CargarTitular()) return RedirectToPage();
            try { turnos.Cancelar(id, Titular!.IdPaciente, "Cancelado por el paciente"); TempData["Mensaje"] = "El turno se canceló y el horario quedó libre para otras personas."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostSalirEspera(int id)
        {
            if (!CargarTitular()) return RedirectToPage();
            try { espera.Quitar(Titular!.IdPaciente, id); TempData["Mensaje"] = "Saliste de la lista de espera."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostLeerAvisos()
        {
            espera.MarcarLeidas(IdUsuarioActual);
            return RedirectToPage();
        }
    }
}
