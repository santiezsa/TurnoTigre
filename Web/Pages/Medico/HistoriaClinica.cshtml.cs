using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio;
using Web.Servicios;

namespace Web.Pages.Medico
{
    [Authorize(Roles = "Medico,SuperAdmin")]
    public class HistoriaClinicaModel : PaginaMedicoBase
    {
        private readonly HistoriaClinicaNegocio historias = new HistoriaClinicaNegocio();
        private readonly PacienteNegocio pacientes = new PacienteNegocio();

        [BindProperty(SupportsGet = true)] public int Turno { get; set; }
        [BindProperty(SupportsGet = true)] public int Paciente { get; set; }
        [BindProperty(SupportsGet = true)] public int? Medico { get; set; }

        public PacienteFicha? Ficha { get; set; }
        public HistoriaClinicaVista Historia { get; set; } = new();
        public TurnoVista? TurnoActual { get; set; }

        private bool Cargar()
        {
            CargarProfesional(Medico);
            try
            {
                int idPaciente = Paciente;
                if (Turno > 0)
                {
                    TurnoActual = turnos.Obtener(Turno);
                    if (TurnoActual?.IdPaciente != null) idPaciente = TurnoActual.IdPaciente.Value;
                    if (EsSuper && TurnoActual != null && Medico == null) { Medico = TurnoActual.IdMedico; CargarProfesional(Medico); }
                    if (TurnoActual != null && Profesional != null && TurnoActual.IdMedico != Profesional.IdMedico) { TurnoActual = null; idPaciente = Paciente; }
                }
                Ficha = pacientes.Obtener(idPaciente);
                if (Ficha != null) Historia = historias.Obtener(Ficha.IdPaciente);
            }
            catch (Exception) { Ficha = null; }
            return Ficha != null && Profesional != null;
        }

        public IActionResult OnGet()
        {
            if (!Cargar())
            {
                TempData["Error"] = "No se encontró el paciente o tu ficha de profesional.";
                return RedirectToPage("MiAgenda");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? grupo, decimal? peso, int? altura, string? alergias, string? cronicas, string? patologias,
                                                     string? observaciones, string? diagnostico, string? tratamientos, string? proximos, List<IFormFile>? archivos)
        {
            if (!Cargar())
            {
                TempData["Error"] = "No se encontró el paciente o tu ficha de profesional.";
                return RedirectToPage("MiAgenda");
            }
            try
            {
                if (string.IsNullOrWhiteSpace(grupo)) throw new Exception("Elegí el grupo sanguíneo.");
                if (!peso.HasValue || !altura.HasValue) throw new Exception("Ingresá el peso y la altura del paciente.");
                var h = new HistoriaClinicaVista
                {
                    IdPaciente = Ficha!.IdPaciente, GrupoSanguineo = grupo, Peso = peso, AlturaCm = altura,
                    Alergias = alergias ?? "", EnfermedadesCronicas = cronicas ?? "", Patologias = patologias ?? ""
                };
                var lista = new List<(string, string, byte[])>();
                foreach (var a in (archivos ?? new()).Where(x => x.Length > 0))
                {
                    if (a.Length > HistoriaClinicaNegocio.MaxBytes) throw new Exception($"\"{a.FileName}\" supera los 5 MB.");
                    using var ms = new MemoryStream();
                    await a.CopyToAsync(ms);
                    lista.Add((Path.GetFileName(a.FileName), a.ContentType, ms.ToArray()));
                }
                var c = new ConsultaVista { Observaciones = observaciones ?? "", Diagnostico = diagnostico ?? "", Tratamientos = tratamientos ?? "", ProximosPasos = proximos ?? "" };

                historias.GuardarDatosClinicos(h);
                historias.RegistrarConsulta(Profesional!.IdMedico, Ficha.IdPaciente, TurnoActual?.IdTurno, c, lista);
                TempData["Mensaje"] = "Se guardó la historia clínica" + (TurnoActual != null ? " y el turno quedó como Atendido." : ".");
                return RedirectToPage("MiAgenda", new { medico = EsSuper ? Profesional.IdMedico : (int?)null });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return Page();
            }
        }
    }
}
