using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;

namespace Web.Pages.Administrativo
{
    [Authorize(Roles = "Administrativo,SuperAdmin")]
    public class PacienteFormModel : PageModel
    {
        private readonly PacienteNegocio negocio = new PacienteNegocio();

        [BindProperty(SupportsGet = true)] public int? Id { get; set; }

        public PacienteFicha? Paciente { get; set; }
        public List<ObraSocial> Obras { get; set; } = new();
        // Para volver a mostrar lo que se escribió si hay un error
        public PacienteFicha Datos { get; set; } = new() { FechaNacimiento = default };

        private void Cargar()
        {
            try { Obras = new ObraSocialNegocio().Listar(); } catch (Exception) { }
        }

        public IActionResult OnGet()
        {
            Cargar();
            if (Id.HasValue)
            {
                try
                {
                    Paciente = negocio.Obtener(Id.Value);
                    if (Paciente == null) return RedirectToPage("Pacientes");
                    Datos = Paciente;
                }
                catch (Exception)
                {
                    TempData["Error"] = "No se pudo cargar el paciente. Revisá la conexión con la base de datos.";
                    return RedirectToPage("Pacientes");
                }
            }
            return Page();
        }

        public IActionResult OnPost(string? nombre, string? apellido, string? sexo, string? tipoDoc, string? dni, DateTime? nacimiento,
                                    string? email, string? celular, string? fijo, string? calle, string? altura, string? cp, string? localidad,
                                    int? obra, string? afiliado)
        {
            Cargar();
            var f = new PacienteFicha
            {
                IdPaciente = Id ?? 0, Nombre = nombre ?? "", Apellido = apellido ?? "", Sexo = sexo ?? "", TipoDocumento = tipoDoc ?? "DNI",
                Dni = dni ?? "", FechaNacimiento = nacimiento ?? default, Email = email ?? "", Celular = celular ?? "", Fijo = fijo ?? "",
                Calle = calle ?? "", Altura = altura ?? "", CodigoPostal = cp ?? "", Localidad = localidad ?? "", IdObraSocial = obra, NroAfiliado = afiliado ?? ""
            };
            try
            {
                if (Id.HasValue) negocio.EditarPorPersonal(f); else negocio.CrearPorPersonal(f);
                TempData["Mensaje"] = $"Se guardó a {f.Apellido}, {f.Nombre}.";
                return RedirectToPage("Pacientes");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                if (Id.HasValue) Paciente = negocio.Obtener(Id.Value);
                Datos = f;
                return Page();
            }
        }
    }
}
