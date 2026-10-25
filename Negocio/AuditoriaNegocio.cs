using AccesoDatos;
using Dominio;

namespace Negocio
{
    /// <summary>Consulta de la auditoría de accesos a historias clínicas (solo para el Super Admin).</summary>
    public class AuditoriaNegocio
    {
        public const int PorPagina = 25;

        public static readonly string[] Acciones =
        {
            HistoriaClinicaNegocio.AccionVer, HistoriaClinicaNegocio.AccionGuardar,
            HistoriaClinicaNegocio.AccionDescargar, HistoriaClinicaNegocio.AccionDenegado
        };

        private readonly AuditoriaDatos datos = new AuditoriaDatos();

        /// <summary>
        /// Registros que cumplen los filtros (todos opcionales), del más reciente al más antiguo.
        /// "hasta" incluye el día completo. Una acción desconocida se ignora. La página se acomoda al rango válido.
        /// </summary>
        public (List<AuditoriaVista> Registros, int Total, int Pagina, int TotalPaginas) Buscar(
            string? paciente, string? usuario, string? accion, DateTime? desde, DateTime? hasta, int pagina)
        {
            if (accion != null && !Acciones.Contains(accion)) accion = null;
            int total = datos.Contar(paciente, usuario, accion, desde, hasta);
            int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)PorPagina));
            pagina = Math.Clamp(pagina, 1, totalPaginas);
            var registros = datos.Buscar(paciente, usuario, accion, desde, hasta, (pagina - 1) * PorPagina, PorPagina);
            return (registros, total, pagina, totalPaginas);
        }
    }
}
