using Dominio;
using System.Text;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class AgendaDatos
    {
        private const string Seleccion = @"
            SELECT a.id_agenda, a.id_medico, pm.apellido AS ap_med, pm.nombre AS no_med,
                   a.id_especialidad, e.descripcion AS esp, a.id_centro, c.nombre AS centro,
                   a.tipo, a.vigencia_desde, a.vigencia_hasta, a.hora_inicio, a.hora_fin, a.dias_semana,
                   a.duracion_minutos, a.pacientes_por_franja,
                   (SELECT COUNT(*) FROM TURNO t WHERE t.id_agenda = a.id_agenda) AS total,
                   (SELECT COUNT(*) FROM TURNO t WHERE t.id_agenda = a.id_agenda AND t.id_paciente IS NOT NULL
                       AND t.estado IN ('PendienteConfirmacion','Confirmado','Presente','EnAtencion','Atendido','Ausente')) AS asignados,
                   (SELECT COUNT(*) FROM AGENDA_FECHA f WHERE f.id_agenda = a.id_agenda) AS fechas
            FROM AGENDA a
            JOIN PERSONA pm ON pm.id_persona = a.id_medico
            JOIN CENTRO_SALUD c ON c.id_centro = a.id_centro
            LEFT JOIN ESPECIALIDAD e ON e.id_especialidad = a.id_especialidad ";

        private static AgendaVista Mapear(Microsoft.Data.SqlClient.SqlDataReader r) => new AgendaVista
        {
            IdAgenda = Ent(r, "id_agenda"),
            IdMedico = Ent(r, "id_medico"),
            Profesional = Txt(r, "ap_med") + ", " + Txt(r, "no_med"),
            IdEspecialidad = Ent(r, "id_especialidad"),
            Especialidad = Txt(r, "esp"),
            IdCentro = Ent(r, "id_centro"),
            Centro = Txt(r, "centro"),
            Tipo = Txt(r, "tipo"),
            VigenciaDesde = Fecha(r, "vigencia_desde"),
            VigenciaHasta = Fecha(r, "vigencia_hasta"),
            HoraInicio = Txt(r, "hora_inicio"),
            HoraFin = Txt(r, "hora_fin"),
            DiasSemana = Txt(r, "dias_semana"),
            DuracionMinutos = Ent(r, "duracion_minutos"),
            PacientesPorFranja = Ent(r, "pacientes_por_franja"),
            TotalTurnos = Ent(r, "total"),
            TurnosAsignados = Ent(r, "asignados"),
            CantFechas = Ent(r, "fechas")
        };

        public List<AgendaVista> Listar(string? texto = null, int? idMedico = null)
        {
            return Lista(Seleccion + @"
                WHERE (@m IS NULL OR a.id_medico = @m)
                  AND (@t IS NULL OR pm.apellido LIKE @t OR pm.nombre LIKE @t OR e.descripcion LIKE @t OR c.nombre LIKE @t)
                ORDER BY pm.apellido, pm.nombre, a.id_agenda",
                Mapear, ("@m", idMedico), ("@t", string.IsNullOrWhiteSpace(texto) ? null : "%" + texto.Trim() + "%"));
        }

        public AgendaVista? Obtener(int idAgenda)
        {
            var a = Uno(Seleccion + " WHERE a.id_agenda = @id", Mapear, ("@id", idAgenda));
            return a;
        }

        public List<FechaAgenda> Fechas(int idAgenda)
        {
            return Lista("SELECT fecha, hora_inicio, hora_fin FROM AGENDA_FECHA WHERE id_agenda = @id ORDER BY fecha",
                r => new FechaAgenda { Fecha = Fecha(r, "fecha"), HoraInicio = Txt(r, "hora_inicio"), HoraFin = Txt(r, "hora_fin") },
                ("@id", idAgenda));
        }

        /// <summary>Horarios que el profesional ya tiene ocupados por otras agendas (para evitar superposiciones).</summary>
        public HashSet<DateTime> HorariosOcupados(int idMedico, DateTime desde, DateTime hasta, int? excluirAgenda)
        {
            var l = Lista(@"SELECT DISTINCT t.fecha_hora FROM TURNO t JOIN AGENDA a ON a.id_agenda = t.id_agenda
                            WHERE a.id_medico = @m AND (@x IS NULL OR a.id_agenda <> @x)
                              AND t.fecha_hora >= @d AND t.fecha_hora <= @h AND t.estado NOT IN ('Cancelado','Vencido')",
                r => Fecha(r, "fecha_hora"), ("@m", idMedico), ("@x", excluirAgenda), ("@d", desde), ("@h", hasta));
            return new HashSet<DateTime>(l);
        }

        public int Crear(AgendaNueva a, List<DateTime> franjas, int? reemplazar)
        {
            var sb = new StringBuilder();
            sb.Append("SET XACT_ABORT ON; BEGIN TRAN;\n");
            if (reemplazar.HasValue)
                sb.Append(SqlEliminar("@reemplazar"));
            sb.Append(@"INSERT INTO AGENDA (id_medico, id_centro, id_especialidad, vigencia_desde, vigencia_hasta, duracion_minutos,
                                 pacientes_por_franja, tipo, hora_inicio, hora_fin, dias_semana)
                        VALUES (@medico, @centro, @esp, @desde, @hasta, @dur, @cupos, @tipo, @hi, @hf, @dias);
                        DECLARE @id INT = SCOPE_IDENTITY();" + "\n");

            if (a.Tipo == "PorFecha" && a.Fechas.Count > 0)
            {
                var filas = a.Fechas.Select(f => $"(@id, '{f.Fecha:yyyyMMdd}', '{f.HoraInicio}', '{f.HoraFin}')");
                sb.Append("INSERT INTO AGENDA_FECHA (id_agenda, fecha, hora_inicio, hora_fin) VALUES " + string.Join(",", filas) + ";\n");
            }

            // un registro de TURNO "Disponible" por cada cupo de cada franja
            var tuplas = new List<string>();
            foreach (var f in franjas)
                for (int i = 0; i < a.PacientesPorFranja; i++)
                    tuplas.Add($"(@id, '{Sql.F(f)}', 'Disponible')");
            for (int i = 0; i < tuplas.Count; i += 900)
                sb.Append("INSERT INTO TURNO (id_agenda, fecha_hora, estado) VALUES " + string.Join(",", tuplas.Skip(i).Take(900)) + ";\n");

            sb.Append("COMMIT;\nSELECT @id;");

            var datos = new ConexionDB();
            try
            {
                datos.setearConsulta(sb.ToString());
                datos.setearParametro("@reemplazar", reemplazar);
                datos.setearParametro("@medico", a.IdMedico);
                datos.setearParametro("@centro", a.IdCentro);
                datos.setearParametro("@esp", a.IdEspecialidad);
                datos.setearParametro("@desde", a.Desde.Date);
                datos.setearParametro("@hasta", a.Hasta.Date);
                datos.setearParametro("@dur", a.DuracionMinutos);
                datos.setearParametro("@cupos", a.PacientesPorFranja);
                datos.setearParametro("@tipo", a.Tipo);
                datos.setearParametro("@hi", a.HoraInicio);
                datos.setearParametro("@hf", a.HoraFin);
                datos.setearParametro("@dias", a.Tipo == "Mensual" ? string.Join(",", a.Dias) : null);
                return datos.obtenerId();
            }
            finally { datos.cerrarConexion(); }
        }

        private static string SqlEliminar(string param) => $@"
            DELETE FROM HISTORIAL_TURNO WHERE id_turno IN (SELECT id_turno FROM TURNO WHERE id_agenda = {param});
            DELETE FROM NOTIFICACION WHERE id_turno IN (SELECT id_turno FROM TURNO WHERE id_agenda = {param});
            DELETE FROM TURNO WHERE id_agenda = {param};
            DELETE FROM AGENDA_FECHA WHERE id_agenda = {param};
            DELETE FROM AGENDA WHERE id_agenda = {param};
            ";

        public void Eliminar(int idAgenda)
        {
            Ejecutar("SET XACT_ABORT ON; BEGIN TRAN;" + SqlEliminar("@id") + "COMMIT;", ("@id", idAgenda));
        }
    }
}
