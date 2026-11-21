using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using Web.Datos;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class AgendarTurnoModel : PageModel
    {
        private readonly TurnoNegocio turnos = new TurnoNegocio();

        public PacienteFicha? Paciente { get; set; }
        public ReservaVM Reserva { get; set; } = new();
        public string? Error { get; set; }

        public IActionResult OnGet(int paciente, int centro, int esp, string? dia, string? desde, string? hasta, string? horario)
        {
            try
            {
                Paciente = new PacienteNegocio().Obtener(paciente);
                if (Paciente == null) return RedirectToPage("Pacientes");
                turnos.Actualizar();
                Reserva = ReservaVM.Armar(turnos, centro, esp, dia == null ? null : Formato.Fecha(dia),
                                          ReservaVM.FechaOpcional(desde), ReservaVM.FechaOpcional(hasta), horario);
                Reserva.Centros = new CentroSaludNegocio().Listar();
                Reserva.Especialidades = new EspecialidadNegocio().Listar();
                Reserva.PermiteEspera = false;
                Reserva.TextoBoton = "Agendar";
                Reserva.Fijos["paciente"] = paciente.ToString();
                Reserva.AvisoDuplicado = TempData[Pacientes.IndexModel.ClaveDuplicado] as string;
                Reserva.SlotDuplicado = TempData[Pacientes.IndexModel.ClaveSlotDuplicado] as string;
            }
            catch (Exception) { Error = "No se pudo conectar con la base de datos."; }
            return Page();
        }

        public IActionResult OnPostReservar(int paciente, int centro, int esp, string slot, bool aceptaDuplicado, string? desde, string? hasta, string? horario)
        {
            DateTime? dia = null;
            try
            {
                var partes = slot.Split('|');
                var fecha = DateTime.ParseExact(partes[1], "yyyyMMddHHmm", System.Globalization.CultureInfo.InvariantCulture);
                dia = fecha.Date;
                var t = turnos.Reservar(null, paciente, int.Parse(partes[0]), fecha, aceptaDuplicado);
                TempData["Mensaje"] = $"Turno {t.Numero} agendado: {Formato.FechaLarga(t.FechaHora)} a las {t.Hora} hs con {t.Profesional}" +
                                      (t.Estado == "Confirmado" ? " (quedó confirmado)." : ". El paciente debe confirmarlo hasta 24 horas antes.");
                return RedirectToPage("PacienteDetalle", new { id = paciente });
            }
            catch (TurnoDuplicadoException ex)
            {
                TempData[Pacientes.IndexModel.ClaveDuplicado] = ex.Message;
                TempData[Pacientes.IndexModel.ClaveSlotDuplicado] = slot;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage(new { paciente, centro, esp, desde, hasta, horario, dia = dia?.ToString("yyyy-MM-dd") });
        }
    }
}
