namespace Dominio
{
    /// <summary>Aviso por mail encolado (NOTIFICACION con canal Email) que todavía no se envió.</summary>
    public class CorreoPendiente
    {
        public int IdNotificacion { get; set; }
        public string Destino { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
    }
}
