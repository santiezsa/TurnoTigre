using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Notificacion
    {
        public int IdNotificacion { get; set; }
        public int IdUsuario { get; set; }
        public int? IdTurno { get; set; }
        public string Canal { get; set; } = "App"; // "Email", "WhatsApp", "App"
        public string TipoNotificacion { get; set; } = string.Empty;
        public string Mensaje { get; set; } = string.Empty;
        public DateTime FechaEnvio { get; set; } = DateTime.Now;
        public DateTime? LimiteRespuesta { get; set; }
        public bool Leida { get; set; } = false;
        public string Estado { get; set; } = "Pendiente";
    }
}
