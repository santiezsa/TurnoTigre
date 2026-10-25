namespace Dominio
{
    public class NotificacionVista
    {
        public int IdNotificacion { get; set; }
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
    }
}
