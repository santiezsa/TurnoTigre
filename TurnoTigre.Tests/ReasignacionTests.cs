using AccesoDatos;
using Negocio;
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
        private int esp, centro, agenda, medico;
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
            DELETE FROM AUDITORIA_ACCESO; DELETE FROM NOTIFICACION; DELETE FROM HISTORIAL_TURNO; DELETE FROM LISTA_ESPERA;
            DELETE FROM CONSULTA_ARCHIVO; DELETE FROM CONSULTA; DELETE FROM HISTORIA_CLINICA; DELETE FROM RECUPERACION_PASSWORD;
            DELETE FROM TURNO; DELETE FROM AGENDA_FECHA; DELETE FROM AGENDA;
            DELETE FROM MEDICO_ESPECIALIDAD; DELETE FROM MEDICO;
            UPDATE PACIENTE SET id_titular = NULL; DELETE FROM PACIENTE;
            DELETE FROM PERSONA; DELETE FROM USUARIO; DELETE FROM CENTRO_SALUD; DELETE FROM ESPECIALIDAD;");

        private void Armar()
        {
            esp = Sql.Numero("INSERT INTO ESPECIALIDAD (codigo, descripcion) VALUES ('CLI', 'Clínica'); SELECT CAST(SCOPE_IDENTITY() AS INT)");
            centro = CrearCentro("C1");
            medico = Persona("Medico", "Casas", conUsuario: false);
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
        private static int Notificaciones(int idUsuario) => Sql.Numero("SELECT COUNT(*) FROM NOTIFICACION WHERE id_usuario = @u AND canal = 'App' AND tipo_notificacion = 'TurnoReasignado'", ("@u", idUsuario));
        private static int UsuarioDe(int idPersona) => Sql.Numero("SELECT id_usuario FROM PERSONA WHERE id_persona = @p", ("@p", idPersona));

        // ---------- recordatorio de confirmación (RF4) y motor de mantenimiento ----------

        private readonly TurnoDatos turnosDatos = new TurnoDatos();

        private int Reservado(int idPaciente, double horasParaLaCita, double horasParaElLimite)
        {
            var fecha = Cita(horasParaLaCita);
            int turno = Turno(fecha, "PendienteConfirmacion", idPaciente);
            Sql.Ejecutar("UPDATE TURNO SET limite_confirmacion = @l WHERE id_turno = @t", ("@l", Cita(horasParaElLimite)), ("@t", turno));
            return turno;
        }

        private static int Recordatorios(int idUsuario) => Sql.Numero("SELECT COUNT(*) FROM NOTIFICACION WHERE id_usuario = @u AND canal = 'App' AND tipo_notificacion = 'RecordatorioConfirmacion'", ("@u", idUsuario));

        private static int Correos(int idUsuario, string tipo, string estado = "Pendiente") =>
            Sql.Numero("SELECT COUNT(*) FROM NOTIFICACION WHERE id_usuario = @u AND canal = 'Email' AND tipo_notificacion = @t AND estado = @e", ("@u", idUsuario), ("@t", tipo), ("@e", estado));

        [FactoConBase]
        public void Recordatorio_SeGeneraUnaSolaVezCuandoSeAbreLaVentana()
        {
            int pac = Paciente("Pac");
            Reservado(pac, horasParaLaCita: 36, horasParaElLimite: 12);   // límite = cita - 24 hs: la ventana abrió hace 12 hs

            Assert.Equal(1, turnosDatos.GenerarRecordatoriosConfirmacion());
            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());
            Assert.Equal(1, Recordatorios(UsuarioDe(pac)));
        }

        [FactoConBase]
        public void Recordatorio_NoAvisaSiLaVentanaTodaviaNoAbrio()
        {
            int pac = Paciente("Pac");
            Reservado(pac, horasParaLaCita: 100, horasParaElLimite: 76);

            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());
        }

        [FactoConBase]
        public void Recordatorio_NoAvisaSiElPlazoYaVencio()
        {
            int pac = Paciente("Pac");
            Reservado(pac, horasParaLaCita: 30, horasParaElLimite: -1);

            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());
        }

        [FactoConBase]
        public void Recordatorio_NoAvisaSiYaEstaConfirmado()
        {
            int pac = Paciente("Pac");
            int turno = Reservado(pac, 36, 12);
            Sql.Ejecutar("UPDATE TURNO SET estado = 'Confirmado' WHERE id_turno = @t", ("@t", turno));

            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());
        }

        [FactoConBase]
        public void Recordatorio_NoSeRepiteEnUnTurnoReasignado()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);
            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());   // ya recibió TurnoReasignado
            Assert.Equal(1, Notificaciones(UsuarioDe(pac)));
        }

        [FactoConBase]
        public void Recordatorio_DeUnFamiliarSinLoginVaAlTitular()
        {
            int titular = Paciente("Titular");
            int hijo = Paciente("Hijo", conUsuario: false, idTitular: titular);
            Reservado(hijo, 36, 12);

            Assert.Equal(1, turnosDatos.GenerarRecordatoriosConfirmacion());
            Assert.Equal(1, Recordatorios(UsuarioDe(titular)));
        }

        [FactoConBase]
        public void Mantener_VenceElTurnoSinConfirmarYSeLoDaAlQueEspera()
        {
            int quePerdio = Paciente("Perdio"), espera = Paciente("Espera");
            Reservado(quePerdio, horasParaLaCita: 48, horasParaElLimite: -1);
            Espera(espera);

            var r = new Negocio.TurnoNegocio().Mantener();

            Assert.Equal(1, r.Vencidos);
            Assert.Equal(1, r.Reasignados);
            Assert.Equal(1, Sql.Numero("SELECT COUNT(*) FROM TURNO WHERE id_paciente = @p AND estado = 'PendienteConfirmacion'", ("@p", espera)));
            Assert.Equal(1, Sql.Numero("SELECT COUNT(*) FROM TURNO WHERE id_paciente = @p AND estado = 'Vencido'", ("@p", quePerdio)));
            Assert.Equal(1, Notificaciones(UsuarioDe(espera)));
        }

        [FactoConBase]
        public void Mantener_SinNadaPendienteNoHaceNada()
        {
            var r = new Negocio.TurnoNegocio().Mantener();

            Assert.False(r.HuboCambios);
        }

        // ---------- avisos por mail (cola de NOTIFICACION con canal Email) ----------

        [FactoConBase]
        public void Mail_LaReasignacionEncolaUnMailPendiente()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);

            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Equal(1, Correos(UsuarioDe(pac), "TurnoReasignado"));
            Assert.Equal(1, Notificaciones(UsuarioDe(pac)));   // y el aviso en la app sigue siendo uno solo
        }

        [FactoConBase]
        public void Mail_ElRecordatorioEncolaAppYMailUnaSolaVez()
        {
            int pac = Paciente("Pac");
            Reservado(pac, 36, 12);

            Assert.Equal(1, turnosDatos.GenerarRecordatoriosConfirmacion());
            Assert.Equal(0, turnosDatos.GenerarRecordatoriosConfirmacion());
            Assert.Equal(1, Recordatorios(UsuarioDe(pac)));
            Assert.Equal(1, Correos(UsuarioDe(pac), "RecordatorioConfirmacion"));
        }

        [FactoConBase]
        public void Mail_LaPantallaDeAvisosNoMuestraLosMails()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);
            datos.ReasignarCupo(agenda, fecha, 0);

            Assert.Single(datos.NotificacionesDeUsuario(UsuarioDe(pac)));
        }

        [FactoConBase]
        public void Mail_PendientesTraeElMailDelUsuarioYAlEnviarloSaleDeLaCola()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);
            datos.ReasignarCupo(agenda, fecha, 0);
            var negocio = new Negocio.NotificacionNegocio();

            var c = Assert.Single(negocio.CorreosPendientes());
            Assert.Equal(Sql.Lista("SELECT email FROM USUARIO WHERE id_usuario = @u", r => r.GetString(0), ("@u", UsuarioDe(pac))).Single(), c.Destino);
            Assert.Equal("TurnoReasignado", c.Tipo);

            negocio.MarcarCorreoEnviado(c.IdNotificacion);

            Assert.Empty(negocio.CorreosPendientes());
            Assert.Equal(1, Correos(UsuarioDe(pac), "TurnoReasignado", "Enviada"));
        }

        [FactoConBase]
        public void Mail_SeDescartaElQueYaVencio()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);
            datos.ReasignarCupo(agenda, fecha, 0);
            Sql.Ejecutar("UPDATE NOTIFICACION SET limite_respuesta = DATEADD(MINUTE, -1, GETDATE()) WHERE canal = 'Email'");

            Assert.Empty(new Negocio.NotificacionNegocio().CorreosPendientes());
            Assert.Equal(1, Correos(UsuarioDe(pac), "TurnoReasignado", "Descartada"));
        }

        [FactoConBase]
        public void Mail_NoSeEnviaAUnUsuarioDeshabilitado()
        {
            int pac = Paciente("Pac"); Espera(pac);
            var fecha = Cita(48); Turno(fecha);
            datos.ReasignarCupo(agenda, fecha, 0);
            Sql.Ejecutar("UPDATE USUARIO SET activo = 0 WHERE id_usuario = @u", ("@u", UsuarioDe(pac)));

            Assert.Empty(new Negocio.NotificacionNegocio().CorreosPendientes());
        }

        // ---------- revalidación de la sesión ----------

        [FactoConBase]
        public void Sesion_SeInvalidaSiElUsuarioSeDeshabilitaOCambiaDeRol()
        {
            int usuario = UsuarioDe(Paciente("Pac"));
            var negocio = new Negocio.UsuarioNegocio();

            Assert.True(negocio.SesionVigente(usuario, "Paciente"));
            Assert.False(negocio.SesionVigente(usuario, "Medico"));         // su rol cambió
            Assert.False(negocio.SesionVigente(999999, "Paciente"));        // el usuario ya no existe

            Sql.Ejecutar("UPDATE USUARIO SET activo = 0 WHERE id_usuario = @u", ("@u", usuario));
            Assert.False(negocio.SesionVigente(usuario, "Paciente"));       // lo deshabilitaron
        }

        // ---------- acceso a la historia clínica y auditoría ----------

        private readonly HistoriaClinicaNegocio historias = new HistoriaClinicaNegocio();

        private bool MedicoAccede(int idPaciente, int? idMedico = null, string accion = HistoriaClinicaNegocio.AccionVer) =>
            historias.AutorizarAcceso(UsuarioDe(Paciente("Usuario" + (++contador))), Dominio.Roles.Medico, idMedico ?? medico, idPaciente, accion);

        private static int Registros(int idPaciente, string accion) =>
            Sql.Numero("SELECT COUNT(*) FROM AUDITORIA_ACCESO WHERE id_paciente = @p AND accion = @a", ("@p", idPaciente), ("@a", accion));

        [FactoConBase]
        public void Historia_MedicoSinRelacionNoAccede()
        {
            int pac = Paciente("Pac");

            Assert.False(MedicoAccede(pac));
        }

        [FactoConBase]
        public void Historia_MedicoConTurnoQueVaAOcurrirOYaOcurrioAccede()
        {
            foreach (var estado in new[] { "Confirmado", "Presente", "EnAtencion", "Atendido" })
            {
                int pac = Paciente("Pac" + estado);
                Turno(Cita(48), estado, pac);

                Assert.True(MedicoAccede(pac), estado);
            }
        }

        [FactoConBase]
        public void Historia_UnTurnoSinConfirmarCanceladoVencidoOAusenteNoDaAcceso()
        {
            foreach (var estado in new[] { "PendienteConfirmacion", "Cancelado", "Vencido", "Ausente" })
            {
                int pac = Paciente("Pac" + estado);
                Turno(Cita(48), estado, pac);

                Assert.False(MedicoAccede(pac), estado);
            }
        }

        [FactoConBase]
        public void Historia_MedicoQueYaRegistroUnaConsultaSigueAccediendo()
        {
            int pac = Paciente("Pac");
            Sql.Ejecutar("INSERT INTO CONSULTA (id_paciente, id_medico, observaciones, diagnostico) VALUES (@p, @m, 'obs', 'diag')", ("@p", pac), ("@m", medico));

            Assert.True(MedicoAccede(pac));
        }

        [FactoConBase]
        public void Historia_ElTurnoDeOtroMedicoNoCuenta()
        {
            int pac = Paciente("Pac");
            int otroMedico = Persona("Otro", "Medico", conUsuario: false);
            Turno(Cita(48), "Confirmado", pac);

            Assert.True(MedicoAccede(pac));                         // el médico del turno
            Assert.False(MedicoAccede(pac, idMedico: otroMedico));  // otro médico, aunque el paciente tenga turno en el centro
        }

        [FactoConBase]
        public void Historia_ElFamiliarTieneSuPropiaRelacionDeAtencion()
        {
            int titular = Paciente("Titular");
            int hijo = Paciente("Hijo", conUsuario: false, idTitular: titular);
            Turno(Cita(48), "Confirmado", titular);

            Assert.True(MedicoAccede(titular));
            Assert.False(MedicoAccede(hijo));     // atender al titular no habilita la historia de sus familiares
        }

        [FactoConBase]
        public void Historia_SuperAdminAccedeSiempre()
        {
            int pac = Paciente("Pac");

            Assert.True(historias.AutorizarAcceso(UsuarioDe(pac), Dominio.Roles.SuperAdmin, null, pac, HistoriaClinicaNegocio.AccionVer));
        }

        [FactoConBase]
        public void Historia_AdministrativoYPacienteNoAccedenPorEstaVia()
        {
            int pac = Paciente("Pac");

            Assert.False(historias.AutorizarAcceso(UsuarioDe(pac), Dominio.Roles.Administrativo, null, pac, HistoriaClinicaNegocio.AccionVer));
            Assert.False(historias.AutorizarAcceso(UsuarioDe(pac), Dominio.Roles.Paciente, null, pac, HistoriaClinicaNegocio.AccionVer));
        }

        [FactoConBase]
        public void Auditoria_RegistraLosAccesosPermitidosYLosDenegados()
        {
            int pac = Paciente("Pac");
            Turno(Cita(48), "Confirmado", pac);
            int sinRelacion = Paciente("Otro");

            MedicoAccede(pac, accion: HistoriaClinicaNegocio.AccionVer);
            MedicoAccede(pac, accion: HistoriaClinicaNegocio.AccionGuardar);
            MedicoAccede(sinRelacion, accion: HistoriaClinicaNegocio.AccionDescargar);

            Assert.Equal(1, Registros(pac, HistoriaClinicaNegocio.AccionVer));
            Assert.Equal(1, Registros(pac, HistoriaClinicaNegocio.AccionGuardar));
            Assert.Equal(0, Registros(sinRelacion, HistoriaClinicaNegocio.AccionDescargar));
            Assert.Equal(1, Registros(sinRelacion, HistoriaClinicaNegocio.AccionDenegado));
            var detalle = Sql.Lista("SELECT detalle FROM AUDITORIA_ACCESO WHERE id_paciente = @p", r => r.GetString(0), ("@p", sinRelacion)).Single();
            Assert.Contains("DescargarArchivo", detalle);   // el acceso denegado conserva qué se intentó hacer
        }

        [FactoConBase]
        public void Auditoria_GuardaUsuarioRolYFecha()
        {
            int pac = Paciente("Pac");
            int usuario = UsuarioDe(pac);

            historias.RegistrarAcceso(usuario, Dominio.Roles.Paciente, pac, HistoriaClinicaNegocio.AccionDescargar, "archivo 7");

            var fila = Sql.Lista("SELECT id_usuario, rol, detalle, fecha FROM AUDITORIA_ACCESO WHERE id_paciente = @p",
                r => (r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDateTime(3)), ("@p", pac)).Single();
            Assert.Equal((usuario, "Paciente", "archivo 7"), (fila.Item1, fila.Item2, fila.Item3));
            Assert.InRange(fila.Item4, AhoraDb().AddMinutes(-1), AhoraDb().AddMinutes(1));
        }

        // ---------- pantalla de auditoría (consulta con filtros) ----------

        private readonly AuditoriaNegocio auditoria = new AuditoriaNegocio();

        private void Acceso(int idUsuario, string rol, int idPaciente, string accion = HistoriaClinicaNegocio.AccionVer, string? detalle = null) =>
            historias.RegistrarAcceso(idUsuario, rol, idPaciente, accion, detalle);

        [FactoConBase]
        public void Consulta_SinFiltrosTraeTodoDelMasRecienteAlMasAntiguo()
        {
            int pac = Paciente("Zulema"), medico1 = Paciente("Alfa");
            Acceso(UsuarioDe(medico1), "Medico", pac, HistoriaClinicaNegocio.AccionVer);
            Acceso(UsuarioDe(medico1), "Medico", pac, HistoriaClinicaNegocio.AccionGuardar);

            var r = auditoria.Buscar(null, null, null, null, null, 1);

            Assert.Equal(2, r.Total);
            Assert.Equal(new[] { HistoriaClinicaNegocio.AccionGuardar, HistoriaClinicaNegocio.AccionVer }, r.Registros.Select(x => x.Accion).ToArray());
            Assert.Equal("Test, Zulema", r.Registros[0].Paciente);
        }

        [FactoConBase]
        public void Consulta_FiltraPorPacienteConNombreApellidoDniONombreCompleto()
        {
            int zulema = Paciente("Zulema"), otro = Paciente("Otro"), quien = Paciente("Alfa");
            Acceso(UsuarioDe(quien), "Medico", zulema);
            Acceso(UsuarioDe(quien), "Medico", otro);
            string dni = Sql.Lista("SELECT dni FROM PERSONA WHERE id_persona = @p", r => r.GetString(0), ("@p", zulema)).Single();

            foreach (var texto in new[] { "Zulema", "zule", dni, "Zulema Test" })
            {
                var r = auditoria.Buscar(texto, null, null, null, null, 1);
                Assert.True(r.Total == 1 && r.Registros[0].IdPaciente == zulema, $"paciente '{texto}'");
            }
        }

        [FactoConBase]
        public void Consulta_FiltraPorQuienAccedio()
        {
            int pac = Paciente("Zulema"), alfa = Paciente("Alfa"), beta = Paciente("Beta");
            Acceso(UsuarioDe(alfa), "Medico", pac);
            Acceso(UsuarioDe(beta), "Medico", pac);

            var r = auditoria.Buscar(null, "Alfa", null, null, null, 1);

            Assert.Equal(1, r.Total);
            Assert.Equal("Test, Alfa", r.Registros[0].NombreUsuario);
            Assert.Equal("Medico", r.Registros[0].Rol);
        }

        [FactoConBase]
        public void Consulta_FiltraPorAccionEIgnoraUnaAccionDesconocida()
        {
            int pac = Paciente("Zulema"), quien = Paciente("Alfa");
            Acceso(UsuarioDe(quien), "Medico", pac, HistoriaClinicaNegocio.AccionVer);
            Acceso(UsuarioDe(quien), "Medico", pac, HistoriaClinicaNegocio.AccionDenegado, "VerHistoria");

            Assert.Equal(1, auditoria.Buscar(null, null, HistoriaClinicaNegocio.AccionDenegado, null, null, 1).Total);
            Assert.Equal(2, auditoria.Buscar(null, null, "ConsultaInventada", null, null, 1).Total);
        }

        [FactoConBase]
        public void Consulta_FiltraPorFechasYHastaIncluyeElDiaCompleto()
        {
            int pac = Paciente("Zulema"), quien = Paciente("Alfa");
            Acceso(UsuarioDe(quien), "Medico", pac);                      // este se manda 10 días atrás
            Acceso(UsuarioDe(quien), "Medico", pac);                      // este queda con la fecha de hoy
            Sql.Ejecutar("UPDATE AUDITORIA_ACCESO SET fecha = DATEADD(DAY, -10, fecha) WHERE id_auditoria = (SELECT MIN(id_auditoria) FROM AUDITORIA_ACCESO)");
            var hoy = AhoraDb().Date;

            Assert.Equal(1, auditoria.Buscar(null, null, null, hoy.AddDays(-2), null, 1).Total);   // desde hace 2 días: solo el de hoy
            Assert.Equal(1, auditoria.Buscar(null, null, null, null, hoy.AddDays(-5), 1).Total);   // hasta hace 5 días: solo el viejo
            Assert.Equal(2, auditoria.Buscar(null, null, null, null, hoy, 1).Total);               // hasta hoy incluye lo de hoy a cualquier hora
        }

        [FactoConBase]
        public void Consulta_PaginaDe25Registros()
        {
            int pac = Paciente("Zulema"), quien = Paciente("Alfa");
            for (int i = 0; i < 30; i++) Acceso(UsuarioDe(quien), "Medico", pac, HistoriaClinicaNegocio.AccionVer, "n" + i);

            var p1 = auditoria.Buscar(null, null, null, null, null, 1);
            var p2 = auditoria.Buscar(null, null, null, null, null, 2);
            var fuera = auditoria.Buscar(null, null, null, null, null, 99);

            Assert.Equal((30, 2, 25), (p1.Total, p1.TotalPaginas, p1.Registros.Count));
            Assert.Equal(5, p2.Registros.Count);
            Assert.Equal(2, fuera.Pagina);                                   // una página inexistente se acomoda a la última
            Assert.Equal("n29", p1.Registros[0].Detalle);                   // el más reciente primero
            Assert.Empty(p1.Registros.Select(x => x.IdAuditoria).Intersect(p2.Registros.Select(x => x.IdAuditoria)));
        }

        [FactoConBase]
        public void Consulta_SinResultadosDevuelveUnaPaginaVacia()
        {
            var r = auditoria.Buscar("nadie", null, null, null, null, 1);

            Assert.Equal((0, 1, 1), (r.Total, r.TotalPaginas, r.Pagina));
            Assert.Empty(r.Registros);
        }

        // ---------- agenda nueva con lista de espera, y bloqueo de la inscripción si hay turnos libres ----------

        private readonly ListaEsperaNegocio lista = new ListaEsperaNegocio();

        private static int Inscriptos(int idPaciente) => Sql.Numero("SELECT COUNT(*) FROM LISTA_ESPERA WHERE id_paciente = @p AND activa = 1", ("@p", idPaciente));

        [FactoConBase]
        public void AgendaNueva_OfreceLosCuposALaListaDeEsperaPorAntiguedad()
        {
            int a = Paciente("A"), b = Paciente("B"), c = Paciente("C");
            Espera(a, diasAtras: 5); Espera(b, diasAtras: 3); int eC = Espera(c, diasAtras: 1);
            int t1 = Turno(Cita(48)), t2 = Turno(Cita(72));

            int asignados = lista.AsignarCuposLibres(agenda);

            Assert.Equal(2, asignados);
            Assert.Equal(a, PacienteDeTurno(t1));           // el horario más cercano, al que se anotó primero
            Assert.Equal(b, PacienteDeTurno(t2));
            Assert.True(EsperaActiva(eC));                  // no alcanzaron los cupos para el tercero
            Assert.Equal("PendienteConfirmacion", EstadoTurno(t1));
        }

        [FactoConBase]
        public void AgendaNueva_AsignaTodosLosCuposDeUnMismoHorario()
        {
            int a = Paciente("A"), b = Paciente("B"), c = Paciente("C");
            Espera(a, diasAtras: 5); Espera(b, diasAtras: 3); int eC = Espera(c, diasAtras: 1);
            var fecha = Cita(48);
            int t1 = Turno(fecha), t2 = Turno(fecha);       // pacientes_por_franja = 2

            Assert.Equal(2, lista.AsignarCuposLibres(agenda));

            var duenios = new[] { PacienteDeTurno(t1), PacienteDeTurno(t2) };
            Assert.Contains((int?)a, duenios);              // los dos más antiguos se reparten los dos cupos
            Assert.Contains((int?)b, duenios);
            Assert.True(EsperaActiva(eC));
        }

        [FactoConBase]
        public void AgendaNueva_SinInscriptosNoCambiaNada()
        {
            int t1 = Turno(Cita(48));

            Assert.Equal(0, lista.AsignarCuposLibres(agenda));
            Assert.Equal("Disponible", EstadoTurno(t1));
        }

        [FactoConBase]
        public void AgendaNueva_RespetaLaFranjaYNoOfreceCuposDeMenosDeUnaHora()
        {
            int tarde = Paciente("Tarde"), cualquiera = Paciente("Cualquiera");
            int eTarde = Espera(tarde, "Tarde", diasAtras: 5);
            int turnoManana = Turno(AhoraDb().Date.AddDays(2).AddHours(9));      // mañana: no le sirve a quien espera de tarde
            Espera(cualquiera, "Indistinto", diasAtras: 1);
            int turnoYa = Turno(Cita(0.5));                                      // queda menos de 1 hora: no se ofrece

            int asignados = lista.AsignarCuposLibres(agenda);

            Assert.Equal(1, asignados);                      // solo el de la mañana, y para quien acepta cualquier franja
            Assert.Equal(cualquiera, PacienteDeTurno(turnoManana));
            Assert.Equal("Disponible", EstadoTurno(turnoYa));
            Assert.True(EsperaActiva(eTarde));
        }

        [FactoConBase]
        public void AgendaNueva_AvisaALosAsignadosEnLaAppYPorMail()
        {
            int pac = Paciente("Pac"); Espera(pac);
            Turno(Cita(48));

            lista.AsignarCuposLibres(agenda);

            Assert.Equal(1, Notificaciones(UsuarioDe(pac)));
            Assert.Equal(1, Correos(UsuarioDe(pac), "TurnoReasignado"));
        }

        [FactoConBase]
        public void AgendaNueva_UnaAgendaQueNoExisteNoHaceNada()
        {
            Assert.Equal(0, lista.AsignarCuposLibres(999999));
        }

        [FactoConBase]
        public void Inscribir_NoDejaAnotarseSiHayTurnosLibres()
        {
            int pac = Paciente("Pac");
            Turno(Cita(48));

            var error = Assert.Throws<Exception>(() => lista.Inscribir(pac, pac, esp, centro, "Indistinto"));

            Assert.Contains("Hay turnos disponibles", error.Message);
            Assert.Equal(0, Inscriptos(pac));
        }

        [FactoConBase]
        public void Inscribir_PermiteAnotarseSiNoHayTurnosLibres()
        {
            int pac = Paciente("Pac");

            lista.Inscribir(pac, pac, esp, centro, "Indistinto");

            Assert.Equal(1, Inscriptos(pac));
        }

        [FactoConBase]
        public void Inscribir_SoloBloqueaSiLosTurnosLibresLeSirvenALaFranja()
        {
            int deTarde = Paciente("DeTarde"), deManana = Paciente("DeManana");
            Turno(AhoraDb().Date.AddDays(2).AddHours(9));        // único turno libre: de mañana

            lista.Inscribir(deTarde, deTarde, esp, centro, "Tarde");                                       // no le sirve: puede esperar
            var error = Assert.Throws<Exception>(() => lista.Inscribir(deManana, deManana, esp, centro, "Mañana"));   // sí le sirve: que lo reserve

            Assert.Equal(1, Inscriptos(deTarde));
            Assert.Contains("Hay turnos disponibles", error.Message);
        }

        [FactoConBase]
        public void Inscribir_LosTurnosLibresDeOtroCentroOUnTurnoOcupadoNoBloquean()
        {
            int pac = Paciente("Pac"), ocupante = Paciente("Ocupante");
            int otroCentro = CrearCentro("C2");
            int agendaOtroCentro = CrearAgenda(medico, otroCentro, esp);
            Turno(Cita(48), idAgenda: agendaOtroCentro);                       // libre, pero en otro centro
            Turno(Cita(72), "Confirmado", ocupante);                           // en este centro, pero ya tomado

            lista.Inscribir(pac, pac, esp, centro, "Indistinto");

            Assert.Equal(1, Inscriptos(pac));
        }

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
            var msg = Sql.Lista("SELECT mensaje FROM NOTIFICACION WHERE id_turno = @t AND canal = 'App'", r => r.GetString(0), ("@t", turno)).Single();
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
