using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class CatalogosModel : PageModel
    {
        private readonly EspecialidadNegocio especialidades = new EspecialidadNegocio();
        private readonly CentroSaludNegocio centrosNegocio = new CentroSaludNegocio();

        public List<(Especialidad Esp, int Profesionales)> Especialidades { get; set; } = new();
        public List<CentroSalud> Centros { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            try { Especialidades = especialidades.ListarConUso(); Centros = centrosNegocio.Listar(); }
            catch (Exception) { Error = "No se pudieron cargar los datos. Revisá la conexión con la base de datos."; }
        }

        private IActionResult Ejecutar(Action accion, string ok)
        {
            try { accion(); TempData["Mensaje"] = ok; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostGuardarEspecialidad(int? id, string? codigo, string? descripcion) =>
            Ejecutar(() => especialidades.Guardar(id, codigo ?? "", descripcion ?? ""), "La especialidad se guardó.");

        public IActionResult OnPostEliminarEspecialidad(int id) =>
            Ejecutar(() => especialidades.Eliminar(id), "La especialidad se eliminó.");

        public IActionResult OnPostGuardarCentro(int? id, string? codigo, string? nombre, string? direccion, string? localidad) =>
            Ejecutar(() => centrosNegocio.Guardar(new CentroSalud
            {
                IdCentro = id ?? 0, Codigo = codigo ?? "", Nombre = nombre ?? "", Direccion = direccion ?? "", Localidad = localidad ?? ""
            }), "El centro de salud se guardó.");

        public IActionResult OnPostEliminarCentro(int id) =>
            Ejecutar(() => centrosNegocio.Eliminar(id), "El centro de salud se eliminó.");
    }
}
