using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class ListaEsperaDatos
    {
        public const int DiasVigencia = 30;

        public void VencerAntiguas() =>
            Ejecutar("UPDATE LISTA_ESPERA SET activa = 0 WHERE activa = 1 AND fecha_vencimiento IS NOT NULL AND fecha_vencimiento < GETDATE()");

        public bool Inscripto(int idPaciente, int idEspecialidad, int idCentro) =>
            Existe("SELECT COUNT(*) FROM LISTA_ESPERA WHERE id_paciente = @p AND id_especialidad = @e AND id_centro = @c AND activa = 1",
                ("@p", idPaciente), ("@e", idEspecialidad), ("@c", idCentro));

        public void Inscribir(int idPaciente, int idEspecialidad, int idCentro, string franja) =>
            Ejecutar(@"INSERT INTO LISTA_ESPERA (id_paciente, id_especialidad, id_centro, franja, fecha_vencimiento)
                       VALUES (@p, @e, @c, @f, DATEADD(DAY, " + DiasVigencia + ", GETDATE()))",
                ("@p", idPaciente), ("@e", idEspecialidad), ("@c", idCentro), ("@f", franja));

        public void Quitar(int idEspera) =>
            Ejecutar("UPDATE LISTA_ESPERA SET activa = 0 WHERE id_espera = @id", ("@id", idEspera));

        public void QuitarDePaciente(int idPaciente, int idEspecialidad) =>
            Ejecutar("UPDATE LISTA_ESPERA SET activa = 0 WHERE id_paciente = @p AND id_especialidad = @e AND activa = 1", ("@p", idPaciente), ("@e", idEspecialidad));

        public EsperaVista? Obtener(int idEspera) =>
            Uno(Seleccion + " WHERE le.id_espera = @id", Mapear, ("@id", idEspera));

        private const string Seleccion = @"
            SELECT le.id_espera, le.id_paciente, p.apellido, p.nombre, le.id_especialidad, e.descripcion AS esp,
                   le.id_centro, c.nombre AS centro, le.franja, le.fecha_inscripcion,
                   (SELECT COUNT(*) FROM LISTA_ESPERA x WHERE x.activa = 1 AND x.id_especialidad = le.id_especialidad
                       AND x.id_centro = le.id_centro AND x.fecha_inscripcion <= le.fecha_inscripcion) AS puesto
            FROM LISTA_ESPERA le
            JOIN PERSONA p ON p.id_persona = le.id_paciente
            JOIN ESPECIALIDAD e ON e.id_especialidad = le.id_especialidad
            JOIN CENTRO_SALUD c ON c.id_centro = le.id_centro ";

        private static EsperaVista Mapear(Microsoft.Data.SqlClient.SqlDataReader r) => new EsperaVista
        {
            IdEspera = Ent(r, "id_espera"),
            IdPaciente = Ent(r, "id_paciente"),
            Paciente = Txt(r, "nombre") + " " + Txt(r, "apellido"),
            IdEspecialidad = Ent(r, "id_especialidad"),
            Especialidad = Txt(r, "esp"),
            IdCentro = Ent(r, "id_centro"),
            Centro = Txt(r, "centro"),
            Franja = Txt(r, "franja"),
            FechaInscripcion = Fecha(r, "fecha_inscripcion"),
            Puesto = Ent(r, "puesto")
        };

        public List<EsperaVista> DeGrupoFamiliar(int idTitular) =>
            Lista(Seleccion + @" JOIN PACIENTE pac ON pac.id_paciente = le.id_paciente
                                 WHERE le.activa = 1 AND (le.id_paciente = @t OR pac.id_titular = @t)
                                 ORDER BY le.fecha_inscripcion", Mapear, ("@t", idTitular));

        /// <summary>
        /// RF5. Reasigna un cupo recién liberado (cancelación o vencimiento) al primer inscripto compatible de la lista de espera:
        /// mismo centro y especialidad, franja compatible, inscripción vigente, sin turno activo en esa especialidad y con usuario al que avisar.
        /// El elegido es el de inscripción más antigua. Queda con el turno en PendienteConfirmacion y plazo = MIN(ahora + 24 hs, cita - 1 h);
        /// sale de la lista y recibe una notificación. Si no hay nadie compatible (o faltan menos de 1 hs), el turno queda Disponible.
        /// Devuelve el id del turno reasignado, o 0 si no se reasignó.
        /// </summary>
        public int ReasignarCupo(int idAgenda, DateTime fechaHora, int idPacienteExcluido)
        {
            string franja = fechaHora.Hour < 13 ? "Mañana" : "Tarde";
            return Numero(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                DECLARE @ahora DATETIME = GETDATE();
                DECLARE @idTurno INT, @idEsp INT, @idPac INT, @idUsuario INT, @limite DATETIME, @msg VARCHAR(500);
                DECLARE @espec INT, @centro INT;
                SELECT @espec = id_especialidad, @centro = id_centro FROM AGENDA WHERE id_agenda = @a;

                SELECT TOP 1 @idTurno = id_turno FROM TURNO WITH (UPDLOCK, ROWLOCK, READPAST)
                WHERE id_agenda = @a AND fecha_hora = @f AND estado = 'Disponible' AND fecha_hora > DATEADD(HOUR, 1, @ahora)
                ORDER BY id_turno;

                IF @idTurno IS NOT NULL
                BEGIN
                    SELECT TOP 1 @idEsp = le.id_espera, @idPac = le.id_paciente, @idUsuario = COALESCE(pe.id_usuario, pt.id_usuario)
                    FROM LISTA_ESPERA le WITH (UPDLOCK, READPAST)
                    JOIN PERSONA pe ON pe.id_persona = le.id_paciente
                    JOIN PACIENTE pac ON pac.id_paciente = le.id_paciente
                    LEFT JOIN PERSONA pt ON pt.id_persona = pac.id_titular
                    WHERE le.activa = 1 AND le.id_especialidad = @espec AND le.id_centro = @centro
                      AND le.franja IN ('Indistinto', @fr)
                      AND (le.fecha_vencimiento IS NULL OR le.fecha_vencimiento > @ahora)
                      AND le.id_paciente <> @excl
                      AND COALESCE(pe.id_usuario, pt.id_usuario) IS NOT NULL
                      AND NOT EXISTS (SELECT 1 FROM TURNO t JOIN AGENDA a2 ON a2.id_agenda = t.id_agenda
                                      WHERE t.id_paciente = le.id_paciente AND a2.id_especialidad = @espec
                                        AND t.fecha_hora > @ahora AND t.estado IN ('PendienteConfirmacion', 'Confirmado'))
                    ORDER BY le.fecha_inscripcion, le.id_espera;
                END

                IF @idTurno IS NOT NULL AND @idEsp IS NOT NULL
                BEGIN
                    SET @limite = CASE WHEN DATEADD(HOUR, 24, @ahora) < DATEADD(HOUR, -1, @f) THEN DATEADD(HOUR, 24, @ahora) ELSE DATEADD(HOUR, -1, @f) END;
                    UPDATE TURNO SET id_paciente = @idPac, estado = 'PendienteConfirmacion', fecha_reserva = @ahora, limite_confirmacion = @limite
                    WHERE id_turno = @idTurno;
                    INSERT INTO HISTORIAL_TURNO (id_turno, id_paciente, estado_anterior, estado_nuevo, motivo)
                    VALUES (@idTurno, @idPac, 'Disponible', 'PendienteConfirmacion', 'Reasignado desde la lista de espera');
                    UPDATE LISTA_ESPERA SET activa = 0 WHERE id_espera = @idEsp;
                    SELECT @msg = CONCAT('Se liberó un turno y se te asignó automáticamente para ', p.nombre, ' ', p.apellido, ': ', e.descripcion, ' en ', c.nombre,
                                         ' el ', LEFT(CONVERT(VARCHAR(10), @f, 103), 5), ' a las ', CONVERT(VARCHAR(5), @f, 108),
                                         ' hs. Confirmalo antes del ', LEFT(CONVERT(VARCHAR(10), @limite, 103), 5), ' a las ', CONVERT(VARCHAR(5), @limite, 108),
                                         ' hs o pasará al siguiente de la lista.')
                    FROM PERSONA p, ESPECIALIDAD e, CENTRO_SALUD c
                    WHERE p.id_persona = @idPac AND e.id_especialidad = @espec AND c.id_centro = @centro;
                    INSERT INTO NOTIFICACION (id_usuario, id_turno, canal, tipo_notificacion, mensaje, limite_respuesta, estado)
                    VALUES (@idUsuario, @idTurno, 'App', 'TurnoReasignado', @msg, @limite, 'Pendiente');
                END

                COMMIT;
                SELECT CASE WHEN @idEsp IS NOT NULL THEN @idTurno ELSE 0 END;",
                ("@a", idAgenda), ("@f", fechaHora), ("@fr", franja), ("@excl", idPacienteExcluido));
        }

        public List<NotificacionVista> NotificacionesDeUsuario(int idUsuario, int max = 10) =>
            Lista($@"SELECT TOP ({max}) id_notificacion, tipo_notificacion, mensaje, fecha_envio, leida
                     FROM NOTIFICACION WHERE id_usuario = @u ORDER BY fecha_envio DESC",
                r => new NotificacionVista { IdNotificacion = Ent(r, "id_notificacion"), Tipo = Txt(r, "tipo_notificacion"), Mensaje = Txt(r, "mensaje"), Fecha = Fecha(r, "fecha_envio"), Leida = Bool(r, "leida") },
                ("@u", idUsuario));

        public void MarcarLeidas(int idUsuario) =>
            Ejecutar("UPDATE NOTIFICACION SET leida = 1 WHERE id_usuario = @u AND leida = 0", ("@u", idUsuario));
    }
}
