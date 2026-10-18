using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class ListaEsperaNegocio
    {
        private readonly ListaEsperaDatos datos = new ListaEsperaDatos();
        private readonly PacienteDatos pacientes = new PacienteDatos();
        private readonly TurnoDatos turnos = new TurnoDatos();

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
            datos.Inscribir(idPaciente, idEspecialidad, idCentro, franja);
        }

        public void Quitar(int idTitular, int idEspera)
        {
            var e = datos.Obtener(idEspera) ?? throw new Exception("La inscripción no existe.");
            var ficha = pacientes.Obtener(e.IdPaciente);
            if (ficha == null || (ficha.IdPaciente != idTitular && ficha.IdTitular != idTitular)) throw new Exception("No podés modificar esa inscripción.");
            datos.Quitar(idEspera);
        }

        public List<NotificacionVista> Notificaciones(int idUsuario) => datos.NotificacionesDeUsuario(idUsuario);
        public void MarcarLeidas(int idUsuario) => datos.MarcarLeidas(idUsuario);
    }
}
