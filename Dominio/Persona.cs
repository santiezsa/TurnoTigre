using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Persona
    {
        public int IdPersona { get; set; }
        public int? IdUsuario { get; set; }
        public Usuario? Usuario { get; set; }
        public string Dni { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public bool Activo { get; set; } = true;

        public string NombreCompleto => $"{Apellido}, {Nombre}".Trim(',', ' ');

    }
}
