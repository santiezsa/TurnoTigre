using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class HistorialTurno
    {
        public int IdHistorial { get; set; }
        public int IdTurno { get; set; }
        public int? IdPaciente { get; set; }
        public DateTime FechaHora { get; set; } = DateTime.Now;
        public string? EstadoAnterior { get; set; }
        public string EstadoNuevo { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }
}
