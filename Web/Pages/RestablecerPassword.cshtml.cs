using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;

namespace Web.Pages
{
    /// <summary>Pantalla a la que llega el enlace del correo para elegir una contraseña nueva.</summary>
    public class RestablecerPasswordModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public string? Token { get; set; }

        [BindProperty, Required(ErrorMessage = "Ingresá la contraseña nueva.")]
        public string Nueva { get; set; } = string.Empty;

        [BindProperty, Required(ErrorMessage = "Repetí la contraseña nueva.")]
        public string Repetir { get; set; } = string.Empty;

        public bool Vigente { get; private set; }
        public string? Error { get; private set; }

        private void ProtegerEnlace()
        {
            // El enlace no debe quedar en caché ni viajar a otros sitios como referencia
            Response.Headers["Referrer-Policy"] = "no-referrer";
            Response.Headers["Cache-Control"] = "no-store";
        }

        public void OnGet()
        {
            ProtegerEnlace();
            try { Vigente = new UsuarioNegocio().EnlaceDeRecuperacionVigente(Token); }
            catch (Exception) { Vigente = false; }
        }

        public IActionResult OnPost()
        {
            ProtegerEnlace();
            var negocio = new UsuarioNegocio();
            try
            {
                Vigente = negocio.EnlaceDeRecuperacionVigente(Token);
                if (!Vigente) return Page();
                if (!ModelState.IsValid) return Page();
                if (Nueva != Repetir) { Error = "Las contraseñas no coinciden."; return Page(); }

                bool personal = negocio.RestablecerConEnlace(Token, Nueva);
                TempData["Mensaje"] = "Tu contraseña se cambió correctamente. Ya podés ingresar con la nueva.";
                return personal ? RedirectToPage("/IngresarPersonal") : RedirectToPage("/Ingresar");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                try { Vigente = negocio.EnlaceDeRecuperacionVigente(Token); } catch (Exception) { Vigente = false; }
                return Page();
            }
        }
    }
}
