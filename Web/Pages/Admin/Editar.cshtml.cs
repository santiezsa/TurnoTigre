using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;

namespace Web.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class EditarModel : PageModel
    {
        [BindProperty(SupportsGet = true)] public int Id { get; set; }

        // Solo lectura (no se editan: DNI, usuario y rol)
        public string NombreUsuario { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Rol { get; set; } = "";

        [BindProperty, Required(ErrorMessage = "Ingresá el nombre.")] public string Nombre { get; set; } = "";
        [BindProperty, Required(ErrorMessage = "Ingresá el apellido.")] public string Apellido { get; set; } = "";
        [BindProperty, Required(ErrorMessage = "Ingresá el correo."), EmailAddress(ErrorMessage = "El correo no es válido.")] public string Email { get; set; } = "";
        [BindProperty] public string? Telefono { get; set; }

        public string? Error { get; set; }

        private bool CargarSoloLectura()
        {
            var u = new UsuarioNegocio().ObtenerPorId(Id);
            if (u == null) return false;
            NombreUsuario = u.NombreUsuario;
            Dni = u.Dni;
            Rol = u.Rol;
            return true;
        }

        public IActionResult OnGet()
        {
            try
            {
                var u = new UsuarioNegocio().ObtenerPorId(Id);
                if (u == null) return RedirectToPage("Usuarios");
                CargarSoloLectura();
                Nombre = u.Nombre; Apellido = u.Apellido; Email = u.Email; Telefono = u.Telefono;
                return Page();
            }
            catch (Exception)
            {
                TempData["Error"] = "No se pudo cargar el usuario.";
                return RedirectToPage("Usuarios");
            }
        }

        public IActionResult OnPost()
        {
            if (!CargarSoloLectura()) return RedirectToPage("Usuarios");
            if (!ModelState.IsValid) return Page();

            try
            {
                new UsuarioNegocio().EditarUsuario(Id, Email, Nombre, Apellido, Telefono ?? "");
                TempData["Mensaje"] = "Usuario actualizado.";
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
