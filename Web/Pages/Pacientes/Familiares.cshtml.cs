using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Servicios;

namespace Web.Pages.Pacientes
{
    [Authorize(Roles = "Paciente,SuperAdmin")]
    public class FamiliaresModel : PaginaPacienteBase
    {
        public List<PacienteFicha> Familiares { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet()
        {
            if (!CargarTitular()) return;
            try { Familiares = pacientes.Familiares(Titular!.IdPaciente); }
            catch (Exception) { Error = "No se pudieron cargar tus familiares."; }
        }

        public IActionResult OnPostAgregar(string? nombre, string? apellido, string? dni, DateTime? nacimiento, string? parentesco, string? sexo)
        {
            if (!CargarTitular()) return RedirectToPage();
            try
            {
                var f = new PacienteFicha
                {
                    Nombre = nombre ?? "", Apellido = apellido ?? "", Dni = dni ?? "",
                    FechaNacimiento = nacimiento ?? default, Parentesco = parentesco ?? "", Sexo = sexo ?? ""
                };
                pacientes.AgregarFamiliar(Titular!.IdPaciente, f);
                TempData["Mensaje"] = $"Agregaste a {f.Nombre} como familiar a cargo. Ya podés sacarle turnos.";
            }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }

        public IActionResult OnPostQuitar(int id)
        {
            if (!CargarTitular()) return RedirectToPage();
            try { pacientes.QuitarFamiliar(Titular!.IdPaciente, id); TempData["Mensaje"] = "El familiar se quitó de tu cuenta."; }
            catch (Exception ex) { TempData["Error"] = ex.Message; }
            return RedirectToPage();
        }
    }
}
