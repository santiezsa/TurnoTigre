namespace Dominio
{
    /// <summary>Una fila de la auditoría de accesos a historias clínicas.</summary>
    public class AuditoriaVista
    {
        public int IdAuditoria { get; set; }
        public DateTime Fecha { get; set; }
        public string Usuario { get; set; } = "";
        /// <summary>"Apellido, Nombre" de quien accedió (vacío si el usuario no tiene ficha de persona).</summary>
        public string NombreUsuario { get; set; } = "";
        public string Rol { get; set; } = "";
        public int IdPaciente { get; set; }
        public string Paciente { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Accion { get; set; } = "";
        public string Detalle { get; set; } = "";
    }
}
