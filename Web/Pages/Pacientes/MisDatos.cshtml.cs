using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Negocio;
using Web.Servicios;

namespace Web.Pages.Pacientes
{
    [Authorize(Roles = "Paciente,SuperAdmin")]
    public class MisDatosModel : PaginaPacienteBase
    {
        public List<ObraSocial> Obras { get; set; } = new();

        public void OnGet()
        {
            if (!CargarTitular()) return;
            try { Obras = new ObraSocialNegocio().Listar(); } catch (Exception) { }
        }

        public async Task<IActionResult> OnPostFotoAsync(IFormFile? foto)
        {
            if (!CargarTitular()) return RedirectToPage();
            try
            {
                if (foto == null || foto.Length == 0) throw new Exception("Elegí una foto.");
                if (foto.Length > PacienteNegocio.FotoMaxBytes) throw new Exception("La foto pesa más de 2 MB. Elegí una más liviana.");
                using var ms = new MemoryStream();
                await foto.CopyToAsync(ms);
                pacientes.GuardarFoto(Titular!.IdPaciente, ms.ToArray());
                TempData["Mensaje"] = "Tu foto de perfil se actualizó.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostQuitarFoto()
        {
            if (!CargarTitular()) return RedirectToPage();
            try { pacientes.QuitarFoto(Titular!.IdPaciente); TempData["Mensaje"] = "Se quitó tu foto de perfil."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPost(string? apellido, string? nombre, string? calle, string? altura, string? localidad,
                                    string? telefono, string? fijo, DateTime? nacimiento, string? sexo, int? obra, string? afiliado, string? cp)
        {
            if (!CargarTitular()) return RedirectToPage();
            try
            {
                var f = new PacienteFicha
                {
                    IdPaciente = Titular!.IdPaciente, Apellido = apellido ?? "", Nombre = nombre ?? "", Calle = calle ?? "", Altura = altura ?? "",
                    Localidad = localidad ?? "", Celular = telefono ?? "", Fijo = fijo ?? "", FechaNacimiento = nacimiento ?? Titular.FechaNacimiento,
                    Sexo = sexo ?? Titular.Sexo, IdObraSocial = obra, NroAfiliado = afiliado ?? "", CodigoPostal = cp ?? ""
                };
                pacientes.GuardarMisDatos(f);
                TempData["Mensaje"] = "Tus datos se actualizaron correctamente.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }
    }
}
