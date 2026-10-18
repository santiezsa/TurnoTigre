using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Web.Pages
{
    [Authorize]
    public class CambiarPasswordModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu contraseña actual.")]
        public string Actual { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá la nueva contraseña.")]
        public string Nueva { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Repetí la nueva contraseña.")]
        public string Repetir { get; set; } = string.Empty;

        public bool EsPrimerIngreso => User.FindFirstValue("DebeCambiarPassword") == "1";
        public string? Error { get; set; }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            if (Nueva != Repetir)
            {
                Error = "Las contraseñas nuevas no coinciden.";
                return Page();
            }

            try
            {
                int idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                new UsuarioNegocio().CambiarPassword(idUsuario, Actual, Nueva);

                // Volver a emitir la cookie sin la marca de "debe cambiar contraseña"
                var claims = User.Claims.Where(c => c.Type != "DebeCambiarPassword").ToList();
                claims.Add(new Claim("DebeCambiarPassword", "0"));
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

                TempData["Mensaje"] = "Contraseña actualizada correctamente.";
                if (User.IsInRole("SuperAdmin")) return RedirectToPage("/Admin/Usuarios");
                if (User.IsInRole("Administrativo")) return RedirectToPage("/Administrativo/TurnosDelDia");
                if (User.IsInRole("Medico")) return RedirectToPage("/Medico/MiAgenda");
                return RedirectToPage("/Pacientes/Index");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }
    }
}
