namespace Dominio
{
    public class HistoriaClinicaVista
    {
        public int IdPaciente { get; set; }
        public string GrupoSanguineo { get; set; } = "";
        public decimal? Peso { get; set; }
        public int? AlturaCm { get; set; }
        public string Alergias { get; set; } = "";
        public string EnfermedadesCronicas { get; set; } = "";
        public string Patologias { get; set; } = "";
        public List<ConsultaVista> Consultas { get; set; } = new();
    }
}
