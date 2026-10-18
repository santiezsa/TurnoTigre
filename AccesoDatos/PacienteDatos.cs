using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class PacienteDatos
    {
        // ---------- Registro de un paciente nuevo con su cuenta ----------
        public int AgregarPaciente(Paciente paciente)
        {
            ConexionDB datos = new ConexionDB();

            try
            {
                datos.setearConsulta(@"
                    SET XACT_ABORT ON;
                    BEGIN TRAN;

                        INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol)
                        VALUES (@email, @passwordHash, @email, 'Paciente');

                        DECLARE @idUsuario INT = SCOPE_IDENTITY();

                        INSERT INTO PERSONA (id_usuario, dni, nombre, apellido, telefono, email)
                        VALUES (@idUsuario, @dni, @nombre, @apellido, @telefono, @email);

                        DECLARE @idPersona INT = SCOPE_IDENTITY();

                        INSERT INTO PACIENTE (id_paciente, fecha_nacimiento, sexo, calle, altura, localidad, provincia,
                                              id_obra_social, nro_afiliado, id_titular, parentesco)
                        VALUES (@idPersona, @fechaNacimiento, @sexo, @calle, @altura, @localidad, 'Buenos Aires',
                                @idObraSocial, @nroAfiliado, @idTitular, @parentesco);

                    COMMIT;

                    SELECT @idPersona;");

                datos.setearParametro("@email", paciente.Usuario!.Email);
                datos.setearParametro("@passwordHash", paciente.Usuario.PasswordHash);
                datos.setearParametro("@dni", paciente.Dni);
                datos.setearParametro("@nombre", paciente.Nombre);
                datos.setearParametro("@apellido", paciente.Apellido);
                datos.setearParametro("@telefono", paciente.Telefono);
                datos.setearParametro("@fechaNacimiento", paciente.FechaNacimiento);
                datos.setearParametro("@sexo", paciente.Sexo);
                datos.setearParametro("@calle", paciente.Calle);
                datos.setearParametro("@altura", paciente.Altura);
                datos.setearParametro("@localidad", paciente.Localidad);
                datos.setearParametro("@idObraSocial", paciente.IdObraSocial);
                datos.setearParametro("@nroAfiliado", paciente.NroAfiliado);
                datos.setearParametro("@idTitular", paciente.IdTitular);
                datos.setearParametro("@parentesco", paciente.Parentesco);

                return datos.obtenerId();
            }
            finally
            {
                datos.cerrarConexion();
            }
        }

        public bool ExisteDni(string dni) => Existe("SELECT COUNT(*) FROM PERSONA WHERE dni = @dni", ("@dni", dni));

        /// <summary>Crea la cuenta de una ficha que ya existía (cargada por el personal) y actualiza sus datos de contacto.</summary>
        public void VincularUsuario(int idPaciente, PacienteFicha datos, string passwordHash)
        {
            Ejecutar(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                INSERT INTO USUARIO (nombre_usuario, password_hash, email, rol) VALUES (@email, @hash, @email, 'Paciente');
                DECLARE @u INT = SCOPE_IDENTITY();
                UPDATE PERSONA SET id_usuario = @u, telefono = @tel, email = @email, nombre = @nombre, apellido = @apellido, activo = 1 WHERE id_persona = @id;
                UPDATE PACIENTE SET sexo = @sexo, calle = @calle, altura = @altura, localidad = @loc WHERE id_paciente = @id;
                COMMIT;",
                ("@id", idPaciente), ("@email", datos.Email), ("@hash", passwordHash), ("@tel", datos.Celular),
                ("@nombre", datos.Nombre), ("@apellido", datos.Apellido), ("@sexo", datos.Sexo),
                ("@calle", datos.Calle), ("@altura", datos.Altura), ("@loc", datos.Localidad));
        }

        // ---------- Fichas ----------
        private const string FichaSel = @"
            SELECT pa.id_paciente, p.id_usuario, p.nombre, p.apellido, pa.tipo_documento, p.dni,
                   ISNULL(p.email, ISNULL(u.email, '')) AS email, p.telefono, pa.telefono_fijo, pa.fecha_nacimiento, pa.sexo,
                   pa.calle, pa.altura, pa.localidad, pa.provincia, pa.codigo_postal, pa.id_obra_social, o.nombre AS obra,
                   pa.nro_afiliado, pa.id_titular, pa.parentesco, p.activo,
                   CASE WHEN p.foto IS NULL THEN 0 ELSE 1 END AS tiene_foto
            FROM PACIENTE pa
            JOIN PERSONA p ON p.id_persona = pa.id_paciente
            LEFT JOIN USUARIO u ON u.id_usuario = p.id_usuario
            LEFT JOIN OBRA_SOCIAL o ON o.id_obra_social = pa.id_obra_social ";

        private static PacienteFicha Mapear(Microsoft.Data.SqlClient.SqlDataReader r) => new PacienteFicha
        {
            IdPaciente = Ent(r, "id_paciente"),
            IdUsuario = EntN(r, "id_usuario"),
            Nombre = Txt(r, "nombre"),
            Apellido = Txt(r, "apellido"),
            TipoDocumento = Txt(r, "tipo_documento"),
            Dni = Txt(r, "dni"),
            Email = Txt(r, "email"),
            Celular = Txt(r, "telefono"),
            Fijo = Txt(r, "telefono_fijo"),
            FechaNacimiento = Fecha(r, "fecha_nacimiento"),
            Sexo = Txt(r, "sexo"),
            Calle = Txt(r, "calle"),
            Altura = Txt(r, "altura"),
            Localidad = Txt(r, "localidad"),
            Provincia = Txt(r, "provincia"),
            CodigoPostal = Txt(r, "codigo_postal"),
            IdObraSocial = EntN(r, "id_obra_social"),
            ObraSocial = Txt(r, "obra"),
            NroAfiliado = Txt(r, "nro_afiliado"),
            IdTitular = EntN(r, "id_titular"),
            Parentesco = Txt(r, "parentesco"),
            TieneFoto = Ent(r, "tiene_foto") == 1
        };

        // ---------- Foto de perfil ----------
        public void GuardarFoto(int idPersona, byte[] contenido, string tipo) =>
            Ejecutar("UPDATE PERSONA SET foto = @f, foto_tipo = @t WHERE id_persona = @id", ("@f", contenido), ("@t", tipo), ("@id", idPersona));

        public void QuitarFoto(int idPersona) =>
            Ejecutar("UPDATE PERSONA SET foto = NULL, foto_tipo = NULL WHERE id_persona = @id", ("@id", idPersona));

        public (byte[] Contenido, string Tipo)? ObtenerFoto(int idPersona) =>
            Lista("SELECT foto, foto_tipo FROM PERSONA WHERE id_persona = @id AND foto IS NOT NULL",
                  r => ((byte[])r["foto"], Txt(r, "foto_tipo")), ("@id", idPersona))
            .Select(x => ((byte[], string)?)x).FirstOrDefault();

        public PacienteFicha? Obtener(int idPaciente) => Uno(FichaSel + " WHERE pa.id_paciente = @id", Mapear, ("@id", idPaciente));

        public PacienteFicha? PorUsuario(int idUsuario) => Uno(FichaSel + " WHERE p.id_usuario = @u", Mapear, ("@u", idUsuario));

        /// <summary>Busca por DNI incluyendo fichas inactivas.</summary>
        public PacienteFicha? PorDni(string dni) => Uno(FichaSel + " WHERE p.dni = @d", Mapear, ("@d", dni));

        public bool DniEsDeOtraPersona(string dni, int? idPaciente) =>
            Existe("SELECT COUNT(*) FROM PERSONA WHERE dni = @d AND (@x IS NULL OR id_persona <> @x)", ("@d", dni), ("@x", idPaciente));

        public List<PacienteFicha> Buscar(string? texto)
        {
            return Lista(FichaSel + @"
                WHERE p.activo = 1 AND (@t IS NULL OR p.apellido LIKE @t OR p.nombre LIKE @t OR p.dni LIKE @t)
                ORDER BY p.apellido, p.nombre",
                Mapear, ("@t", string.IsNullOrWhiteSpace(texto) ? null : "%" + texto.Trim() + "%"));
        }

        public List<PacienteFicha> Familiares(int idTitular) =>
            Lista(FichaSel + " WHERE pa.id_titular = @t AND p.activo = 1 ORDER BY pa.fecha_nacimiento", Mapear, ("@t", idTitular));

        public int ContarFamiliares(int idTitular) =>
            Numero("SELECT COUNT(*) FROM PACIENTE pa JOIN PERSONA p ON p.id_persona = pa.id_paciente WHERE pa.id_titular = @t AND p.activo = 1", ("@t", idTitular));

        public int Crear(PacienteFicha f)
        {
            return Numero(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                INSERT INTO PERSONA (dni, nombre, apellido, telefono, email) VALUES (@dni, @nombre, @apellido, @tel, @email);
                DECLARE @id INT = SCOPE_IDENTITY();
                INSERT INTO PACIENTE (id_paciente, tipo_documento, telefono_fijo, fecha_nacimiento, sexo, calle, altura, localidad, provincia,
                                      codigo_postal, id_obra_social, nro_afiliado, id_titular, parentesco)
                VALUES (@id, @tipodoc, @fijo, @nac, @sexo, @calle, @altura, @loc, @prov, @cp, @obra, @afil, @tit, @par);
                COMMIT;
                SELECT @id;", Parametros(f));
        }

        public void Actualizar(PacienteFicha f)
        {
            Ejecutar(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                UPDATE PERSONA SET nombre = @nombre, apellido = @apellido, telefono = @tel, email = @email WHERE id_persona = @id;
                UPDATE PACIENTE SET tipo_documento = @tipodoc, telefono_fijo = @fijo, fecha_nacimiento = @nac, sexo = @sexo,
                                    calle = @calle, altura = @altura, localidad = @loc, provincia = @prov, codigo_postal = @cp,
                                    id_obra_social = @obra, nro_afiliado = @afil
                WHERE id_paciente = @id;
                COMMIT;", Parametros(f, f.IdPaciente));
        }

        private static (string, object?)[] Parametros(PacienteFicha f, int? id = null)
        {
            var lista = ParametrosBase(f);
            if (id.HasValue) lista.Add(("@id", id.Value));   // solo en UPDATE: en el INSERT @id se declara dentro del lote
            return lista.ToArray();
        }

        private static List<(string, object?)> ParametrosBase(PacienteFicha f) => new List<(string, object?)>
        {
            ("@dni", f.Dni), ("@nombre", f.Nombre), ("@apellido", f.Apellido), ("@tel", f.Celular ?? ""),
            ("@email", string.IsNullOrWhiteSpace(f.Email) ? null : f.Email), ("@tipodoc", string.IsNullOrWhiteSpace(f.TipoDocumento) ? "DNI" : f.TipoDocumento),
            ("@fijo", string.IsNullOrWhiteSpace(f.Fijo) ? null : f.Fijo), ("@nac", f.FechaNacimiento.Date), ("@sexo", f.Sexo),
            ("@calle", f.Calle), ("@altura", f.Altura), ("@loc", f.Localidad), ("@prov", string.IsNullOrWhiteSpace(f.Provincia) ? "Buenos Aires" : f.Provincia),
            ("@cp", string.IsNullOrWhiteSpace(f.CodigoPostal) ? null : f.CodigoPostal), ("@obra", f.IdObraSocial),
            ("@afil", string.IsNullOrWhiteSpace(f.NroAfiliado) ? null : f.NroAfiliado), ("@tit", f.IdTitular), ("@par", string.IsNullOrWhiteSpace(f.Parentesco) ? null : f.Parentesco)
        };

        /// <summary>Asocia una ficha existente (sin titular) a un titular como familiar y la deja activa.</summary>
        public void AsignarTitular(int idPaciente, int idTitular, string parentesco)
        {
            Ejecutar(@"UPDATE PACIENTE SET id_titular = @t, parentesco = @par WHERE id_paciente = @id;
                       UPDATE PERSONA SET activo = 1 WHERE id_persona = @id;",
                ("@id", idPaciente), ("@t", idTitular), ("@par", parentesco));
        }

        /// <summary>Quita al familiar del grupo (queda inactivo; sus turnos pasados se conservan).</summary>
        public void QuitarFamiliar(int idPaciente)
        {
            Ejecutar(@"UPDATE PACIENTE SET id_titular = NULL, parentesco = NULL WHERE id_paciente = @id;
                       UPDATE PERSONA SET activo = 0 WHERE id_persona = @id;
                       UPDATE LISTA_ESPERA SET activa = 0 WHERE id_paciente = @id;", ("@id", idPaciente));
        }

        /// <summary>Datos de contacto/domicilio del titular para su propia pantalla "Mis datos".</summary>
        public void ActualizarContacto(PacienteFicha f)
        {
            Ejecutar(@"
                SET XACT_ABORT ON; BEGIN TRAN;
                UPDATE PERSONA SET nombre = @nombre, apellido = @apellido, telefono = @tel WHERE id_persona = @id;
                UPDATE PACIENTE SET telefono_fijo = @fijo, fecha_nacimiento = @nac, sexo = @sexo, calle = @calle, altura = @altura,
                                    localidad = @loc, codigo_postal = @cp, id_obra_social = @obra, nro_afiliado = @afil
                WHERE id_paciente = @id;
                COMMIT;", Parametros(f, f.IdPaciente));
        }
    }
}
