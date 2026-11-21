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

        // RF3: filtros opcionales (rango de fechas y horario preferido)
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
        public string Horario { get; set; } = "";
        public string? ErrorFiltros { get; set; }
        public bool HayFiltros => Desde.HasValue || Hasta.HasValue || Horario != "";
        public string Minimo => DateTime.Today.ToString("yyyy-MM-dd");
        // RF3: turno duplicado de la misma especialidad, pendiente de que la persona confirme si quiere otro de todas formas
        public string? AvisoDuplicado { get; set; }
        public string? SlotDuplicado { get; set; }

        public string Maximo => DateTime.Today.AddDays(Negocio.TurnoNegocio.DiasDeBusqueda - 1).ToString("yyyy-MM-dd");

        public static ReservaVM Armar(Negocio.TurnoNegocio turnos, int idCentro, int idEsp, DateTime? dia,
                                      DateTime? desde = null, DateTime? hasta = null, string? horario = null)
        {
            var vm = new ReservaVM
            {
                IdCentro = idCentro, IdEspecialidad = idEsp, Desde = desde?.Date, Hasta = hasta?.Date,
                Horario = horario is "Mañana" or "Tarde" ? horario : ""
            };
            if (idCentro <= 0 || idEsp <= 0) return vm;
            List<SlotDisponible> todos;
            try { todos = turnos.Buscar(idCentro, idEsp, vm.Desde, vm.Hasta, vm.Horario); }
            catch (Exception ex) when (ex.GetType() == typeof(Exception)) { vm.ErrorFiltros = ex.Message; return vm; }
            vm.Dias = todos.GroupBy(s => s.FechaHora.Date).Select(g => (g.Key, g.Sum(x => x.Cupos))).OrderBy(x => x.Key).ToList();
            if (vm.Dias.Count == 0) return vm;
            vm.Dia = (dia.HasValue && vm.Dias.Any(d => d.Dia == dia.Value.Date)) ? dia.Value.Date : vm.Dias[0].Dia;
            vm.Slots = todos.Where(s => s.FechaHora.Date == vm.Dia).OrderBy(s => s.FechaHora).ThenBy(s => s.Profesional).ToList();
            return vm;
        }

        /// <summary>Enlace a un día conservando la búsqueda (persona/paciente, centro, especialidad y filtros).</summary>
        public string Enlace(DateTime dia)
        {
            var p = new List<KeyValuePair<string, string>>(Fijos)
            {
                new("centro", IdCentro.ToString()),
                new("esp", IdEspecialidad.ToString())
            };
            if (Desde.HasValue) p.Add(new("desde", Desde.Value.ToString("yyyy-MM-dd")));
            if (Hasta.HasValue) p.Add(new("hasta", Hasta.Value.ToString("yyyy-MM-dd")));
            if (Horario != "") p.Add(new("horario", Horario));
            p.Add(new("dia", dia.ToString("yyyy-MM-dd")));
            return "?" + string.Join("&", p.Select(x => x.Key + "=" + Uri.EscapeDataString(x.Value)));
        }

        /// <summary>Convierte un parámetro "yyyy-MM-dd" de la búsqueda en fecha (null si viene vacío o no es una fecha).</summary>
        public static DateTime? FechaOpcional(string? texto) =>
            DateTime.TryParseExact(texto, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var f) ? f : null;
    }
}
