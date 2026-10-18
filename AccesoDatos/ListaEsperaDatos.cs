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

        /// <summary>Avisa (notificación dentro del sistema) a quienes esperan un turno de esa especialidad y centro.</summary>
        public int NotificarCupoLiberado(int idAgenda, DateTime fechaHora)
        {
            var infoL = Lista("SELECT a.id_especialidad, a.id_centro, e.descripcion AS esp, c.nombre AS centro FROM AGENDA a " +
                           "JOIN CENTRO_SALUD c ON c.id_centro = a.id_centro LEFT JOIN ESPECIALIDAD e ON e.id_especialidad = a.id_especialidad WHERE a.id_agenda = @a",
                r => (Ent(r, "id_especialidad"), Ent(r, "id_centro"), Txt(r, "esp"), Txt(r, "centro")), ("@a", idAgenda));
            if (infoL.Count == 0) return 0;
            var (esp, centro, nomEsp, nomCentro) = infoL[0];
            string franja = fechaHora.Hour < 13 ? "Mañana" : "Tarde";
            string mensaje = $"Se liberó un turno de {nomEsp} en {nomCentro} el {fechaHora:dd/MM} a las {fechaHora:HH:mm}. Ingresá a Solicitar turno para reservarlo.";
            return Numero(@"
                DECLARE @n INT;
                INSERT INTO NOTIFICACION (id_usuario, canal, tipo_notificacion, mensaje, estado)
                SELECT DISTINCT COALESCE(pe.id_usuario, pt.id_usuario), 'App', 'CupoLiberado', @msg, 'Pendiente'
                FROM LISTA_ESPERA le
                JOIN PERSONA pe ON pe.id_persona = le.id_paciente
                JOIN PACIENTE pac ON pac.id_paciente = le.id_paciente
                LEFT JOIN PERSONA pt ON pt.id_persona = pac.id_titular
                WHERE le.activa = 1 AND le.id_especialidad = @e AND le.id_centro = @c AND le.franja IN ('Indistinto', @fr)
                  AND COALESCE(pe.id_usuario, pt.id_usuario) IS NOT NULL;
                SET @n = @@ROWCOUNT;
                SELECT @n;",
                ("@msg", mensaje), ("@e", esp), ("@c", centro), ("@fr", franja));
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
