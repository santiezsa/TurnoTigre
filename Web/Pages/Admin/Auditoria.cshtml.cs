using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class AuditoriaModel : PageModel
    {
        private readonly AuditoriaNegocio negocio = new AuditoriaNegocio();

        [BindProperty(SupportsGet = true)] public string? Paciente { get; set; }
        [BindProperty(SupportsGet = true)] public string? Usuario { get; set; }
        [BindProperty(SupportsGet = true)] public string? Accion { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Desde { get; set; }
        [BindProperty(SupportsGet = true)] public DateTime? Hasta { get; set; }
        [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;

        public List<AuditoriaVista> Registros { get; set; } = new();
        public int Total { get; set; }
        public int TotalPaginas { get; set; } = 1;
        public string? Error { get; set; }

        public void OnGet()
        {
            try
            {
                var r = negocio.Buscar(Paciente, Usuario, Accion, Desde, Hasta, Pagina);
                Registros = r.Registros;
                Total = r.Total;
                Pagina = r.Pagina;
                TotalPaginas = r.TotalPaginas;
            }
            catch (Exception)
            {
                Error = "No se pudo cargar la auditoría. Revisá la conexión a la base de datos (y que esté creada la tabla AUDITORIA_ACCESO).";
            }
        }
    }
}
