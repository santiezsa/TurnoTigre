using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class ProfesionalDatos
    {
        private const string Seleccion = @"
            SELECT m.id_medico, p.id_usuario, p.nombre, p.apellido, p.dni, m.matricula,
                   ISNULL(p.email, ISNULL(u.email, '')) AS email, p.telefono, p.activo,
                   (SELECT COUNT(*) FROM AGENDA a WHERE a.id_medico = m.id_medico) AS agendas
            FROM MEDICO m
            JOIN PERSONA p ON p.id_persona = m.id_medico
            LEFT JOIN USUARIO u ON u.id_usuario = p.id_usuario ";

        private static ProfesionalVista Mapear(Microsoft.Data.SqlClient.SqlDataReader r) => new ProfesionalVista
        {
            IdMedico = Ent(r, "id_medico"),
            IdUsuario = EntN(r, "id_usuario"),
            Nombre = Txt(r, "nombre"),
            Apellido = Txt(r, "apellido"),
            Dni = Txt(r, "dni"),
            Matricula = Txt(r, "matricula"),
            Email = Txt(r, "email"),
            Telefono = Txt(r, "telefono"),
            Activo = Bool(r, "activo"),
            CantAgendas = Ent(r, "agendas")
        };

        private void CargarEspecialidades(List<ProfesionalVista> lista)
        {
            if (lista.Count == 0) return;
            var rel = Lista(@"SELECT me.id_medico, e.id_especialidad, e.codigo, e.descripcion
                              FROM MEDICO_ESPECIALIDAD me JOIN ESPECIALIDAD e ON e.id_especialidad = me.id_especialidad
                              ORDER BY e.descripcion",
                r => (Ent(r, "id_medico"), new Especialidad { IdEspecialidad = Ent(r, "id_especialidad"), Codigo = Txt(r, "codigo"), Descripcion = Txt(r, "descripcion") }));
            foreach (var p in lista)
                p.Especialidades = rel.Where(x => x.Item1 == p.IdMedico).Select(x => x.Item2).ToList();
        }

        public List<ProfesionalVista> Listar(string? texto = null, int? idEspecialidad = null)
        {
            var lista = Lista(Seleccion + @"
                WHERE p.activo = 1
                  AND (@t IS NULL OR p.apellido LIKE @t OR p.nombre LIKE @t OR p.dni LIKE @t OR m.matricula LIKE @t)
                  AND (@e IS NULL OR EXISTS (SELECT 1 FROM MEDICO_ESPECIALIDAD me WHERE me.id_medico = m.id_medico AND me.id_especialidad = @e))
                ORDER BY p.apellido, p.nombre",
                Mapear, ("@t", string.IsNullOrWhiteSpace(texto) ? null : "%" + texto.Trim() + "%"), ("@e", idEspecialidad));
            CargarEspecialidades(lista);
            return lista;
        }

        public ProfesionalVista? Obtener(int idMedico)
        {
            var p = Uno(Seleccion + " WHERE m.id_medico = @id", Mapear, ("@id", idMedico));
            if (p != null) CargarEspecialidades(new List<ProfesionalVista> { p });
            return p;
        }

        public ProfesionalVista? ObtenerPorUsuario(int idUsuario)
        {
            var p = Uno(Seleccion + " WHERE p.id_usuario = @u", Mapear, ("@u", idUsuario));
            if (p != null) CargarEspecialidades(new List<ProfesionalVista> { p });
            return p;
        }

        public List<ProfesionalVista> SinUsuario()
        {
            var lista = Lista(Seleccion + " WHERE p.id_usuario IS NULL AND p.activo = 1 ORDER BY p.apellido, p.nombre", Mapear);
            CargarEspecialidades(lista);
            return lista;
        }

        public bool ExisteDni(string dni, int? excluirPersona = null) =>
            Existe("SELECT COUNT(*) FROM PERSONA WHERE dni = @d AND (@x IS NULL OR id_persona <> @x)", ("@d", dni), ("@x", excluirPersona));

        public bool ExisteMatricula(string matricula, int? excluirMedico = null) =>
            Existe("SELECT COUNT(*) FROM MEDICO WHERE matricula = @m AND (@x IS NULL OR id_medico <> @x)", ("@m", matricula), ("@x", excluirMedico));

        public int Crear(ProfesionalVista p, List<int> especialidades)
        {
            string filas = string.Join(",", especialidades.Select(e => $"(@id, {int.Parse(e.ToString())})"));
            return Numero($@"
                SET XACT_ABORT ON; BEGIN TRAN;
                INSERT INTO PERSONA (dni, nombre, apellido, telefono, email) VALUES (@dni, @nombre, @apellido, @tel, @email);
                DECLARE @id INT = SCOPE_IDENTITY();
                INSERT INTO MEDICO (id_medico, matricula, id_especialidad) VALUES (@id, @mat, @primera);
                INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES {filas};
                COMMIT;
                SELECT @id;",
                ("@dni", p.Dni), ("@nombre", p.Nombre), ("@apellido", p.Apellido), ("@tel", p.Telefono ?? ""),
                ("@email", string.IsNullOrWhiteSpace(p.Email) ? null : p.Email), ("@mat", p.Matricula), ("@primera", especialidades[0]));
        }

        public void Actualizar(ProfesionalVista p, List<int> especialidades)
        {
            string filas = string.Join(",", especialidades.Select(e => $"(@id, {int.Parse(e.ToString())})"));
            Ejecutar($@"
                SET XACT_ABORT ON; BEGIN TRAN;
                UPDATE PERSONA SET nombre = @nombre, apellido = @apellido, telefono = @tel, email = @email WHERE id_persona = @id;
                UPDATE MEDICO SET matricula = @mat, id_especialidad = @primera WHERE id_medico = @id;
                DELETE FROM MEDICO_ESPECIALIDAD WHERE id_medico = @id;
                INSERT INTO MEDICO_ESPECIALIDAD (id_medico, id_especialidad) VALUES {filas};
                COMMIT;",
                ("@id", p.IdMedico), ("@nombre", p.Nombre), ("@apellido", p.Apellido), ("@tel", p.Telefono ?? ""),
                ("@email", string.IsNullOrWhiteSpace(p.Email) ? null : p.Email), ("@mat", p.Matricula), ("@primera", especialidades[0]));
        }

        public bool TieneConsultas(int idMedico) => Existe("SELECT COUNT(*) FROM CONSULTA WHERE id_medico = @id", ("@id", idMedico));

        /// <summary>Baja lógica: el profesional deja de aparecer y su usuario (si tiene) queda deshabilitado.</summary>
        public void DarDeBaja(int idMedico)
        {
            Ejecutar(@"SET XACT_ABORT ON; BEGIN TRAN;
                       UPDATE USUARIO SET activo = 0
                       WHERE rol = 'Medico' AND id_usuario = (SELECT id_usuario FROM PERSONA WHERE id_persona = @id);
                       UPDATE PERSONA SET activo = 0 WHERE id_persona = @id;
                       COMMIT;", ("@id", idMedico));
        }

        public void Eliminar(int idMedico)
        {
            Ejecutar(@"SET XACT_ABORT ON; BEGIN TRAN;
                       DELETE FROM MEDICO_ESPECIALIDAD WHERE id_medico = @id;
                       DELETE FROM MEDICO WHERE id_medico = @id;
                       DELETE FROM PERSONA WHERE id_persona = @id;
                       COMMIT;", ("@id", idMedico));
        }
    }
}
