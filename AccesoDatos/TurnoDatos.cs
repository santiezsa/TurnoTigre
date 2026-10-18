using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class TurnoDatos
    {
        private const string Seleccion = @"
            SELECT t.id_turno, t.id_agenda, t.fecha_hora, t.estado, t.id_paciente, t.limite_confirmacion,
                   pp.apellido AS ap_pac, pp.nombre AS no_pac, pp.dni AS dni_pac, pp.telefono AS tel_pac,
                   a.id_medico, pm.apellido AS ap_med, pm.nombre AS no_med,
                   a.id_especialidad, e.descripcion AS esp, a.id_centro, c.nombre AS centro
            FROM TURNO t
            JOIN AGENDA a ON a.id_agenda = t.id_agenda
            JOIN PERSONA pm ON pm.id_persona = a.id_medico
            JOIN CENTRO_SALUD c ON c.id_centro = a.id_centro
            LEFT JOIN ESPECIALIDAD e ON e.id_especialidad = a.id_especialidad
            LEFT JOIN PERSONA pp ON pp.id_persona = t.id_paciente ";

        private static TurnoVista Mapear(Microsoft.Data.SqlClient.SqlDataReader r) => new TurnoVista
        {
            IdTurno = Ent(r, "id_turno"),
            IdAgenda = Ent(r, "id_agenda"),
            FechaHora = Fecha(r, "fecha_hora"),
            Estado = Txt(r, "estado"),
            IdPaciente = EntN(r, "id_paciente"),
            Paciente = r["ap_pac"] is DBNull ? "" : Txt(r, "ap_pac") + ", " + Txt(r, "no_pac"),
            Dni = Txt(r, "dni_pac"),
            Telefono = Txt(r, "tel_pac"),
            IdMedico = Ent(r, "id_medico"),
            Profesional = Txt(r, "ap_med") + ", " + Txt(r, "no_med"),
            IdEspecialidad = Ent(r, "id_especialidad"),
            Especialidad = Txt(r, "esp"),
            IdCentro = Ent(r, "id_centro"),
            Centro = Txt(r, "centro"),
            LimiteConfirmacion = FechaN(r, "limite_confirmacion")
        };

        /// <summary>Los turnos reservados que no se confirmaron a tiempo pasan a "Vencido" y el horario vuelve a quedar libre.</summary>
        public void VencerPendientes()
        {
            Ejecutar(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                DECLARE @v TABLE (id_turno INT, id_agenda INT, fecha_hora DATETIME, id_paciente INT);
                UPDATE TURNO SET estado = 'Vencido'
                OUTPUT inserted.id_turno, inserted.id_agenda, inserted.fecha_hora, inserted.id_paciente INTO @v
                WHERE estado = 'PendienteConfirmacion' AND limite_confirmacion IS NOT NULL AND limite_confirmacion < GETDATE();
                INSERT INTO HISTORIAL_TURNO (id_turno, id_paciente, estado_anterior, estado_nuevo, motivo)
                    SELECT id_turno, id_paciente, 'PendienteConfirmacion', 'Vencido', 'No se confirmó a tiempo' FROM @v;
                INSERT INTO TURNO (id_agenda, fecha_hora, estado)
                    SELECT id_agenda, fecha_hora, 'Disponible' FROM @v WHERE fecha_hora > GETDATE();
                COMMIT;");
        }

        public TurnoVista? Obtener(int idTurno) => Uno(Seleccion + " WHERE t.id_turno = @id", Mapear, ("@id", idTurno));

        public List<TurnoVista> DelDia(DateTime dia, string? texto, int? idMedico, int? idEspecialidad, string? estado)
        {
            return Lista(Seleccion + @"
                WHERE t.fecha_hora >= @d AND t.fecha_hora < DATEADD(DAY, 1, @d) AND t.id_paciente IS NOT NULL
                  AND t.estado <> 'Vencido'
                  AND (@m IS NULL OR a.id_medico = @m) AND (@e IS NULL OR a.id_especialidad = @e)
                  AND (@s IS NULL OR t.estado = @s)
                  AND (@t IS NULL OR pp.apellido LIKE @t OR pp.nombre LIKE @t OR pp.dni LIKE @t)
                ORDER BY t.fecha_hora, pp.apellido",
                Mapear, ("@d", dia.Date), ("@m", idMedico), ("@e", idEspecialidad), ("@s", estado),
                ("@t", string.IsNullOrWhiteSpace(texto) ? null : "%" + texto.Trim() + "%"));
        }

        public List<TurnoVista> DelMedicoEnDia(int idMedico, DateTime dia)
        {
            return Lista(Seleccion + @"
                WHERE a.id_medico = @m AND t.fecha_hora >= @d AND t.fecha_hora < DATEADD(DAY, 1, @d)
                  AND t.id_paciente IS NOT NULL AND t.estado NOT IN ('Vencido', 'Cancelado')
                ORDER BY t.fecha_hora, pp.apellido", Mapear, ("@m", idMedico), ("@d", dia.Date));
        }

        public HashSet<DateTime> DiasConTurnos(DateTime mes, int? idMedico)
        {
            var ini = new DateTime(mes.Year, mes.Month, 1);
            var l = Lista(@"SELECT DISTINCT CAST(t.fecha_hora AS DATE) AS dia FROM TURNO t JOIN AGENDA a ON a.id_agenda = t.id_agenda
                            WHERE t.fecha_hora >= @i AND t.fecha_hora < DATEADD(MONTH, 1, @i) AND t.id_paciente IS NOT NULL
                              AND t.estado NOT IN ('Vencido', 'Cancelado') AND (@m IS NULL OR a.id_medico = @m)",
                r => Fecha(r, "dia").Date, ("@i", ini), ("@m", idMedico));
            return new HashSet<DateTime>(l);
        }

        /// <summary>Turnos de un paciente titular y de sus familiares.</summary>
        public List<TurnoVista> DeGrupoFamiliar(int idTitular)
        {
            return Lista(Seleccion + @"
                JOIN PACIENTE pac ON pac.id_paciente = t.id_paciente
                WHERE t.id_paciente IS NOT NULL AND (t.id_paciente = @t OR pac.id_titular = @t)
                ORDER BY t.fecha_hora DESC", Mapear, ("@t", idTitular));
        }

        public List<TurnoVista> DePaciente(int idPaciente)
        {
            return Lista(Seleccion + " WHERE t.id_paciente = @p ORDER BY t.fecha_hora DESC", Mapear, ("@p", idPaciente));
        }

        public bool TieneTurnoFuturo(int idPaciente) =>
            Existe(@"SELECT COUNT(*) FROM TURNO WHERE id_paciente = @p AND fecha_hora > GETDATE() AND estado IN ('PendienteConfirmacion','Confirmado')", ("@p", idPaciente));

        public bool TieneTurnoActivoEnEspecialidad(int idPaciente, int idEspecialidad) =>
            Existe(@"SELECT COUNT(*) FROM TURNO t JOIN AGENDA a ON a.id_agenda = t.id_agenda
                     WHERE t.id_paciente = @p AND a.id_especialidad = @e AND t.fecha_hora > GETDATE()
                       AND t.estado IN ('PendienteConfirmacion','Confirmado')", ("@p", idPaciente), ("@e", idEspecialidad));

        public List<SlotDisponible> Disponibles(int idCentro, int idEspecialidad, DateTime desde, DateTime hasta)
        {
            return Lista(@"
                SELECT a.id_agenda, t.fecha_hora, COUNT(*) AS cupos, a.id_medico, pm.apellido AS ap_med, pm.nombre AS no_med,
                       e.descripcion AS esp, c.nombre AS centro
                FROM TURNO t
                JOIN AGENDA a ON a.id_agenda = t.id_agenda
                JOIN PERSONA pm ON pm.id_persona = a.id_medico
                JOIN CENTRO_SALUD c ON c.id_centro = a.id_centro
                LEFT JOIN ESPECIALIDAD e ON e.id_especialidad = a.id_especialidad
                WHERE t.estado = 'Disponible' AND t.fecha_hora > GETDATE() AND a.id_centro = @c AND a.id_especialidad = @e
                  AND t.fecha_hora >= @d AND t.fecha_hora < @h AND pm.activo = 1
                GROUP BY a.id_agenda, t.fecha_hora, a.id_medico, pm.apellido, pm.nombre, e.descripcion, c.nombre
                ORDER BY t.fecha_hora, pm.apellido",
                r => new SlotDisponible
                {
                    IdAgenda = Ent(r, "id_agenda"),
                    FechaHora = Fecha(r, "fecha_hora"),
                    Cupos = Ent(r, "cupos"),
                    IdMedico = Ent(r, "id_medico"),
                    Profesional = Txt(r, "ap_med") + ", " + Txt(r, "no_med"),
                    Especialidad = Txt(r, "esp"),
                    Centro = Txt(r, "centro")
                },
                ("@c", idCentro), ("@e", idEspecialidad), ("@d", desde), ("@h", hasta));
        }

        /// <summary>Reserva atómica de un cupo. Devuelve el id del turno o 0 si el horario ya no estaba libre.</summary>
        public int Reservar(int idAgenda, DateTime fechaHora, int idPaciente)
        {
            return Numero(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                DECLARE @t TABLE (id INT, estado VARCHAR(30));
                DECLARE @ahora DATETIME = GETDATE();
                UPDATE TOP (1) TURNO
                   SET id_paciente = @p,
                       estado = CASE WHEN fecha_hora <= DATEADD(HOUR, 24, @ahora) THEN 'Confirmado' ELSE 'PendienteConfirmacion' END,
                       fecha_reserva = @ahora,
                       limite_confirmacion = DATEADD(HOUR, -24, fecha_hora)
                OUTPUT inserted.id_turno, inserted.estado INTO @t
                WHERE id_agenda = @a AND fecha_hora = @f AND estado = 'Disponible' AND fecha_hora > @ahora;
                INSERT INTO HISTORIAL_TURNO (id_turno, id_paciente, estado_anterior, estado_nuevo, motivo)
                    SELECT id, @p, 'Disponible', estado, 'Reserva' FROM @t;
                COMMIT;
                SELECT ISNULL(MAX(id), 0) FROM @t;",
                ("@a", idAgenda), ("@f", fechaHora), ("@p", idPaciente));
        }

        /// <summary>Cambia el estado si el turno está en alguno de los estados de origen. Devuelve true si cambió.</summary>
        public bool CambiarEstado(int idTurno, string nuevo, string[] desde, string motivo, bool liberarHorario)
        {
            string lista = string.Join(",", desde.Select(d => "'" + d.Replace("'", "") + "'"));
            string liberar = liberarHorario
                ? "INSERT INTO TURNO (id_agenda, fecha_hora, estado) SELECT id_agenda, fecha_hora, 'Disponible' FROM @t WHERE fecha_hora > GETDATE();"
                : "";
            int n = Numero($@"
                SET XACT_ABORT ON; BEGIN TRAN;
                DECLARE @t TABLE (id_agenda INT, fecha_hora DATETIME, id_paciente INT, anterior VARCHAR(30));
                UPDATE TURNO SET estado = @nuevo
                OUTPUT inserted.id_agenda, inserted.fecha_hora, inserted.id_paciente, deleted.estado INTO @t
                WHERE id_turno = @id AND estado IN ({lista});
                INSERT INTO HISTORIAL_TURNO (id_turno, id_paciente, estado_anterior, estado_nuevo, motivo)
                    SELECT @id, id_paciente, anterior, @nuevo, @motivo FROM @t;
                {liberar}
                COMMIT;
                SELECT COUNT(*) FROM @t;",
                ("@id", idTurno), ("@nuevo", nuevo), ("@motivo", motivo));
            return n > 0;
        }

    }
}
