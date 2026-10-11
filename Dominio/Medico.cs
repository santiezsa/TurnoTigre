using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Medico : Persona
    {
        public string Matricula { get; set; } = string.Empty;
        public int IdEspecialidad { get; set; }
        public Especialidad? Especialidad { get; set; }
    }
}
