using Dominio;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;
using Web.Servicios;

namespace Web.Pages.Admin
{
    [Authorize(Roles = "SuperAdmin")]
    public class UsuariosModel : PageModel
    {
        private readonly CorreoService correo;
        private readonly IWebHostEnvironment env;
        private readonly UsuarioNegocio negocio = new UsuarioNegocio();

        public UsuariosModel(CorreoService correo, IWebHostEnvironment env)
        {
            this.correo = correo;
            this.env = env;
        }

        [BindProperty(SupportsGet = true)] public string? Texto { get; set; }
        [BindProperty(SupportsGet = true)] public string? Rol { get; set; }
        [BindProperty(SupportsGet = true)] public string? Estado { get; set; }   // "activo" | "inactivo" | vacío
        [BindProperty(SupportsGet = true)] public int Pagina { get; set; } = 1;

        public List<UsuarioListado> Usuarios { get; set; } = new();
        public int Total { get; set; }
        public int TotalPaginas { get; set; } = 1;
        public int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        public string? Error { get; set; }

        public void OnGet() => Cargar();

        private void Cargar()
        {
            try
            {
                bool? activo = Estado == "activo" ? true : Estado == "inactivo" ? false : null;
                var todos = negocio.Listar(Texto, Rol, activo);
                Total = todos.Count;
                TotalPaginas = Math.Max(1, (int)Math.Ceiling(Total / (double)UsuarioNegocio.UsuariosPorPagina));
                Pagina = Math.Clamp(Pagina, 1, TotalPaginas);
                Usuarios = todos.Skip((Pagina - 1) * UsuarioNegocio.UsuariosPorPagina)
                                .Take(UsuarioNegocio.UsuariosPorPagina).ToList();
            }
            catch (Exception)
            {
                Error = "No se pudo cargar la lista de usuarios. Revisá la conexión a la base de datos.";
            }
        }

        public IActionResult OnPostCambiarEstado(int id, bool activar)
        {
            try
            {
                negocio.CambiarEstado(id, activar, IdActual);
                TempData["Mensaje"] = activar ? "Usuario habilitado." : "Usuario deshabilitado.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage(new { Texto, Rol, Estado, Pagina });
        }

        public async Task<IActionResult> OnPostRestablecerAsync(int id)
        {
            try
            {
                var usuario = negocio.ObtenerPorId(id);
                if (usuario == null) throw new Exception("El usuario no existe.");

                string provisoria = negocio.RestablecerPassword(id);
                bool enviado = await correo.EnviarCredencialesAsync(usuario.Email, usuario.NombreUsuario, provisoria, true);

                TempData["Mensaje"] = enviado
                    ? $"Se envió una nueva contraseña provisoria a {usuario.Email}."
                    : $"Contraseña restablecida, pero no se pudo enviar el mail (SMTP sin configurar)." +
                      (env.IsDevelopment() ? $" Provisoria (solo visible en desarrollo): {provisoria}" : "");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToPage(new { Texto, Rol, Estado, Pagina });
        }
    }
}
