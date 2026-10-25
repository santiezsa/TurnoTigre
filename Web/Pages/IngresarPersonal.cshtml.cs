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
        private readonly ProteccionIngreso proteccion;

        public IngresarPersonalModel(CorreoService correo, IConfiguration config, ProteccionIngreso proteccion)
        {
            this.correo = correo;
            this.config = config;
            this.proteccion = proteccion;
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
            var bloqueo = proteccion.Bloqueo("personal", Usuario, HttpContext);
            if (bloqueo != null)
            {
                Error = bloqueo;
                Contrasena = string.Empty;
                ModelState.Remove(nameof(Contrasena));
                return Page();
            }
            try
            {
                var usuario = new UsuarioNegocio().ValidarPersonal(Usuario, Contrasena);
                proteccion.Exito("personal", Usuario);
                return await SesionHelper.IniciarAsync(this, usuario);
            }
            catch (Exception ex)
            {
                proteccion.Fallo("personal", Usuario, HttpContext);
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
