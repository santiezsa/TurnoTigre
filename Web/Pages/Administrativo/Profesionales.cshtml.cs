using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class ProfesionalesModel : PageModel
    {
        private readonly ProfesionalNegocio negocio = new ProfesionalNegocio();

        [BindProperty(SupportsGet = true)] public int? Editar { get; set; }

        public List<ProfesionalVista> Profesionales { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();
        public ProfesionalVista? Actual { get; set; }
        public string MatTipo { get; set; } = "";
        public string MatNum { get; set; } = "";
        public string? Error { get; set; }

        public void OnGet()
        {
            try
            {
                Profesionales = negocio.Listar();
                Especialidades = new EspecialidadNegocio().Listar();
                if (Editar.HasValue)
                {
                    Actual = Profesionales.FirstOrDefault(p => p.IdMedico == Editar.Value);
                    if (Actual != null)
                    {
                        var partes = Actual.Matricula.Split(' ', 2);
                        MatTipo = partes.Length == 2 ? partes[0] : "";
                        MatNum = partes.Length == 2 ? partes[1] : Actual.Matricula;
                    }
                }
            }
            catch (Exception) { Error = "No se pudieron cargar los profesionales. Revisá la conexión con la base de datos."; }
        }

        public IActionResult OnPostGuardar(int? id, string? nombre, string? apellido, string? dni, string? matTipo, string? matNum,
                                           string? email, string? tel, int[]? especialidades)
        {
            try
            {
                var p = new ProfesionalVista
                {
                    IdMedico = id ?? 0, Nombre = nombre ?? "", Apellido = apellido ?? "", Dni = dni ?? "",
                    Matricula = $"{matTipo} {matNum}".Trim(), Email = email ?? "", Telefono = tel ?? ""
                };
                var esps = (especialidades ?? Array.Empty<int>()).ToList();
                if (id.HasValue) negocio.Editar(p, esps); else negocio.Crear(p, esps);
                TempData["Mensaje"] = $"Se guardó a {Catalogos.Normalizar(p.Apellido)}, {Catalogos.Normalizar(p.Nombre)}.";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return id.HasValue ? RedirectToPage(new { editar = id }) : RedirectToPage();
            }
        }

        public IActionResult OnPostEliminar(int id)
        {
            try
            {
                var p = negocio.Obtener(id);
                negocio.Eliminar(id);
                TempData["Mensaje"] = p != null ? $"Se eliminó a {p.Completo}." : "El profesional se eliminó.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }
    }
}
