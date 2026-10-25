namespace Dominio
{
    public class AgendaNueva
    {
        public int IdMedico { get; set; }
        public int IdCentro { get; set; }
        public int IdEspecialidad { get; set; }
        public string Tipo { get; set; } = "Mensual";          // Mensual | PorFecha
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public string HoraInicio { get; set; } = "08:00";
        public string HoraFin { get; set; } = "12:00";
        public List<int> Dias { get; set; } = new();           // DayOfWeek (0 = domingo)
        public int DuracionMinutos { get; set; } = 20;
        public int PacientesPorFranja { get; set; } = 1;
        public List<FechaAgenda> Fechas { get; set; } = new();
    }
}
