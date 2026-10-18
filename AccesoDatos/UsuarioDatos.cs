using Dominio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccesoDatos
{
    public class UsuarioDatos
    {

        public bool ExisteEmail(string email)
        {
            ConexionDB datos = new ConexionDB();

            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM USUARIO WHERE email = @email");
                datos.setearParametro("@email", email);

                return datos.obtenerId() > 0;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public Usuario BuscarUsuarioPorEmail(string email)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"SELECT id_usuario, nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password
                                     FROM USUARIO
                                     WHERE email = @email");
                datos.setearParametro("@email", email);
                datos.ejecutarLectura();

                if (datos.Lector.Read())
                {
                    Usuario usuario = new Usuario();
                    usuario.IdUsuario = (int)datos.Lector["id_usuario"];
                    usuario.NombreUsuario = (string)datos.Lector["nombre_usuario"];
                    usuario.PasswordHash = (string)datos.Lector["password_hash"];
                    usuario.Email = (string)datos.Lector["email"];
                    usuario.Rol = (string)datos.Lector["rol"];
                    usuario.Activo = (bool)datos.Lector["activo"];
                    usuario.DebeCambiarPassword = (bool)datos.Lector["debe_cambiar_password"];

                    return usuario;
                }

                return null;

            }
            finally {
                datos.cerrarConexion();
            }
        }

        public Usuario? BuscarUsuarioPorNombre(string nombreUsuario)
        {
            return Sql.Uno(@"SELECT id_usuario, nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password
                             FROM USUARIO WHERE nombre_usuario = @n",
                r => new Usuario
                {
                    IdUsuario = Sql.Ent(r, "id_usuario"), NombreUsuario = Sql.Txt(r, "nombre_usuario"), PasswordHash = Sql.Txt(r, "password_hash"),
                    Email = Sql.Txt(r, "email"), Rol = Sql.Txt(r, "rol"), Activo = Sql.Bool(r, "activo"), DebeCambiarPassword = Sql.Bool(r, "debe_cambiar_password")
                }, ("@n", nombreUsuario));
        }

        public Usuario? BuscarUsuarioPorId(int idUsuario)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"SELECT id_usuario, nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password
                                     FROM USUARIO
                                     WHERE id_usuario = @id");
                datos.setearParametro("@id", idUsuario);
                datos.ejecutarLectura();

                if (datos.Lector.Read())
                {
                    return new Usuario
                    {
                        IdUsuario = (int)datos.Lector["id_usuario"],
                        NombreUsuario = (string)datos.Lector["nombre_usuario"],
                        PasswordHash = (string)datos.Lector["password_hash"],
                        Email = (string)datos.Lector["email"],
                        Rol = (string)datos.Lector["rol"],
                        Activo = (bool)datos.Lector["activo"],
                        DebeCambiarPassword = (bool)datos.Lector["debe_cambiar_password"]
                    };
                }
                return null;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public bool ExisteNombreUsuario(string nombreUsuario)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM USUARIO WHERE nombre_usuario = @nombreUsuario");
                datos.setearParametro("@nombreUsuario", nombreUsuario);
                return datos.obtenerId() > 0;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public bool ExisteEmailDeOtroUsuario(string email, int idUsuario)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM USUARIO WHERE email = @email AND id_usuario <> @id");
                datos.setearParametro("@email", email);
                datos.setearParametro("@id", idUsuario);
                return datos.obtenerId() > 0;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public bool ExisteDniPersona(string dni)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM PERSONA WHERE dni = @dni");
                datos.setearParametro("@dni", dni);
                return datos.obtenerId() > 0;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public bool ExisteMatricula(string matricula)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM MEDICO WHERE matricula = @matricula");
                datos.setearParametro("@matricula", matricula);
                return datos.obtenerId() > 0;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public int ContarSuperAdminsActivos()
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("SELECT COUNT(*) FROM USUARIO WHERE rol = 'SuperAdmin' AND activo = 1");
                return datos.obtenerId();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        // Lista usuarios con filtros opcionales (texto: nombre, apellido, DNI, usuario o email)
        public List<UsuarioListado> Listar(string? texto, string? rol, bool? activo)
        {
            ConexionDB datos = new ConexionDB();
            List<UsuarioListado> lista = new List<UsuarioListado>();
            try
            {
                datos.setearConsulta(@"
                    SELECT u.id_usuario, u.nombre_usuario, u.email, u.rol, u.activo, u.debe_cambiar_password,
                           ISNULL(p.nombre, '') AS nombre, ISNULL(p.apellido, '') AS apellido,
                           ISNULL(p.dni, '') AS dni, ISNULL(p.telefono, '') AS telefono
                    FROM USUARIO u
                    LEFT JOIN PERSONA p ON p.id_usuario = u.id_usuario
                    WHERE (@rol IS NULL OR u.rol = @rol)
                      AND (@activo IS NULL OR u.activo = @activo)
                      AND (@texto IS NULL
                           OR u.nombre_usuario LIKE '%' + @texto + '%'
                           OR u.email LIKE '%' + @texto + '%'
                           OR p.nombre LIKE '%' + @texto + '%'
                           OR p.apellido LIKE '%' + @texto + '%'
                           OR p.dni LIKE '%' + @texto + '%')
                    ORDER BY p.apellido, p.nombre, u.nombre_usuario");
                datos.setearParametro("@texto", string.IsNullOrWhiteSpace(texto) ? DBNull.Value : (object)texto.Trim());
                datos.setearParametro("@rol", string.IsNullOrWhiteSpace(rol) ? DBNull.Value : (object)rol);
                datos.setearParametro("@activo", activo.HasValue ? (object)activo.Value : DBNull.Value);
                datos.ejecutarLectura();

                while (datos.Lector.Read())
                {
                    lista.Add(new UsuarioListado
                    {
                        IdUsuario = (int)datos.Lector["id_usuario"],
                        NombreUsuario = (string)datos.Lector["nombre_usuario"],
                        Email = (string)datos.Lector["email"],
                        Rol = (string)datos.Lector["rol"],
                        Activo = (bool)datos.Lector["activo"],
                        DebeCambiarPassword = (bool)datos.Lector["debe_cambiar_password"],
                        Nombre = (string)datos.Lector["nombre"],
                        Apellido = (string)datos.Lector["apellido"],
                        Dni = (string)datos.Lector["dni"],
                        Telefono = (string)datos.Lector["telefono"]
                    });
                }
                return lista;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public UsuarioListado? ObtenerListadoPorId(int idUsuario)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"
                    SELECT u.id_usuario, u.nombre_usuario, u.email, u.rol, u.activo, u.debe_cambiar_password,
                           ISNULL(p.nombre, '') AS nombre, ISNULL(p.apellido, '') AS apellido,
                           ISNULL(p.dni, '') AS dni, ISNULL(p.telefono, '') AS telefono
                    FROM USUARIO u
                    LEFT JOIN PERSONA p ON p.id_usuario = u.id_usuario
                    WHERE u.id_usuario = @id");
                datos.setearParametro("@id", idUsuario);
                datos.ejecutarLectura();

                if (datos.Lector.Read())
                {
                    return new UsuarioListado
                    {
                        IdUsuario = (int)datos.Lector["id_usuario"],
                        NombreUsuario = (string)datos.Lector["nombre_usuario"],
                        Email = (string)datos.Lector["email"],
                        Rol = (string)datos.Lector["rol"],
                        Activo = (bool)datos.Lector["activo"],
                        DebeCambiarPassword = (bool)datos.Lector["debe_cambiar_password"],
                        Nombre = (string)datos.Lector["nombre"],
                        Apellido = (string)datos.Lector["apellido"],
                        Dni = (string)datos.Lector["dni"],
                        Telefono = (string)datos.Lector["telefono"]
                    };
                }
                return null;
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        // Alta de usuario interno (Medico / Administrativo / SuperAdmin) con su PERSONA.
        // Si es Medico también crea la fila de MEDICO. Todo en una transacción.
        public int AgregarUsuarioInterno(Usuario usuario, string dni, string nombre, string apellido,
                                         string telefono, string? matricula, int? idEspecialidad)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"
                    SET XACT_ABORT ON;
                    BEGIN TRAN;

                        INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password)
                        VALUES (@nombreUsuario, @passwordHash, @email, @rol, 1, 1);

                        DECLARE @idUsuario INT = SCOPE_IDENTITY();

                        INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono, email)
                        VALUES (@idUsuario, @dni, @nombre, @apellido, @telefono, @email);

                        DECLARE @idPersona INT = SCOPE_IDENTITY();

                        IF @rol = 'Medico'
                        BEGIN
                            INSERT INTO MEDICO (id_medico, matricula, id_especialidad)
                            VALUES (@idPersona, @matricula, @idEspecialidad);
                            INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad)
                            VALUES (@idPersona, @idEspecialidad);
                        END

                    COMMIT;

                    SELECT @idUsuario;");

                datos.setearParametro("@nombreUsuario", usuario.NombreUsuario);
                datos.setearParametro("@passwordHash", usuario.PasswordHash);
                datos.setearParametro("@email", usuario.Email);
                datos.setearParametro("@rol", usuario.Rol);
                datos.setearParametro("@dni", dni);
                datos.setearParametro("@nombre", nombre);
                datos.setearParametro("@apellido", apellido);
                datos.setearParametro("@telefono", telefono);
                datos.setearParametro("@matricula", (object?)matricula ?? DBNull.Value);
                datos.setearParametro("@idEspecialidad", idEspecialidad.HasValue ? (object)idEspecialidad.Value : DBNull.Value);

                return datos.obtenerId();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        /// <summary>Crea el usuario (rol Médico) de un profesional que ya estaba cargado sin cuenta.</summary>
        public int AgregarUsuarioParaPersona(Usuario usuario, int idPersona)
        {
            return Sql.Numero(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol, activo, debe_cambiar_password)
                VALUES (@nombreUsuario, @hash, @email, @rol, 1, 1);
                DECLARE @u INT = SCOPE_IDENTITY();
                UPDATE PERSONA SET id_usuario = @u, email = @email WHERE id_persona = @p AND id_usuario IS NULL;
                COMMIT;
                SELECT @u;",
                ("@nombreUsuario", usuario.NombreUsuario), ("@hash", usuario.PasswordHash), ("@email", usuario.Email),
                ("@rol", usuario.Rol), ("@p", idPersona));
        }

        // Edita email + datos personales. DNI, usuario y rol no se modifican.
        public void ActualizarUsuario(int idUsuario, string email, string nombre, string apellido, string telefono)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"
                    SET XACT_ABORT ON;
                    BEGIN TRAN;
                        UPDATE USUARIO SET email = @email WHERE id_usuario = @id;
                        UPDATE PERSONA SET nombre = @nombre, apellido = @apellido, telefono = @telefono
                        WHERE id_usuario = @id;
                    COMMIT;");
                datos.setearParametro("@id", idUsuario);
                datos.setearParametro("@email", email);
                datos.setearParametro("@nombre", nombre);
                datos.setearParametro("@apellido", apellido);
                datos.setearParametro("@telefono", telefono);
                datos.ejecutarAccion();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public void CambiarEstado(int idUsuario, bool activo)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta("UPDATE USUARIO SET activo = @activo WHERE id_usuario = @id");
                datos.setearParametro("@id", idUsuario);
                datos.setearParametro("@activo", activo);
                datos.ejecutarAccion();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public void ActualizarPassword(int idUsuario, string passwordHash, bool debeCambiar)
        {
            ConexionDB datos = new ConexionDB();
            try
            {
                datos.setearConsulta(@"UPDATE USUARIO
                                       SET password_hash = @hash, debe_cambiar_password = @debe
                                       WHERE id_usuario = @id");
                datos.setearParametro("@id", idUsuario);
                datos.setearParametro("@hash", passwordHash);
                datos.setearParametro("@debe", debeCambiar);
                datos.ejecutarAccion();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

    
        // ---------- Recuperación de contraseña por enlace ----------

        public int ContarRecuperacionesRecientes(int idUsuario) =>
            Sql.Numero("SELECT COUNT(*) FROM RECUPERACION_PASSWORD WHERE id_usuario = @u AND creado > DATEADD(HOUR, -1, GETDATE())", ("@u", idUsuario));

        /// <summary>Guarda el hash del enlace (el enlace en sí nunca se guarda). Vence en 1 hora.</summary>
        public void CrearRecuperacion(int idUsuario, string tokenHash) =>
            Sql.Ejecutar(@"INSERT INTO RECUPERACION_PASSWORD (id_usuario, token_hash, vence)
                           VALUES (@u, @h, DATEADD(HOUR, 1, GETDATE()))", ("@u", idUsuario), ("@h", tokenHash));

        public bool RecuperacionVigente(string tokenHash) =>
            Sql.Existe(@"SELECT COUNT(*) FROM RECUPERACION_PASSWORD r JOIN USUARIO u ON u.id_usuario = r.id_usuario
                         WHERE r.token_hash = @h AND r.usado = 0 AND r.vence > GETDATE() AND u.activo = 1", ("@h", tokenHash));

        /// <summary>
        /// Usa el enlace (una sola vez) y cambia la contraseña. Devuelve 0 si el enlace no sirve,
        /// 1 si era una cuenta de paciente y 2 si era del personal.
        /// </summary>
        public int CompletarRecuperacion(string tokenHash, string passwordHash) =>
            Sql.Numero(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                DECLARE @t TABLE (id_usuario INT);
                UPDATE RECUPERACION_PASSWORD SET usado = 1
                OUTPUT inserted.id_usuario INTO @t
                WHERE token_hash = @h AND usado = 0 AND vence > GETDATE();
                UPDATE USUARIO SET password_hash = @p, debe_cambiar_password = 0
                WHERE activo = 1 AND id_usuario IN (SELECT id_usuario FROM @t);
                UPDATE RECUPERACION_PASSWORD SET usado = 1 WHERE usado = 0 AND id_usuario IN (SELECT id_usuario FROM @t);
                COMMIT;
                SELECT ISNULL(MAX(CASE WHEN u.rol = 'Paciente' THEN 1 ELSE 2 END), 0)
                FROM @t t JOIN USUARIO u ON u.id_usuario = t.id_usuario WHERE u.activo = 1;",
                ("@h", tokenHash), ("@p", passwordHash));
}
}
