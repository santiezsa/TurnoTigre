using Dominio;

namespace Negocio
{
    /// <summary>
    /// Tipos de aviso de turno (columna NOTIFICACION.tipo_notificacion) y los textos que recibe el paciente.
    /// Los de reasignación, recordatorio y vencimiento se arman en SQL (ListaEsperaDatos, TurnoDatos); acá están los que dispara una acción.
    /// </summary>
    public static class AvisosTurno
    {
        public const string Reasignado = "TurnoReasignado";
        public const string Recordatorio = "RecordatorioConfirmacion";
        public const string Reservado = "TurnoReservado";
        public const string Confirmado = "TurnoConfirmado";
        public const string Cancelado = "TurnoCancelado";
        public const string Vencido = "TurnoVencido";

        /// <summary>RF3: detalle del turno reservado (profesional, centro, especialidad, fecha y hora) y cuándo confirmarlo.</summary>
        public static string TextoReservado(TurnoVista t)
        {
            string texto = $"Se reservó el turno de {Detalle(t)}.";
            if (t.Estado == "Confirmado")
                return texto + " Como falta menos de un día, ya quedó confirmado.";
            if (t.LimiteConfirmacion is DateTime limite)
            {
                var apertura = limite.AddHours(-24);
                texto += DateTime.Now >= apertura
                    ? $" Ya podés confirmarlo: tenés tiempo hasta el {Momento(limite)}."
                    : $" Vas a poder confirmarlo desde el {Momento(apertura)} hasta el {Momento(limite)}; si no, se libera.";
            }
            return texto;
        }

        public static string TextoConfirmado(TurnoVista t) =>
            $"Quedó confirmado el turno de {Detalle(t)}. Si al final no podés ir, cancelalo así el horario le sirve a otra persona.";

        /// <param name="porAdministracion">true si lo canceló el centro de salud y no el propio paciente.</param>
        public static string TextoCancelado(TurnoVista t, bool porAdministracion) => porAdministracion
            ? $"El centro de salud canceló el turno de {Detalle(t)}. Si todavía lo necesitás, podés sacar otro turno desde la app."
            : $"Se canceló el turno de {Detalle(t)}.";

        /// <summary>"Nombre Apellido: Especialidad con Profesional en Centro el dd/MM a las HH:mm hs".</summary>
        private static string Detalle(TurnoVista t) =>
            $"{t.PacienteNombre}: {t.Especialidad} con {NombreNatural(t.Profesional)} en {t.Centro} el {t.FechaHora:dd'/'MM} a las {t.FechaHora:HH':'mm} hs";

        private static string Momento(DateTime d) => $"{d:dd'/'MM} a las {d:HH':'mm} hs";

        /// <summary>"Apellido, Nombre" -> "Nombre Apellido".</summary>
        private static string NombreNatural(string apellidoNombre)
        {
            var p = apellidoNombre.Split(", ", 2);
            return p.Length == 2 ? p[1] + " " + p[0] : apellidoNombre;
        }
    }
}
