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

        // La confirmacion se habilita recien 48 hs antes del turno, y vence a las 24 horas previas
        public bool EnVentanaConfirmacion =>
            FechaHora <= DateTime.Now.AddHours(48) && FechaHora > DateTime.Now.AddHours(24);
        public bool PuedeConfirmar => Estado == "PendienteConfirmacion" && EnVentanaConfirmacion;
    }
}
