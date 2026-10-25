using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class TurnoNegocio
    {
        private readonly TurnoDatos datos = new TurnoDatos();
        private readonly PacienteDatos pacientes = new PacienteDatos();
        private readonly AgendaDatos agendas = new AgendaDatos();
        private readonly ListaEsperaDatos espera = new ListaEsperaDatos();
        private readonly ListaEsperaNegocio listaEspera = new ListaEsperaNegocio();

        /// <summary>Qué hizo una pasada de <see cref="Mantener"/>.</summary>
        public record ResultadoMantenimiento(int Vencidos, int Reasignados, int Recordatorios)
        {
            public bool HuboCambios => Vencidos + Reasignados + Recordatorios > 0;
        }

        /// <summary>
        /// Motor del sistema: vence esperas y turnos sin confirmar, reasigna los cupos liberados (RF5) y avisa que se abrió la ventana de confirmación (RF4).
        /// Lo corre el servicio en segundo plano; los errores se propagan para que quien lo llama los registre.
        /// </summary>
        public ResultadoMantenimiento Mantener()
        {
            espera.VencerAntiguas();
            var liberados = datos.VencerPendientes();
            int reasignados = 0;
            foreach (var (idAgenda, fechaHora, idPaciente) in liberados)
                if (listaEspera.ReasignarPrimerPacienteEnEspera(idAgenda, fechaHora, idPaciente)) reasignados++;
            int recordatorios = datos.GenerarRecordatoriosConfirmacion();
            return new ResultadoMantenimiento(liberados.Count, reasignados, recordatorios);
        }

        /// <summary>Misma tarea que <see cref="Mantener"/>, para llamar al abrir una pantalla: no debe impedir mostrarla, así que registra el error y sigue.</summary>
        public void Actualizar()
        {
            try { Mantener(); }
            catch (Exception ex) { RegistroErrores.Informar("Actualizar turnos al abrir una pantalla", ex); }
        }

        // ---------- consultas ----------
        public List<SlotDisponible> Disponibles(int idCentro, int idEspecialidad, DateTime desde, DateTime hasta) => datos.Disponibles(idCentro, idEspecialidad, desde, hasta);
        public List<TurnoVista> DelDia(DateTime dia, string? texto, int? idMedico, int? idEspecialidad, string? estado) => datos.DelDia(dia, texto, idMedico, idEspecialidad, EstadoInterno(estado));
        public List<TurnoVista> DelMedicoEnDia(int idMedico, DateTime dia) => datos.DelMedicoEnDia(idMedico, dia);
        public HashSet<DateTime> DiasConTurnos(DateTime mes, int? idMedico) => datos.DiasConTurnos(mes, idMedico);
        public List<TurnoVista> DeGrupoFamiliar(int idTitular) => datos.DeGrupoFamiliar(idTitular);
        public List<TurnoVista> DePaciente(int idPaciente) => datos.DePaciente(idPaciente);
        public TurnoVista? Obtener(int id) => datos.Obtener(id);
        public bool TieneTurnoFuturo(int idPaciente) => datos.TieneTurnoFuturo(idPaciente);

        /// <summary>Convierte el nombre mostrado ("Reservado") al estado guardado.</summary>
        public static string? EstadoInterno(string? visible) => string.IsNullOrWhiteSpace(visible) ? null : visible switch
        {
            "Reservado" => "PendienteConfirmacion",
            "En atención" => "EnAtencion",
            _ => visible
        };

        // ---------- reglas ----------
        private bool Pertenece(TurnoVista t, int idTitular)
        {
            if (!t.IdPaciente.HasValue) return false;
            if (t.IdPaciente.Value == idTitular) return true;
            var f = pacientes.Obtener(t.IdPaciente.Value);
            return f != null && f.IdTitular == idTitular;
        }

        /// <param name="idTitular">Paciente que inició sesión (null si reserva el personal).</param>
        public TurnoVista Reservar(int? idTitular, int idPaciente, int idAgenda, DateTime fechaHora)
        {
            var ficha = pacientes.Obtener(idPaciente) ?? throw new Exception("El paciente no existe.");
            if (idTitular.HasValue && idPaciente != idTitular.Value && ficha.IdTitular != idTitular.Value)
                throw new Exception("Solo podés sacar turnos para vos o para tus familiares.");
            var agenda = agendas.Obtener(idAgenda) ?? throw new Exception("La agenda ya no existe.");
            if (fechaHora <= DateTime.Now) throw new Exception("No se pueden reservar turnos en el pasado.");
            if (datos.TieneTurnoActivoEnEspecialidad(idPaciente, agenda.IdEspecialidad))
                throw new Exception($"{ficha.Nombre} ya tiene un turno activo de {agenda.Especialidad}. Cancelalo o esperá a que pase para pedir otro.");

            int id = datos.Reservar(idAgenda, fechaHora, idPaciente);
            if (id == 0) throw new Exception("Ese horario ya fue tomado por otra persona. Elegí otro.");
            espera.QuitarDePaciente(idPaciente, agenda.IdEspecialidad);
            return datos.Obtener(id)!;
        }

        public void Confirmar(int idTurno, int? idTitular)
        {
            var t = datos.Obtener(idTurno) ?? throw new Exception("El turno no existe.");
            if (idTitular.HasValue && !Pertenece(t, idTitular.Value)) throw new Exception("No podés modificar ese turno.");

            if (!t.PuedeConfirmar)
                throw new Exception("La confirmación todavía no está habilitada (se abre 48 horas antes de la cita) o el plazo ya venció.");

            if (!datos.CambiarEstado(idTurno, "Confirmado", new[] { "PendienteConfirmacion" }, "Confirmado por el paciente", false))
                throw new Exception("El turno ya no se puede confirmar (venció o cambió de estado).");
        }

        public void Cancelar(int idTurno, int? idTitular, string motivo = "Cancelado")
        {
            var t = datos.Obtener(idTurno) ?? throw new Exception("El turno no existe.");
            if (idTitular.HasValue && !Pertenece(t, idTitular.Value)) throw new Exception("No podés modificar ese turno.");
            if (!datos.CambiarEstado(idTurno, "Cancelado", new[] { "PendienteConfirmacion", "Confirmado" }, motivo, true))
                throw new Exception("El turno no se puede cancelar en su estado actual.");
            if (t.FechaHora > DateTime.Now)
                listaEspera.ReasignarPrimerPacienteEnEspera(t.IdAgenda, t.FechaHora, t.IdPaciente ?? 0);
        }

        /// <summary>Recepción: presente / ausente.</summary>
        public void MarcarAsistencia(int idTurno, bool presente)
        {
            var t = datos.Obtener(idTurno) ?? throw new Exception("El turno no existe.");
            if (t.FechaHora.Date > DateTime.Today) throw new Exception("Solo se puede registrar la asistencia el día del turno.");
            string nuevo = presente ? "Presente" : "Ausente";
            if (!datos.CambiarEstado(idTurno, nuevo, new[] { "PendienteConfirmacion", "Confirmado", "Presente", "Ausente" }, "Asistencia registrada por recepción", false))
                throw new Exception("El turno no admite ese cambio de estado.");
        }

        /// <summary>Médico: pasa el paciente a "En atención".</summary>
        public void IniciarAtencion(int idTurno)
        {
            if (!datos.CambiarEstado(idTurno, "EnAtencion", new[] { "Presente" }, "Atención iniciada", false))
                throw new Exception("El paciente tiene que estar presente para iniciar la atención.");
        }
    }
}
