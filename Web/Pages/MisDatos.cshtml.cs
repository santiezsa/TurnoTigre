using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;

namespace Web.Pages
{
    [Authorize(Roles = "Administrativo,Medico,SuperAdmin")]
    public class MisDatosModel : PageModel
    {
        public string NombreUsuario { get; set; } = "";
        public string Rol { get; set; } = "";
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Email { get; set; } = "";
        public string Telefono { get; set; } = "";
        public string? Error { get; set; }

        public void OnGet()
        {
            try
            {
                var u = new UsuarioNegocio().ObtenerPorId(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
                if (u == null) return;
                NombreUsuario = u.NombreUsuario; Rol = u.Rol; Nombre = u.Nombre; Apellido = u.Apellido; Email = u.Email; Telefono = u.Telefono;
            }
            catch (Exception)
            {
                Error = "No se pudieron cargar tus datos.";
            }
        }

        public IActionResult OnPost(string nombre, string apellido, string email, string? telefono)
        {
            try
            {
                new UsuarioNegocio().EditarUsuario(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), email, nombre, apellido, telefono ?? "");
                TempData["Mensaje"] = "Tus datos se actualizaron.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage();
        }
    }
}
