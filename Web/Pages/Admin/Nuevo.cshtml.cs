using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;
using Web.Servicios;

namespace Web.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class NuevoModel : PageModel
    {
        private readonly CorreoService correo;
        private readonly IWebHostEnvironment env;

        public NuevoModel(CorreoService correo, IWebHostEnvironment env)
        {
            this.correo = correo;
            this.env = env;
        }

        [BindProperty, Required(ErrorMessage = "Elegí un rol.")] public string Rol { get; set; } = "Administrativo";
        [BindProperty, Required(ErrorMessage = "Ingresá el nombre.")] public string Nombre { get; set; } = "";
        [BindProperty, Required(ErrorMessage = "Ingresá el apellido.")] public string Apellido { get; set; } = "";
        [BindProperty, Required(ErrorMessage = "Ingresá el DNI.")] public string Dni { get; set; } = "";
        [BindProperty, Required(ErrorMessage = "Ingresá el correo."), EmailAddress(ErrorMessage = "El correo no es válido.")] public string Email { get; set; } = "";
        [BindProperty] public string? Telefono { get; set; }
        [BindProperty] public string? Matricula { get; set; }
        [BindProperty] public int? IdEspecialidad { get; set; }

        public List<Especialidad> Especialidades { get; set; } = new();
        public string? Error { get; set; }

        public void OnGet() => CargarEspecialidades();

        private void CargarEspecialidades()
        {
            try { Especialidades = new EspecialidadNegocio().Listar(); }
            catch (Exception) { Especialidades = new(); }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            CargarEspecialidades();
            if (!ModelState.IsValid) return Page();

            try
            {
                var creado = new UsuarioNegocio().CrearUsuarioInterno(
                    Rol, Nombre, Apellido, Dni, Email, Telefono ?? "", Matricula, IdEspecialidad);

                bool enviado = await correo.EnviarCredencialesAsync(creado.Email, creado.NombreUsuario, creado.PasswordProvisoria, false);

                TempData["Mensaje"] = enviado
                    ? $"Usuario \"{creado.NombreUsuario}\" creado. La contraseña provisoria se envió a {creado.Email}."
                    : $"Usuario \"{creado.NombreUsuario}\" creado, pero no se pudo enviar el mail (SMTP sin configurar)." +
                      (env.IsDevelopment() ? $" Provisoria (solo visible en desarrollo): {creado.PasswordProvisoria}" : " Restablecé la clave cuando el correo esté configurado.");
                return RedirectToPage("Usuarios");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }
    }
}
