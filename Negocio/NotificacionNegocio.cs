using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class NotificacionNegocio
    {
        private readonly NotificacionDatos datos = new NotificacionDatos();

        /// <summary>Mails encolados por la reasignación (RF5) y el recordatorio (RF4) que todavía no salieron. Descarta antes los que ya vencieron.</summary>
        public List<CorreoPendiente> CorreosPendientes()
        {
            datos.DescartarCorreosVencidos();
            return datos.CorreosPendientes();
        }

        public void MarcarCorreoEnviado(int idNotificacion) => datos.MarcarCorreoEnviado(idNotificacion);
    }
}
