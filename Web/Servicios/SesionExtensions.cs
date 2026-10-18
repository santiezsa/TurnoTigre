using Dominio;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.Security.Claims;

namespace Web.Servicios
{
    public static class SesionExtensions
    {
        public static int IdUsuario(this ClaimsPrincipal u) => int.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);
        public static bool EsPersonal(this ClaimsPrincipal u) => u.IsInRole(Roles.Administrativo) || u.IsInRole(Roles.SuperAdmin);
    }

    /// <summary>Base de las páginas del paciente: carga la ficha del usuario que inició sesión.</summary>
    public abstract class PaginaPacienteBase : PageModel
    {
        protected readonly PacienteNegocio pacientes = new PacienteNegocio();
        protected readonly TurnoNegocio turnos = new TurnoNegocio();
        public PacienteFicha? Titular { get; protected set; }
        public int IdUsuarioActual => User.IdUsuario();

        protected bool CargarTitular()
        {
            try { Titular = pacientes.DeUsuario(IdUsuarioActual); }
            catch (Exception) { Titular = null; }
            return Titular != null;
        }
    }
}

namespace Web.Servicios
{
    /// <summary>Base de las páginas del médico. Un Super Admin elige el profesional con ?medico=ID.</summary>
    public abstract class PaginaMedicoBase : PageModel
    {
        protected readonly ProfesionalNegocio profesionales = new ProfesionalNegocio();
        protected readonly TurnoNegocio turnos = new TurnoNegocio();
        public ProfesionalVista? Profesional { get; protected set; }
        public List<ProfesionalVista> TodosLosProfesionales { get; protected set; } = new();
        public bool EsSuper => User.IsInRole(Roles.SuperAdmin);

        protected void CargarProfesional(int? medicoSolicitado)
        {
            try
            {
                if (EsSuper)
                {
                    TodosLosProfesionales = profesionales.Listar();
                    Profesional = TodosLosProfesionales.FirstOrDefault(p => p.IdMedico == medicoSolicitado) ?? TodosLosProfesionales.FirstOrDefault();
                }
                else
                {
                    Profesional = profesionales.ObtenerPorUsuario(User.IdUsuario());
                }
            }
            catch (Exception) { Profesional = null; }
        }
    }
}
