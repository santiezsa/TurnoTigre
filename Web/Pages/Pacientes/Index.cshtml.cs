using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio;
using Web.Datos;
using Web.Servicios;

namespace Web.Pages.Pacientes
{
    [Authorize(Roles = "Paciente,SuperAdmin")]
    public class IndexModel : PaginaPacienteBase
    {
        private readonly ListaEsperaNegocio espera = new ListaEsperaNegocio();

        public List<PacienteFicha> Grupo { get; set; } = new();
        public ReservaVM Reserva { get; set; } = new();
        public int Persona { get; set; }
        public string? Error { get; set; }

        private void Cargar(int persona, int centro, int esp, string? dia)
        {
            if (!CargarTitular()) return;
            turnos.Actualizar();
            Grupo = pacientes.GrupoFamiliar(Titular!.IdPaciente);
            Persona = Grupo.Any(g => g.IdPaciente == persona) ? persona : Titular.IdPaciente;
            try
            {
                Reserva = ReservaVM.Armar(turnos, centro, esp, dia == null ? null : Formato.Fecha(dia));
                Reserva.Centros = new CentroSaludNegocio().Listar();
                Reserva.Especialidades = new EspecialidadNegocio().Listar();
                Reserva.PermiteEspera = true;
                Reserva.Pagina = "Index";
                Reserva.Fijos["persona"] = Persona.ToString();
            }
            catch (Exception)
            {
                Error = "No se pudo conectar con la base de datos. Probá de nuevo en unos minutos.";
            }
        }

        public void OnGet(int persona, int centro, int esp, string? dia) => Cargar(persona, centro, esp, dia);

        public IActionResult OnPostReservar(int persona, int centro, int esp, string slot)
        {
            if (!CargarTitular()) return RedirectToPage();
            try
            {
                var partes = slot.Split('|');
                var fecha = DateTime.ParseExact(partes[1], "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
                var t = turnos.Reservar(Titular!.IdPaciente, persona, int.Parse(partes[0]), fecha);
                TempData["Mensaje"] = t.Estado == "Confirmado"
                    ? $"¡Turno reservado y confirmado! {t.PacienteNombre}: {Formato.FechaLarga(t.FechaHora)} a las {t.Hora} hs con {t.Profesional}."
                    : $"¡Turno reservado! {t.PacienteNombre}: {Formato.FechaLarga(t.FechaHora)} a las {t.Hora} hs con {t.Profesional}. Recordá confirmar tu asistencia hasta 24 horas antes desde \"Mis turnos\".";
                return RedirectToPage("MisTurnos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage(new { persona, centro, esp });
            }
        }

        public IActionResult OnPostEspera(int persona, int centro, int esp, string? franja)
        {
            if (!CargarTitular()) return RedirectToPage();
            try
            {
                espera.Inscribir(Titular!.IdPaciente, persona, esp, centro, franja ?? "Indistinto");
                TempData["Mensaje"] = "Listo, te anotamos en la lista de espera. Te vamos a avisar cuando se libere un turno.";
                return RedirectToPage("MisTurnos");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToPage(new { persona, centro, esp });
            }
        }
    }
}
