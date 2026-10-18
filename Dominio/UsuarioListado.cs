namespace Dominio
{
    // Fila de la grilla de usuarios del Super Admin (USUARIO + PERSONA)
    public class UsuarioListado
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public bool DebeCambiarPassword { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string Dni { get; set; } = string.Empty;
        public string Telefono { get; set; } = string.Empty;
        public string NombreCompleto => (Apellido + " " + Nombre).Trim();
    }
}
