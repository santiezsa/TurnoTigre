using Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;
using Web.Servicios;

namespace Web.Pages
{
    public class IngresarPersonalModel : PageModel
    {
        private readonly CorreoService correo;
        private readonly IConfiguration config;

        public IngresarPersonalModel(CorreoService correo, IConfiguration config)
        {
            this.correo = correo;
            this.config = config;
        }

        [BindProperty, Required(ErrorMessage = "Ingresá tu usuario.")]
        public string Usuario { get; set; } = string.Empty;

        [BindProperty, Required(ErrorMessage = "Ingresá tu contraseña.")]
        public string Contrasena { get; set; } = string.Empty;

        public string? Error { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();
            try
            {
                var usuario = new UsuarioNegocio().ValidarPersonal(Usuario, Contrasena);
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
        public IActionResult OnPostRecuperar(string? dato) => RecuperacionHelper.Procesar(this, dato, true, correo, config);
    }
}
