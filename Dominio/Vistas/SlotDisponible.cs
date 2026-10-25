namespace Dominio
{
    public class SlotDisponible
    {
        public int IdAgenda { get; set; }
        public DateTime FechaHora { get; set; }
        public int Cupos { get; set; }
        public int IdMedico { get; set; }
        public string Profesional { get; set; } = "";
        public string Especialidad { get; set; } = "";
        public string Centro { get; set; } = "";
    }
}
