namespace Web.Datos
{
    public class CalendarioVM
    {
        public DateTime Seleccionada { get; set; } = DateTime.Today;
        public string Pagina { get; set; } = "";
        public int? Medico { get; set; }
        public HashSet<DateTime> ConAtencion { get; set; } = new();
    }
}
