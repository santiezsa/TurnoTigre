using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class ListaEspera
    {
        public int IdEspera { get; set; }
        public int IdPaciente { get; set; }
        public Paciente? Paciente { get; set; }
        public int IdEspecialidad { get; set; }
        public Especialidad? Especialidad { get; set; }
        public int IdCentro { get; set; }
        public CentroSalud? CentroSalud { get; set; }
        public string Franja { get; set; } = "Indistinto"; // "Mañana", "Tarde", "Indistinto"
        public DateTime FechaInscripcion { get; set; } = DateTime.Now;
        public DateTime? FechaVencimiento { get; set; }
        public bool Activa { get; set; } = true;
    }
}
