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
                     WHERE canal = 'Email' AND estado = 'Pendiente' AND limite_respuesta IS NOT NULL AND limite_respuesta < dbo.Ahora();
                     SELECT @@ROWCOUNT;");

        public List<CorreoPendiente> CorreosPendientes(int max = 50) =>
            Lista($@"SELECT TOP ({max}) n.id_notificacion, u.email, n.tipo_notificacion, n.mensaje
                     FROM NOTIFICACION n JOIN USUARIO u ON u.id_usuario = n.id_usuario
                     WHERE n.canal = 'Email' AND n.estado = 'Pendiente' AND u.activo = 1
                     ORDER BY n.id_notificacion",
                r => new CorreoPendiente { IdNotificacion = Ent(r, "id_notificacion"), Destino = Txt(r, "email"), Tipo = Txt(r, "tipo_notificacion"), Mensaje = Txt(r, "mensaje") });

        /// <summary>
        /// Deja el aviso de un turno para la persona del turno (o su titular, si es un familiar sin cuenta): siempre por mail y, si se pide, también en la app.
        /// El mail no sale después de la hora del turno (limite_respuesta = fecha del turno). Devuelve cuántas filas creó (0 si no hay a quién avisar).
        /// </summary>
        public int AvisarTurno(int idTurno, string tipo, string mensaje, bool enApp) =>
            Numero(@"INSERT INTO NOTIFICACION (id_usuario, id_turno, canal, tipo_notificacion, mensaje, limite_respuesta, estado)
                     SELECT COALESCE(pe.id_usuario, pt.id_usuario), t.id_turno, canales.canal, @tipo, @msg, t.fecha_hora, 'Pendiente'
                     FROM TURNO t
                     JOIN PERSONA pe ON pe.id_persona = t.id_paciente
                     JOIN PACIENTE pac ON pac.id_paciente = t.id_paciente
                     LEFT JOIN PERSONA pt ON pt.id_persona = pac.id_titular
                     CROSS JOIN (VALUES ('Email'), ('App')) canales(canal)
                     WHERE t.id_turno = @t AND COALESCE(pe.id_usuario, pt.id_usuario) IS NOT NULL
                       AND (canales.canal = 'Email' OR @app = 1);
                     SELECT @@ROWCOUNT;",
                ("@t", idTurno), ("@tipo", tipo), ("@msg", mensaje.Length > 500 ? mensaje[..500] : mensaje), ("@app", enApp));

        public void MarcarCorreoEnviado(int idNotificacion) =>
            Ejecutar("UPDATE NOTIFICACION SET estado = 'Enviada' WHERE id_notificacion = @n AND canal = 'Email'", ("@n", idNotificacion));
    }
}
