using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    /// <summary>Consulta de la auditoría de accesos a historias clínicas (AUDITORIA_ACCESO).</summary>
    public class AuditoriaDatos
    {
        private const string Origen = @"
            FROM AUDITORIA_ACCESO a
            JOIN USUARIO u ON u.id_usuario = a.id_usuario
            LEFT JOIN PERSONA pu ON pu.id_usuario = u.id_usuario
            JOIN PERSONA pp ON pp.id_persona = a.id_paciente
            WHERE (@p IS NULL OR pp.apellido LIKE @p OR pp.nombre LIKE @p OR pp.dni LIKE @p OR CONCAT(pp.nombre, ' ', pp.apellido) LIKE @p)
              AND (@u IS NULL OR u.nombre_usuario LIKE @u OR u.email LIKE @u OR pu.apellido LIKE @u OR pu.nombre LIKE @u OR CONCAT(pu.nombre, ' ', pu.apellido) LIKE @u)
              AND (@a IS NULL OR a.accion = @a)
              AND (@d IS NULL OR a.fecha >= @d)
              AND (@h IS NULL OR a.fecha < DATEADD(DAY, 1, @h)) ";

        private static (string, object?)[] Filtros(string? paciente, string? usuario, string? accion, DateTime? desde, DateTime? hasta) => new (string, object?)[]
        {
            ("@p", Patron(paciente)), ("@u", Patron(usuario)), ("@a", string.IsNullOrWhiteSpace(accion) ? null : accion),
            ("@d", desde?.Date), ("@h", hasta?.Date)
        };

        private static string? Patron(string? texto) => string.IsNullOrWhiteSpace(texto) ? null : "%" + texto.Trim() + "%";

        public int Contar(string? paciente, string? usuario, string? accion, DateTime? desde, DateTime? hasta) =>
            Numero("SELECT COUNT(*) " + Origen, Filtros(paciente, usuario, accion, desde, hasta));

        /// <summary>Una página de registros, del más reciente al más antiguo.</summary>
        public List<AuditoriaVista> Buscar(string? paciente, string? usuario, string? accion, DateTime? desde, DateTime? hasta, int salto, int cantidad)
        {
            var parametros = Filtros(paciente, usuario, accion, desde, hasta).Concat(new (string, object?)[] { ("@o", salto), ("@n", cantidad) }).ToArray();
            return Lista(@"SELECT a.id_auditoria, a.fecha, u.nombre_usuario, pu.apellido AS ap_usr, pu.nombre AS no_usr, a.rol,
                                  a.id_paciente, pp.apellido AS ap_pac, pp.nombre AS no_pac, pp.dni, a.accion, a.detalle "
                + Origen + "ORDER BY a.fecha DESC, a.id_auditoria DESC OFFSET @o ROWS FETCH NEXT @n ROWS ONLY",
                r => new AuditoriaVista
                {
                    IdAuditoria = Ent(r, "id_auditoria"),
                    Fecha = Fecha(r, "fecha"),
                    Usuario = Txt(r, "nombre_usuario"),
                    NombreUsuario = r["ap_usr"] is DBNull ? "" : Txt(r, "ap_usr") + ", " + Txt(r, "no_usr"),
                    Rol = Txt(r, "rol"),
                    IdPaciente = Ent(r, "id_paciente"),
                    Paciente = Txt(r, "ap_pac") + ", " + Txt(r, "no_pac"),
                    Dni = Txt(r, "dni"),
                    Accion = Txt(r, "accion"),
                    Detalle = Txt(r, "detalle")
                }, parametros);
        }
    }
}
