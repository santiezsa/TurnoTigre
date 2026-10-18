namespace Dominio
{
    public class FechaAgenda
    {
        public DateTime Fecha { get; set; }
        public string HoraInicio { get; set; } = "08:00";
        public string HoraFin { get; set; } = "12:00";
    }

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

    public class TurnoVista
    {
        public int IdTurno { get; set; }
        public int IdAgenda { get; set; }
        public DateTime FechaHora { get; set; }
        public string Estado { get; set; } = "";
        public int? IdPaciente { get; set; }
        public string Paciente { get; set; } = "";
        public string Dni { get; set; } = "";
        public string Telefono { get; set; } = "";
        public int IdMedico { get; set; }
        public string Profesional { get; set; } = "";
        public int IdEspecialidad { get; set; }
        public string Especialidad { get; set; } = "";
        public int IdCentro { get; set; }
        public string Centro { get; set; } = "";
        public DateTime? LimiteConfirmacion { get; set; }
        public string Hora => FechaHora.ToString("HH:mm");
        /// <summary>"Nombre Apellido" (Paciente viene como "Apellido, Nombre").</summary>
        public string PacienteNombre
        {
            get { var p = Paciente.Split(", ", 2); return p.Length == 2 ? p[1] + " " + p[0] : Paciente; }
        }

        /// <summary>Estado tal como lo muestra el sistema (PendienteConfirmacion = "Reservado").</summary>
        public string EstadoVisible => Estado switch
        {
            "PendienteConfirmacion" => "Reservado",
            "EnAtencion" => "En atención",
            _ => Estado
        };
        public bool Futuro => FechaHora > DateTime.Now;
        public bool PuedeCancelar => (Estado == "PendienteConfirmacion" || Estado == "Confirmado") && Futuro;
        public bool PuedeConfirmar => Estado == "PendienteConfirmacion" && Futuro;
    }

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

    public class PacienteFicha
    {
        public int IdPaciente { get; set; }
        public int? IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string TipoDocumento { get; set; } = "DNI";
        public string Dni { get; set; } = "";
        public string Email { get; set; } = "";
        public string Celular { get; set; } = "";
        public string Fijo { get; set; } = "";
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = "";
        public string Calle { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Localidad { get; set; } = "";
        public string Provincia { get; set; } = "Buenos Aires";
        public string CodigoPostal { get; set; } = "";
        public int? IdObraSocial { get; set; }
        public string ObraSocial { get; set; } = "";
        public string NroAfiliado { get; set; } = "";
        public int? IdTitular { get; set; }
        public string Parentesco { get; set; } = "";
        public bool TieneFoto { get; set; }
        public string Completo => $"{Apellido}, {Nombre}";
        public string Direccion => $"{Calle} {Altura}".Trim();
        public string Tel => !string.IsNullOrEmpty(Celular) ? Celular : Fijo;
        public string Iniciales => (Nombre.Length > 0 && Apellido.Length > 0) ? $"{Nombre[0]}{Apellido[0]}".ToUpper() : "?";
        public int Edad
        {
            get
            {
                var hoy = DateTime.Today; int e = hoy.Year - FechaNacimiento.Year;
                if (FechaNacimiento.Date > hoy.AddYears(-e)) e--;
                return e;
            }
        }
    }

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

    public class ArchivoVista
    {
        public int IdArchivo { get; set; }
        public string Nombre { get; set; } = "";
        public string TipoContenido { get; set; } = "";
    }

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

    public class NotificacionVista
    {
        public int IdNotificacion { get; set; }
        public string Tipo { get; set; } = "";
        public string Mensaje { get; set; } = "";
        public DateTime Fecha { get; set; }
        public bool Leida { get; set; }
    }
}
