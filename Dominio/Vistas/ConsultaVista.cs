namespace Dominio
{
    public class ConsultaVista
    {
        public int IdConsulta { get; set; }
        public int? IdTurno { get; set; }
        public DateTime Fecha { get; set; }
        public int IdMedico { get; set; }
        public string Medico { get; set; } = "";
        public string Especialidad { get; set; } = "";
        public string Observaciones { get; set; } = "";
        public string Diagnostico { get; set; } = "";
        public string Tratamientos { get; set; } = "";
        public string ProximosPasos { get; set; } = "";
        public List<ArchivoVista> Archivos { get; set; } = new();
    }
}
