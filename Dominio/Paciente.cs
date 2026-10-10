using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Paciente : Persona
    {
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = string.Empty;
        public string Calle { get; set; } = string.Empty;
        public string Altura { get; set; } = string.Empty;
        public string Localidad { get; set; } = string.Empty;
        public int? IdObraSocial { get; set; }
        public ObraSocial? ObraSocial { get; set; }
        public string? NroAfiliado { get; set; }
        public int? IdTitular { get; set; }
        public string? Parentesco { get; set; }

        public string DireccionCompleta => $"{Calle} {Altura}, {Localidad}".Trim();
    }
}
