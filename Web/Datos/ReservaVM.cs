using Dominio;

namespace Web.Datos
{
    /// <summary>Datos para el buscador de turnos (lo usan el paciente y el personal administrativo).</summary>
    public class ReservaVM
    {
        public string Pagina { get; set; } = "";
        public Dictionary<string, string> Fijos { get; set; } = new();   // parámetros que viajan siempre (persona / paciente)
        public List<CentroSalud> Centros { get; set; } = new();
        public List<Especialidad> Especialidades { get; set; } = new();
        public int IdCentro { get; set; }
        public int IdEspecialidad { get; set; }
        public DateTime? Dia { get; set; }
        public List<(DateTime Dia, int Turnos)> Dias { get; set; } = new();
        public List<SlotDisponible> Slots { get; set; } = new();
        public bool BuscoAlgo => IdCentro > 0 && IdEspecialidad > 0;
        public bool PermiteEspera { get; set; }
        public string TextoBoton { get; set; } = "Reservar";

        public static ReservaVM Armar(Negocio.TurnoNegocio turnos, int idCentro, int idEsp, DateTime? dia)
        {
            var vm = new ReservaVM { IdCentro = idCentro, IdEspecialidad = idEsp };
            if (idCentro <= 0 || idEsp <= 0) return vm;
            var todos = turnos.Disponibles(idCentro, idEsp, DateTime.Now, DateTime.Today.AddDays(61));
            vm.Dias = todos.GroupBy(s => s.FechaHora.Date).Select(g => (g.Key, g.Sum(x => x.Cupos))).OrderBy(x => x.Key).ToList();
            if (vm.Dias.Count == 0) return vm;
            vm.Dia = (dia.HasValue && vm.Dias.Any(d => d.Dia == dia.Value.Date)) ? dia.Value.Date : vm.Dias[0].Dia;
            vm.Slots = todos.Where(s => s.FechaHora.Date == vm.Dia).OrderBy(s => s.FechaHora).ThenBy(s => s.Profesional).ToList();
            return vm;
        }
    }
}
