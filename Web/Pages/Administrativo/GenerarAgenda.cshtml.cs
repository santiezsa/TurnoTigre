using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Globalization;

namespace Web.Pages.Administrativo
{
    public class FormAgenda
    {
        public int IdEspecialidad { get; set; }
        public int IdMedico { get; set; }
        public int IdCentro { get; set; }
        public string Tipo { get; set; } = "Mensual";
        public DateTime? Desde { get; set; } = DateTime.Today;
        public DateTime? Hasta { get; set; } = DateTime.Today.AddMonths(3);
        public string HoraInicio { get; set; } = "09:00";
        public string HoraFin { get; set; } = "13:00";
        public List<int> Dias { get; set; } = new();
        public int Duracion { get; set; } = 15;
        public int PorFranja { get; set; } = 1;
        public List<string> Fechas { get; set; } = new();   // "yyyy-MM-dd|HH:mm|HH:mm"
    }

    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class GenerarAgendaModel : PageModel
    {
        private readonly AgendaNegocio negocio = new AgendaNegocio();

        [BindProperty(SupportsGet = true)] public int? Id { get; set; }
        [BindProperty] public FormAgenda Form { get; set; } = new();

        public List<ProfesionalVista> Profesionales { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();
        public List<CentroSalud> Centros { get; set; } = new();
        public string? Error { get; set; }

        private void CargarListas()
        {
            try
            {
                Profesionales = new ProfesionalNegocio().Listar();
                Especialidades = new EspecialidadNegocio().Listar();
                Centros = new CentroSaludNegocio().Listar();
            }
            catch (Exception) { Error = "No se pudo conectar con la base de datos."; }
        }

        public IActionResult OnGet()
        {
            CargarListas();
            if (!Id.HasValue) Form.Dias = new List<int> { 1, 2, 3, 5 };   // valores sugeridos para una agenda nueva
            if (Id.HasValue)
            {
                try
                {
                    var a = negocio.Obtener(Id.Value);
                    if (a == null) return RedirectToPage("Agendas");
                    if (!a.Editable)
                    {
                        TempData["Error"] = "La agenda tiene turnos asignados: no se puede modificar.";
                        return RedirectToPage("Agendas");
                    }
                    Form = new FormAgenda
                    {
                        IdEspecialidad = a.IdEspecialidad, IdMedico = a.IdMedico, IdCentro = a.IdCentro, Tipo = a.Tipo,
                        Desde = a.VigenciaDesde, Hasta = a.VigenciaHasta, HoraInicio = string.IsNullOrEmpty(a.HoraInicio) ? "09:00" : a.HoraInicio,
                        HoraFin = string.IsNullOrEmpty(a.HoraFin) ? "13:00" : a.HoraFin,
                        Dias = (a.DiasSemana ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList(),
                        Duracion = a.DuracionMinutos, PorFranja = a.PacientesPorFranja
                    };
                    if (a.Tipo == "PorFecha")
                        Form.Fechas = negocio.Fechas(a.IdAgenda).Select(f => $"{f.Fecha:yyyy-MM-dd}|{f.HoraInicio}|{f.HoraFin}").ToList();
                }
                catch (Exception) { Error = "No se pudo cargar la agenda."; }
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            try
            {
                var n = new AgendaNueva
                {
                    IdMedico = Form.IdMedico, IdCentro = Form.IdCentro, IdEspecialidad = Form.IdEspecialidad, Tipo = Form.Tipo,
                    Desde = Form.Desde ?? DateTime.Today, Hasta = Form.Hasta ?? DateTime.Today,
                    HoraInicio = Form.HoraInicio ?? "", HoraFin = Form.HoraFin ?? "", Dias = Form.Dias ?? new(),
                    DuracionMinutos = Form.Duracion, PacientesPorFranja = Form.PorFranja
                };
                foreach (var f in Form.Fechas ?? new())
                {
                    var p = f.Split('|');
                    n.Fechas.Add(new FechaAgenda { Fecha = DateTime.ParseExact(p[0], "yyyy-MM-dd", CultureInfo.InvariantCulture), HoraInicio = p[1], HoraFin = p[2] });
                }
                if (Id.HasValue) negocio.Editar(Id.Value, n); else negocio.Crear(n);
                TempData["Mensaje"] = Id.HasValue ? "La agenda se modificó y se regeneraron sus turnos." : "¡Agenda generada! Los turnos ya están disponibles para reservar.";
                return RedirectToPage("Agendas");
            }
            catch (Exception ex)
            {
                CargarListas();
                Error = ex.Message;
                return Page();
            }
        }
    }
}
