using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public static class Roles
    {
        public const string Paciente = "Paciente";
        public const string Medico = "Medico";
        public const string Administrativo = "Administrativo";
        public const string SuperAdmin = "SuperAdmin";

        public static readonly string[] Todos = { Paciente, Medico, Administrativo, SuperAdmin };
    }

    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = "Paciente";
        public bool Activo { get; set; } = true;
        public bool DebeCambiarPassword { get; set; }
    }
}
