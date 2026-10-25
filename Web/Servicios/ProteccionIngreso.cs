using Negocio;

namespace Web.Servicios
{
    // Limita los intentos fallidos en /Ingresar y /IngresarPersonal: 5 por cuenta y 30 por IP, cada 15 minutos.
    // Si la aplicación queda detrás de un proxy inverso hay que configurar los encabezados reenviados (UseForwardedHeaders);
    // si no, todas las personas parecen tener la misma IP y comparten el cupo de 30.
    public class ProteccionIngreso
    {
        private static readonly TimeSpan Ventana = TimeSpan.FromMinutes(15);
        private readonly LimiteIntentos porCuenta = new(5, Ventana);
        private readonly LimiteIntentos porIp = new(30, Ventana);

        /// <param name="cuenta">Identificador con el que se intenta ingresar (correo o usuario); se normaliza acá.</param>
        public string? Bloqueo(string tipo, string cuenta, HttpContext http)
        {
            var restante = Mayor(porCuenta.TiempoRestante(Clave(tipo, cuenta)), porIp.TiempoRestante(Ip(http)));
            if (restante is null) return null;
            int minutos = (int)Math.Ceiling(restante.Value.TotalMinutes);
            return $"Hubo demasiados intentos fallidos. Probá de nuevo en {minutos} {(minutos == 1 ? "minuto" : "minutos")}.";
        }

        public void Fallo(string tipo, string cuenta, HttpContext http)
        {
            porCuenta.RegistrarFallo(Clave(tipo, cuenta));
            porIp.RegistrarFallo(Ip(http));
        }

        public void Exito(string tipo, string cuenta) => porCuenta.Reiniciar(Clave(tipo, cuenta));

        private static string Clave(string tipo, string cuenta) => tipo + ":" + (cuenta ?? "").Trim().ToLowerInvariant();
        private static string Ip(HttpContext http) => "ip:" + (http.Connection.RemoteIpAddress?.ToString() ?? "desconocida");
        private static TimeSpan? Mayor(TimeSpan? a, TimeSpan? b) => a is null ? b : b is null ? a : (a > b ? a : b);
    }
}
