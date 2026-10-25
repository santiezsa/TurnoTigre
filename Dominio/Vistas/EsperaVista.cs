namespace Dominio
{
    public class EsperaVista
    {
        public int IdEspera { get; set; }
        public int IdPaciente { get; set; }
        public string Paciente { get; set; } = "";
        public int IdEspecialidad { get; set; }
        public string Especialidad { get; set; } = "";
        public int IdCentro { get; set; }
        public string Centro { get; set; } = "";
        public string Franja { get; set; } = "Indistinto";
        public DateTime FechaInscripcion { get; set; }
        public int Puesto { get; set; }
    }
}
