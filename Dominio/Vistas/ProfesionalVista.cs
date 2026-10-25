namespace Dominio
{
    public class ProfesionalVista
    {
        public int IdMedico { get; set; }
        public int? IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Matricula { get; set; } = "";
        public string Email { get; set; } = "";
        public string Telefono { get; set; } = "";
        public bool Activo { get; set; } = true;
        public List<Especialidad> Especialidades { get; set; } = new();
        public int CantAgendas { get; set; }
        public string Completo => $"{Apellido}, {Nombre}";
        public bool TieneUsuario => IdUsuario.HasValue;
    }
}
