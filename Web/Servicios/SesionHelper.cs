using Dominio;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Web.Servicios
{
    public static class SesionHelper
    {
        /// <summary>Inicia la sesión y devuelve la pantalla a la que corresponde ir según el rol.</summary>
        public static async Task<IActionResult> IniciarAsync(PageModel pagina, Usuario usuario)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol),
                new Claim("DebeCambiarPassword", usuario.DebeCambiarPassword ? "1" : "0")
            };
            var identidad = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await pagina.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

            if (usuario.DebeCambiarPassword) return pagina.RedirectToPage("/CambiarPassword");
            return usuario.Rol switch
            {
                Roles.SuperAdmin => pagina.RedirectToPage("/Admin/Usuarios"),
                Roles.Administrativo => pagina.RedirectToPage("/Administrativo/TurnosDelDia"),
                Roles.Medico => pagina.RedirectToPage("/Medico/MiAgenda"),
                _ => pagina.RedirectToPage("/Pacientes/Index")
            };
        }
    }
}
