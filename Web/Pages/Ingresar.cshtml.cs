using Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;
using Web.Servicios;

namespace Web.Pages
{
    public class IngresarModel : PageModel
    {
        private readonly CorreoService correo;
        private readonly IConfiguration config;

        public IngresarModel(CorreoService correo, IConfiguration config)
        {
            this.correo = correo;
            this.config = config;
        }

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu correo.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá una contraseña.")]
        public string Contrasena { get; set; } = string.Empty;

        public string? Error { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            try
            {
                Usuario usuario = new UsuarioNegocio().ValidarUsuario(Correo, Contrasena);
                if (usuario.Rol != Roles.Paciente)
                {
                    Error = "Esta pantalla es para pacientes. Si sos personal del CAPS, tocá \"Soy personal del CAPS\".";
                    return Page();
                }
                return await SesionHelper.IniciarAsync(this, usuario);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                Contrasena = string.Empty;
                ModelState.Remove(nameof(Contrasena));
                return Page();
            }
        }

        // "¿Olvidaste tu contraseña?" (ventana modal)
        public IActionResult OnPostRecuperar(string? dato) => RecuperacionHelper.Procesar(this, dato, false, correo, config);
    }
}
