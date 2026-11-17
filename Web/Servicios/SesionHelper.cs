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
        /// Pantalla principal que le corresponde a cada rol.
        public static IActionResult DestinoPorRol(PageModel pagina, string rol) => rol switch
        {
            Roles.SuperAdmin => pagina.RedirectToPage("/Admin/Usuarios"),
            Roles.Administrativo => pagina.RedirectToPage("/Administrativo/TurnosDelDia"),
            Roles.Medico => pagina.RedirectToPage("/Medico/MiAgenda"),
            _ => pagina.RedirectToPage("/Pacientes/Index")
        };


        /// Duración de la sesión según el rol: 24 h para el SuperAdmin, jornada completa (8 h)
        /// para Administrativo y Médico, y una hora para el paciente.
        public static AuthenticationProperties PropiedadesPorRol(string rol)
        {
            TimeSpan duracion = rol switch
            {
                Roles.Administrativo or Roles.Medico => TimeSpan.FromHours(8),
                Roles.SuperAdmin => TimeSpan.FromHours(24),
                _ => TimeSpan.FromHours(1)   // Paciente
            };

            return new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.Add(duracion)
            };
        }

/// Inicia la sesión y devuelve la pantalla a la que corresponde ir según el rol.
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
            await pagina.HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad),
                PropiedadesPorRol(usuario.Rol));
            if (usuario.DebeCambiarPassword) return pagina.RedirectToPage("/CambiarPassword");

            return DestinoPorRol(pagina, usuario.Rol);
        }
    }
}