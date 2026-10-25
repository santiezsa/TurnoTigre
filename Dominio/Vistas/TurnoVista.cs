namespace Dominio
{
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

        // La confirmación se habilita 24 hs antes del límite y vence en el límite.
        // Turno normal: límite = 24 hs antes de la cita (se confirma de 48 a 24 hs antes).
        // Turno reasignado desde la lista de espera: el paciente tiene 24 hs desde la reasignación.
        public bool EnVentanaConfirmacion => LimiteConfirmacion.HasValue
            ? DateTime.Now >= LimiteConfirmacion.Value.AddHours(-24) && DateTime.Now < LimiteConfirmacion.Value
            : FechaHora <= DateTime.Now.AddHours(48) && FechaHora > DateTime.Now.AddHours(24);
        public bool PuedeConfirmar => Estado == "PendienteConfirmacion" && EnVentanaConfirmacion;
    }
}
