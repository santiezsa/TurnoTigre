using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Turno
    {
        public int IdTurno { get; set; }
        public int IdAgenda { get; set; }
        public Agenda? Agenda { get; set; }
        public int? IdPaciente { get; set; }
        public Paciente? Paciente { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = "Disponible";
        public DateTime? FechaReserva { get; set; }
        public DateTime? LimiteConfirmacion { get; set; }
        public string? Observaciones { get; set; }
    }
}
