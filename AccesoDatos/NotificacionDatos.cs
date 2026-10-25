using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    /// <summary>Cola de avisos por mail: NOTIFICACION con canal Email. Estados: Pendiente -> Enviada | Descartada (venció el plazo para responder).</summary>
    public class NotificacionDatos
    {
        /// <summary>Marca como descartados los mails pendientes cuyo plazo ya venció (avisar tarde confunde más de lo que ayuda).</summary>
        public int DescartarCorreosVencidos() =>
            Numero(@"UPDATE NOTIFICACION SET estado = 'Descartada'
                     WHERE canal = 'Email' AND estado = 'Pendiente' AND limite_respuesta IS NOT NULL AND limite_respuesta < GETDATE();
                     SELECT @@ROWCOUNT;");

        public List<CorreoPendiente> CorreosPendientes(int max = 50) =>
            Lista($@"SELECT TOP ({max}) n.id_notificacion, u.email, n.tipo_notificacion, n.mensaje
                     FROM NOTIFICACION n JOIN USUARIO u ON u.id_usuario = n.id_usuario
                     WHERE n.canal = 'Email' AND n.estado = 'Pendiente' AND u.activo = 1
                     ORDER BY n.id_notificacion",
                r => new CorreoPendiente { IdNotificacion = Ent(r, "id_notificacion"), Destino = Txt(r, "email"), Tipo = Txt(r, "tipo_notificacion"), Mensaje = Txt(r, "mensaje") });

        public void MarcarCorreoEnviado(int idNotificacion) =>
            Ejecutar("UPDATE NOTIFICACION SET estado = 'Enviada' WHERE id_notificacion = @n AND canal = 'Email'", ("@n", idNotificacion));
    }
}
