using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class ListaEsperaNegocio
    {
        private readonly ListaEsperaDatos datos = new ListaEsperaDatos();
        private readonly PacienteDatos pacientes = new PacienteDatos();
        private readonly TurnoDatos turnos = new TurnoDatos();
        private readonly AgendaDatos agendas = new AgendaDatos();

        public List<EsperaVista> DeGrupoFamiliar(int idTitular) => datos.DeGrupoFamiliar(idTitular);

        public void Inscribir(int idTitular, int idPaciente, int idEspecialidad, int idCentro, string franja)
        {
            var ficha = pacientes.Obtener(idPaciente) ?? throw new Exception("El paciente no existe.");
            if (idPaciente != idTitular && ficha.IdTitular != idTitular) throw new Exception("Solo podés anotar a tu grupo familiar.");
            if (idEspecialidad <= 0 || idCentro <= 0) throw new Exception("Elegí un centro y una especialidad.");
            if (franja != "Mañana" && franja != "Tarde" && franja != "Indistinto") franja = "Indistinto";
            if (turnos.TieneTurnoActivoEnEspecialidad(idPaciente, idEspecialidad))
                throw new Exception("Ya tiene un turno activo en esa especialidad.");
            if (datos.Inscripto(idPaciente, idEspecialidad, idCentro))
                throw new Exception("Ya está anotado en la lista de espera de esa especialidad y centro.");
            if (HayTurnosLibres(idCentro, idEspecialidad, franja))
                throw new Exception("Hay turnos disponibles en esa especialidad y centro. Reservá uno directamente en lugar de anotarte en la lista de espera.");
            datos.Inscribir(idPaciente, idEspecialidad, idCentro, franja);
        }

        public void Quitar(int idTitular, int idEspera)
        {
            var e = datos.Obtener(idEspera) ?? throw new Exception("La inscripción no existe.");
            var ficha = pacientes.Obtener(e.IdPaciente);
            if (ficha == null || (ficha.IdPaciente != idTitular && ficha.IdTitular != idTitular)) throw new Exception("No podés modificar esa inscripción.");
            datos.Quitar(idEspera);
        }

        /// <summary>RF5: ante un cupo liberado, se lo asigna al primer inscripto compatible. Devuelve true si se reasignó.</summary>
        public bool ReasignarPrimerPacienteEnEspera(int idAgenda, DateTime fechaHora, int idPacienteExcluido = 0)
        {
            try { return datos.ReasignarCupo(idAgenda, fechaHora, idPacienteExcluido) > 0; }
            catch (Exception ex) // no debe impedir cancelar ni mostrar la pantalla
            {
                RegistroErrores.Informar($"Reasignar el cupo de la agenda {idAgenda} ({fechaHora:g})", ex);
                return false;
            }
        }

        public List<NotificacionVista> Notificaciones(int idUsuario) => datos.NotificacionesDeUsuario(idUsuario);
        public void MarcarLeidas(int idUsuario) => datos.MarcarLeidas(idUsuario);

        /// <summary>RF5: ofrece los cupos libres de una agenda recién creada o editada a la lista de espera. Devuelve cuántos se asignaron.</summary>
        public int AsignarCuposLibres(int idAgenda)
        {
            var agenda = agendas.Obtener(idAgenda);
            if (agenda == null) return 0;

            int asignados = 0;
            foreach (var fechaHora in turnos.HorariosLibres(idAgenda))
            {
                if (!datos.HayInscriptos(agenda.IdEspecialidad, agenda.IdCentro)) break;
                while (ReasignarPrimerPacienteEnEspera(idAgenda, fechaHora))
                {
                    asignados++;
                }
            }
            return asignados;
        }

        /// <summary>Si hay turnos libres en ese centro y especialidad que le sirvan a la franja elegida.</summary>
        private bool HayTurnosLibres(int idCentro, int idEspecialidad, string franja)
        {
            var libres = turnos.Disponibles(idCentro, idEspecialidad, DateTime.Now, DateTime.Now.AddYears(1));
            foreach (var s in libres)
            {
                if (franja == "Indistinto") return true;
                string franjaTurno = s.FechaHora.Hour < 13 ? "Mañana" : "Tarde";
                if (franjaTurno == franja) return true;
            }
            return false;
        }

    }
}
