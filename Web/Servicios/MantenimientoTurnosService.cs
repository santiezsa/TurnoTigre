using Negocio;

namespace Web.Servicios
{
    // Corre el motor del sistema (vencimientos, reasignación RF5 y recordatorios RF4) cada pocos minutos,
    // sin depender de que alguien abra una pantalla, y después manda por mail los avisos que quedaron encolados.
    // Intervalo en appsettings: "Mantenimiento:IntervaloMinutos" (5 por defecto).
    public class MantenimientoTurnosService : BackgroundService
    {
        private readonly ILogger<MantenimientoTurnosService> logger;
        private readonly CorreoService correo;
        private readonly TimeSpan intervalo;

        public MantenimientoTurnosService(IConfiguration config, CorreoService correo, ILogger<MantenimientoTurnosService> logger)
        {
            this.logger = logger;
            this.correo = correo;
            int minutos = int.TryParse(config["Mantenimiento:IntervaloMinutos"], out int m) && m > 0 ? m : 5;
            intervalo = TimeSpan.FromMinutes(minutos);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            logger.LogInformation("Mantenimiento de turnos iniciado (cada {Intervalo}).", intervalo);
            using var timer = new PeriodicTimer(intervalo);
            do
            {
                try
                {
                    var r = await Task.Run(() => new TurnoNegocio().Mantener(), stoppingToken);
                    if (r.HuboCambios)
                        logger.LogInformation("Mantenimiento de turnos: {Vencidos} vencidos, {Reasignados} reasignados, {Recordatorios} recordatorios.", r.Vencidos, r.Reasignados, r.Recordatorios);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Falló el mantenimiento de turnos; se reintenta en {Intervalo}.", intervalo);
                }

                try { await EnviarCorreosPendientesAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex) { logger.LogError(ex, "Falló el envío de avisos por mail; se reintenta en {Intervalo}.", intervalo); }
            }
            while (await WaitAsync(timer, stoppingToken));
        }

        // Sin SMTP configurado no se toca la cola: los avisos quedan pendientes (y se descartan solos al vencer su plazo).
        // Si un envío falla, queda pendiente y se reintenta en la próxima pasada.
        private async Task EnviarCorreosPendientesAsync(CancellationToken ct)
        {
            if (!correo.EstaConfigurado) return;
            var negocio = new NotificacionNegocio();
            var pendientes = await Task.Run(() => negocio.CorreosPendientes(), ct);
            int enviados = 0;
            foreach (var c in pendientes)
            {
                ct.ThrowIfCancellationRequested();
                if (!await correo.EnviarAvisoTurnoAsync(c.Destino, c.Tipo, c.Mensaje)) continue;
                negocio.MarcarCorreoEnviado(c.IdNotificacion);
                enviados++;
            }
            if (pendientes.Count > 0)
                logger.LogInformation("Avisos por mail: {Enviados} enviados, {Fallidos} para reintentar.", enviados, pendientes.Count - enviados);
        }

        private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken ct)
        {
            try { return await timer.WaitForNextTickAsync(ct); }
            catch (OperationCanceledException) { return false; }
        }
    }
}
