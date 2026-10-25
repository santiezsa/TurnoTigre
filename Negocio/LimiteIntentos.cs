namespace Negocio
{
    /// <summary>
    /// Cuenta fallos por clave (una cuenta, una IP) y bloquea cuando se llega al máximo.
    /// Los fallos cuentan dentro de una ventana que arranca con el primero; al llegar al máximo la clave queda bloqueada por una ventana entera.
    /// Todo en memoria: se reinicia con la aplicación.
    /// </summary>
    public class LimiteIntentos
    {
        private class Registro
        {
            public int Fallos;
            public DateTimeOffset Inicio;
            public DateTimeOffset? BloqueadoHasta;
        }

        private const int MaxRegistrosAntesDeLimpiar = 10000;

        private readonly int maxFallos;
        private readonly TimeSpan ventana;
        private readonly Func<DateTimeOffset> reloj;
        private readonly Dictionary<string, Registro> registros = new();
        private readonly object candado = new();

        public LimiteIntentos(int maxFallos, TimeSpan ventana, Func<DateTimeOffset>? reloj = null)
        {
            this.maxFallos = maxFallos;
            this.ventana = ventana;
            this.reloj = reloj ?? (() => DateTimeOffset.UtcNow);
        }

        /// <summary>Cuánto falta para que se pueda volver a intentar, o null si la clave no está bloqueada.</summary>
        public TimeSpan? TiempoRestante(string clave)
        {
            lock (candado)
            {
                if (!registros.TryGetValue(clave, out var r) || r.BloqueadoHasta is null) return null;
                var restante = r.BloqueadoHasta.Value - reloj();
                if (restante > TimeSpan.Zero) return restante;
                registros.Remove(clave);   // el bloqueo ya terminó
                return null;
            }
        }

        public void RegistrarFallo(string clave)
        {
            lock (candado)
            {
                var ahora = reloj();
                if (registros.Count > MaxRegistrosAntesDeLimpiar) Limpiar(ahora);

                if (!registros.TryGetValue(clave, out var r) || ahora - r.Inicio >= ventana)
                    registros[clave] = r = new Registro { Inicio = ahora };

                r.Fallos++;
                if (r.Fallos >= maxFallos) r.BloqueadoHasta = ahora + ventana;
            }
        }

        /// <summary>Ingreso correcto: se olvidan los fallos anteriores.</summary>
        public void Reiniciar(string clave)
        {
            lock (candado) registros.Remove(clave);
        }

        private void Limpiar(DateTimeOffset ahora)
        {
            foreach (var clave in registros.Where(kv => ahora - kv.Value.Inicio >= ventana && (kv.Value.BloqueadoHasta is null || kv.Value.BloqueadoHasta <= ahora)).Select(kv => kv.Key).ToList())
                registros.Remove(clave);
        }
    }
}
