using AccesoDatos;
using Microsoft.Data.SqlClient;
using Xunit;

namespace TurnoTigre.Tests
{
    /// <summary>
    /// Pruebas de integración de RF5 contra una base SQL Server APARTE de la de desarrollo.
    /// Se activan definiendo TURNOTIGRE_TEST_CONN (la base tiene que llamarse TurnoTigreTest: cada prueba la borra y la rearma).
    /// </summary>
    public class FactoConBaseAttribute : FactAttribute
    {
        public const string Variable = "TURNOTIGRE_TEST_CONN";
        public FactoConBaseAttribute()
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(Variable)))
                Skip = $"Definí {Variable} apuntando a la base TurnoTigreTest.";
        }
    }

    [Collection("BaseDeDatos")]
    public class ReasignacionTests
    {
        private readonly ListaEsperaDatos datos = new ListaEsperaDatos();
        private int esp, centro, agenda;
        private int contador;

        public ReasignacionTests()
        {
            var cadena = Environment.GetEnvironmentVariable(FactoConBaseAttribute.Variable);
            if (string.IsNullOrEmpty(cadena)) return;
            var db = new SqlConnectionStringBuilder(cadena).InitialCatalog;
            if (db != "TurnoTigreTest") throw new InvalidOperationException($"Estas pruebas borran datos: solo corren sobre TurnoTigreTest (no sobre '{db}').");
            ConexionDB.CadenaConexionDefault = cadena;
            Limpiar();
            Armar();
        }

        // ---------- armado de datos ----------

        private static void Limpiar() => Sql.Ejecutar(@"
            DELETE FROM NOTIFICACION; DELETE FROM HISTORIAL_TURNO; DELETE FROM LISTA_ESPERA;
            DELETE FROM CONSULTA_ARCHIVO; DELETE FROM CONSULTA; DELETE FROM HISTORIA_CLINICA; DELETE FROM RECUPERACION_PASSWORD;
            DELETE FROM TURNO; DELETE FROM AGENDA_FECHA; DELETE FROM AGENDA;
            DELETE FROM MEDICO_ESPECIALIDAD; DELETE FROM MEDICO;
            UPDATE PACIENTE SET id_titular = NULL; DELETE FROM PACIENTE;
            DELETE FROM PERSONA; DELETE FROM USUARIO; DELETE FROM CENTRO_SALUD; DELETE FROM ESPECIALIDAD;");

        private void Armar()
        {
            esp = Sql.Numero("INSERT INTO ESPECIALIDAD (codigo, descripcion) VALUES ('CLI', 'Clínica'); SELECT CAST(SCOPE_IDENTITY() AS INT)");
            centro = CrearCentro("C1");
            int medico = Persona("Medico", "Casas", conUsuario: false);
            Sql.Ejecutar("INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@m, 'MP1', @e)", ("@m", medico), ("@e", esp));
            agenda = CrearAgenda(medico, centro, esp);
        }

        private int CrearCentro(string codigo) =>
            Sql.Numero("INSERT INTO CENTRO_SALUD (codigo, nombre, direccion, localidad) VALUES (@c, @c, 'Calle 1', 'Tigre'); SELECT CAST(SCOPE_IDENTITY() AS INT)", ("@c", codigo));

        private static int CrearAgenda(int medico, int centro, int esp) =>
            Sql.Numero(@"INSERT INTO AGENDA (id_medico, id_centro, vigencia_desde, vigencia_hasta, duracion_minutos, pacientes_por_franja, id_especialidad)
                         VALUES (@m, @c, DATEADD(DAY, -1, GETDATE()), DATEADD(YEAR, 1, GETDATE()), 30, 1, @e); SELECT CAST(SCOPE_IDENTITY() AS INT)",
                ("@m", medico), ("@c", centro), ("@e", esp));

        private int Persona(string nombre, string apellido, bool conUsuario)
        {
            int n = ++contador;
            object? idUsuario = null;
            if (conUsuario)
                idUsuario = Sql.Numero(@"INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol) VALUES (@u, 'x', @m, 'Paciente'); SELECT CAST(SCOPE_IDENTITY() AS INT)",
                    ("@u", "u" + n + nombre), ("@m", "u" + n + "@test.com"));
            return Sql.Numero(@"INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono) VALUES (@u, @d, @n, @a, '1111'); SELECT CAST(SCOPE_IDENTITY() AS INT)",
                ("@u", idUsuario), ("@d", (30000000 + n).ToString()), ("@n", nombre), ("@a", apellido));
        }

        private int Paciente(string nombre, bool conUsuario = true, int? idTitular = null)
        {
            int id = Persona(nombre, "Test", conUsuario);
            Sql.Ejecutar("INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad, id_titular) VALUES (@p, '1990-01-01', 'F', 'Calle', '1', 'Tigre', @t)",
                ("@p", id), ("@t", idTitular));
            return id;
        }

        /// <summary>Hora de cita redondeada al minuto, para que viaje igual a la base.</summary>
        private static DateTime AhoraDb() => Sql.Lista("SELECT GETDATE()", r => r.GetDateTime(0)).Single();   // puede diferir del reloj de la app (p. ej. Docker en UTC)

        private static DateTime Cita(double horasDesdeAhora)
        {
            var c = AhoraDb().AddHours(horasDesdeAhora);
            return new DateTime(c.Year, c.Month, c.Day, c.Hour, c.Minute, 0);
        }

        private int Turno(DateTime fecha, string estado = "Disponible", int? idPaciente = null, int? idAgenda = null) =>
            Sql.Numero("INSERT INTO TURNO (id_agenda, id_paciente, fecha_hora, estado) VALUES (@a, @p, @f, @e); SELECT CAST(SCOPE_IDENTITY() AS INT)",
                ("@a", idAgenda ?? agenda), ("@p", idPaciente), ("@f", fecha), ("@e", estado));

        private int Espera(int idPaciente, string franja = "Indistinto", double diasAtras = 0, double? vencimientoEnDias = 30, int? idEsp = null, int? idCentro = null) =>
            Sql.Numero(@"INSERT INTO LISTA_ESPERA (id_paciente, id_especialidad, id_centro, franja, fecha_inscripcion, fecha_vencimiento)
                         VALUES (@p, @e, @c, @fr, DATEADD(MINUTE, @min, GETDATE()), DATEADD(MINUTE, @venc, GETDATE())); SELECT CAST(SCOPE_IDENTITY() AS INT)",
                ("@p", idPaciente), ("@e", idEsp ?? esp), ("@c", idCentro ?? centro), ("@fr", franja),
                ("@min", (int)(-diasAtras * 24 * 60)), ("@venc", (int)((vencimientoEnDias ?? 0) * 24 * 60)));

        // ---------- lecturas ----------

        private static string EstadoTurno(int idTurno) => Sql.Lista("SELECT estado FROM TURNO WHERE id_turno = @t", r => r.GetString(0), ("@t", idTurno)).Single();
        private static int? PacienteDeTurno(int idTurno) => Sql.Lista("SELECT id_paciente FROM TURNO WHERE id_turno = @t", r => r.IsDBNull(0) ? (int?)null : r.GetInt32(0), ("@t", idTurno)).Single();
        private static DateTime Limite(int idTurno) => Sql.Lista("SELECT limite_confirmacion FROM TURNO WHERE id_turno = @t", r => r.GetDateTime(0), ("@t", idTurno)).Single();
        private static bool EsperaActiva(int idEspera) => Sql.Numero("SELECT COUNT(*) FROM LISTA_ESPERA WHERE id_espera = @e AND activa = 1", ("@e", idEspera)) == 1;
        private static int Notificaciones(int idUsuario) => Sql.Numero("SELECT COUNT(*) FROM NOTIFICACION WHERE id_usuario = @u AND tipo_notificacion = 'TurnoReasignado'", ("@u", idUsuario));
        private static int UsuarioDe(int idPersona) => Sql.Numero("SELECT id_usuario FROM PERSONA WHERE id_persona = @p", ("@p", idPersona));

        // ---------- pruebas ----------

        [FactoConBase]
        public void AsignaAlInscriptoMasAntiguo()
        {
            int nuevo = Paciente("Nuevo"), viejo = Paciente("Viejo");
            int eNuevo = Espera(nuevo, diasAtras: 1), eViejo = Espera(viejo, diasAtras: 5);
            var fecha = Cita(48); int turno = Turno(fecha);

            int r = datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(turno, r);
            Assert.Equal(viejo, PacienteDeTurno(turno));
            Assert.Equal("PendienteConfirmacion", EstadoTurno(turno));
            Assert.False(EsperaActiva(eViejo));
            Assert.True(EsperaActiva(eNuevo));
        }

        [FactoConBase]
        public void RegistraHistorialYNotificacion()
        {
            int pac = Paciente("Ana");
            Espera(pac);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(1, Notificaciones(UsuarioDe(pac)));
            Assert.Equal(1, Sql.Numero("SELECT COUNT(*) FROM HISTORIAL_TURNO WHERE id_turno = @t AND estado_nuevo = 'PendienteConfirmacion'", ("@t", turno)));
            var msg = Sql.Lista("SELECT mensaje FROM NOTIFICACION WHERE id_turno = @t", r => r.GetString(0), ("@t", turno)).Single();
            Assert.Contains("Clínica", msg);
        }

        [FactoConBase]
        public void ExcluyeAlPacienteQuePerdioElTurno()
        {
            int perdio = Paciente("Perdio"), otro = Paciente("Otro");
            Espera(perdio, diasAtras: 10); Espera(otro, diasAtras: 1);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, perdio);

            Assert.Equal(otro, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void RespetaLaFranja()
        {
            int tarde = Paciente("Tarde"), manana = Paciente("Manana");
            Espera(tarde, "Tarde", diasAtras: 10); Espera(manana, "Mañana", diasAtras: 1);
            var hoy = AhoraDb().Date.AddDays(2);
            var fecha = hoy.AddHours(9);                 // turno de mañana
            int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(manana, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void IndistintoSirveParaTarde()
        {
            int ind = Paciente("Ind");
            Espera(ind, "Indistinto");
            var fecha = AhoraDb().Date.AddDays(2).AddHours(15);
            int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(ind, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void SaltaInscripcionesVencidas()
        {
            int vencido = Paciente("Vencido"), vigente = Paciente("Vigente");
            Espera(vencido, diasAtras: 10, vencimientoEnDias: -1); Espera(vigente, diasAtras: 1);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(vigente, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void SaltaAQuienYaTieneTurnoActivoEnLaEspecialidad()
        {
            int conTurno = Paciente("ConTurno"), libre = Paciente("Libre");
            Turno(Cita(100), "Confirmado", conTurno);
            Espera(conTurno, diasAtras: 10); Espera(libre, diasAtras: 1);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(libre, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void UnTurnoCanceladoNoImpideLaReasignacion()
        {
            int pac = Paciente("Pac");
            Turno(Cita(100), "Cancelado", pac);
            Espera(pac);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(pac, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void SaltaAQuienNoTieneUsuarioAlQueAvisar()
        {
            int sinUsuario = Paciente("SinUsuario", conUsuario: false), conUsuario = Paciente("ConUsuario");
            Espera(sinUsuario, diasAtras: 10); Espera(conUsuario, diasAtras: 1);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(conUsuario, PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void FamiliarSinLoginAvisaAlTitular()
        {
            int titular = Paciente("Titular");
            int hijo = Paciente("Hijo", conUsuario: false, idTitular: titular);
            Espera(hijo);
            var fecha = Cita(48); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(hijo, PacienteDeTurno(turno));
            Assert.Equal(1, Notificaciones(UsuarioDe(titular)));
        }

        [FactoConBase]
        public void NoMezclaCentrosNiEspecialidades()
        {
            int otroCentro = CrearCentro("C2");
            int otraEsp = Sql.Numero("INSERT INTO ESPECIALIDAD (codigo, descripcion) VALUES ('PED', 'Pediatría'); SELECT CAST(SCOPE_IDENTITY() AS INT)");
            int enOtroCentro = Paciente("OtroCentro"), enOtraEsp = Paciente("OtraEsp");
            Espera(enOtroCentro, idCentro: otroCentro, diasAtras: 10);
            Espera(enOtraEsp, idEsp: otraEsp, diasAtras: 9);
            var fecha = Cita(48); int turno = Turno(fecha);

            int r = datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(0, r);
            Assert.Equal("Disponible", EstadoTurno(turno));
        }

        [FactoConBase]
        public void SinCompatiblesElTurnoQuedaDisponible()
        {
            var fecha = Cita(48); int turno = Turno(fecha);

            Assert.Equal(0, datos.ReasignarCupo(agenda, fecha, 0));
            Assert.Equal("Disponible", EstadoTurno(turno));
            Assert.Null(PacienteDeTurno(turno));
        }

        [FactoConBase]
        public void SiFaltaMenosDeUnaHoraNoReasigna()
        {
            int pac = Paciente("Pac"); int e = Espera(pac);
            var fecha = Cita(0.5); int turno = Turno(fecha);

            Assert.Equal(0, datos.ReasignarCupo(agenda, fecha, 0));
            Assert.Equal("Disponible", EstadoTurno(turno));
            Assert.True(EsperaActiva(e));
        }

        [FactoConBase]
        public void NoReasignaUnTurnoQueNoEstaDisponible()
        {
            int pac = Paciente("Pac"), ocupante = Paciente("Ocupante"); int e = Espera(pac);
            var fecha = Cita(48); int turno = Turno(fecha, "Confirmado", ocupante);

            Assert.Equal(0, datos.ReasignarCupo(agenda, fecha, 0));
            Assert.Equal(ocupante, PacienteDeTurno(turno));
            Assert.True(EsperaActiva(e));
        }

        [FactoConBase]
        public void LimiteEsAhoraMas24HsSiLaCitaEstaLejos()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(72); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            var ahora = AhoraDb();
            Assert.InRange(Limite(turno), ahora.AddHours(24).AddMinutes(-2), ahora.AddHours(24).AddMinutes(2));
        }

        [FactoConBase]
        public void LimiteEsCitaMenosUnaHoraSiLaCitaEstaCerca()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(10); int turno = Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(fecha.AddHours(-1), Limite(turno));
        }

        [FactoConBase]
        public void SoloSeReasignaUnaVez()
        {
            int a = Paciente("A"), b = Paciente("B");
            Espera(a, diasAtras: 5); Espera(b, diasAtras: 1);
            var fecha = Cita(48); int turno = Turno(fecha);

            Assert.Equal(turno, datos.ReasignarCupo(agenda, fecha, 0));
            Assert.Equal(0, datos.ReasignarCupo(agenda, fecha, 0));   // ya no está Disponible
            Assert.Equal(a, PacienteDeTurno(turno));
        }
    }
}
