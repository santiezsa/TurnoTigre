using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Agenda
    {
        public int IdAgenda { get; set; }
        public int IdMedico { get; set; }
        public Medico? Medico { get; set; }
        public int IdCentro { get; set; }
        public CentroSalud? CentroSalud { get; set; }
        public DateTime VigenciaDesde { get; set; }
        public DateTime VigenciaHasta { get; set; }
        public int DuracionMinutos { get; set; }
        public int PacientesPorFranja { get; set; }
    }
}
