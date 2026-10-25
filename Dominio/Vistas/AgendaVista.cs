namespace Dominio
{
    public class AgendaVista
    {
        public int IdAgenda { get; set; }
        public int IdMedico { get; set; }
        public string Profesional { get; set; } = "";
        public int IdEspecialidad { get; set; }
        public string Especialidad { get; set; } = "";
        public int IdCentro { get; set; }
        public string Centro { get; set; } = "";
        public string Tipo { get; set; } = "Mensual";
        public DateTime VigenciaDesde { get; set; }
        public DateTime VigenciaHasta { get; set; }
        public string HoraInicio { get; set; } = "";
        public string HoraFin { get; set; } = "";
        public string DiasSemana { get; set; } = "";
        public int DuracionMinutos { get; set; }
        public int PacientesPorFranja { get; set; }
        public int TotalTurnos { get; set; }
        public int TurnosAsignados { get; set; }
        public int CantFechas { get; set; }
        public bool Editable => TurnosAsignados == 0;

        public string Resumen
        {
            get
            {
                if (Tipo == "PorFecha") return $"{CantFechas} fecha{(CantFechas == 1 ? "" : "s")} cargada{(CantFechas == 1 ? "" : "s")}";
                var nombres = new[] { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };
                var dias = (DiasSemana ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).OrderBy(d => d == 0 ? 7 : d).Select(d => nombres[d]);
                return $"{string.Join(", ", dias)} · {HoraInicio} a {HoraFin} · hasta {VigenciaHasta:dd/MM}";
            }
        }
    }
}
