using Dominio;
using Xunit;

namespace TurnoTigre.Tests
{
    public class TurnoVistaTests
    {
        private static TurnoVista Turno(string estado, DateTime fechaHora, DateTime? limite = null) =>
            new TurnoVista { Estado = estado, FechaHora = fechaHora, LimiteConfirmacion = limite };

        // --- Turno normal (sin límite explícito): se confirma de 48 a 24 hs antes ---

        [Theory]
        [InlineData(47, true)]
        [InlineData(30, true)]
        [InlineData(25, true)]
        [InlineData(23, false)]  // ya pasó el límite de 24 hs
        [InlineData(49, false)]  // todavía no se abrió la ventana
        public void Normal_VentanaDe48A24Hs(int horasParaLaCita, bool esperado)
        {
            var t = Turno("PendienteConfirmacion", DateTime.Now.AddHours(horasParaLaCita));
            Assert.Equal(esperado, t.EnVentanaConfirmacion);
        }

        // --- Turno con límite (el normal lo trae como cita - 24 hs) ---

        [Fact]
        public void ConLimite_NormalEquivaleAlCalculoPorCita()
        {
            var cita = DateTime.Now.AddHours(36);
            var t = Turno("PendienteConfirmacion", cita, cita.AddHours(-24));
            Assert.True(t.EnVentanaConfirmacion);
        }

        [Fact]
        public void ConLimite_ReasignadoSePuedeConfirmarDeInmediato()
        {
            // Cita en 5 días, reasignado recién: límite = ahora + 24 hs. La cita está lejos de la ventana de 48 hs.
            var t = Turno("PendienteConfirmacion", DateTime.Now.AddDays(5), DateTime.Now.AddHours(24));
            Assert.True(t.EnVentanaConfirmacion);
            Assert.True(t.PuedeConfirmar);
        }

        [Fact]
        public void ConLimite_ReasignadoCercaDeLaCitaUsaCitaMenosUnaHora()
        {
            // Cita en 10 hs: límite = cita - 1 h = en 9 hs. Todavía se puede confirmar.
            var cita = DateTime.Now.AddHours(10);
            var t = Turno("PendienteConfirmacion", cita, cita.AddHours(-1));
            Assert.True(t.EnVentanaConfirmacion);
        }

        [Fact]
        public void ConLimite_VencidoNoSePuedeConfirmar()
        {
            var t = Turno("PendienteConfirmacion", DateTime.Now.AddDays(2), DateTime.Now.AddMinutes(-1));
            Assert.False(t.EnVentanaConfirmacion);
            Assert.False(t.PuedeConfirmar);
        }

        [Fact]
        public void ConLimite_AunNoAbiertaLaVentana()
        {
            // El límite está a más de 24 hs: la ventana (límite - 24 hs) todavía no abrió.
            var t = Turno("PendienteConfirmacion", DateTime.Now.AddDays(10), DateTime.Now.AddDays(3));
            Assert.False(t.EnVentanaConfirmacion);
        }

        // --- PuedeConfirmar / PuedeCancelar según estado ---

        [Theory]
        [InlineData("Confirmado")]
        [InlineData("Disponible")]
        [InlineData("Cancelado")]
        [InlineData("Vencido")]
        public void PuedeConfirmar_SoloDesdePendienteConfirmacion(string estado)
        {
            var t = Turno(estado, DateTime.Now.AddHours(30), DateTime.Now.AddHours(6));
            Assert.False(t.PuedeConfirmar);
        }

        [Theory]
        [InlineData("PendienteConfirmacion", true)]
        [InlineData("Confirmado", true)]
        [InlineData("Disponible", false)]
        [InlineData("Atendido", false)]
        [InlineData("Cancelado", false)]
        public void PuedeCancelar_SoloActivosYFuturos(string estado, bool esperado)
        {
            Assert.Equal(esperado, Turno(estado, DateTime.Now.AddDays(2)).PuedeCancelar);
        }

        [Fact]
        public void PuedeCancelar_NoSiLaCitaYaPaso()
        {
            Assert.False(Turno("Confirmado", DateTime.Now.AddHours(-1)).PuedeCancelar);
        }

        [Fact]
        public void EstadoVisible_PendienteSeMuestraComoReservado()
        {
            Assert.Equal("Reservado", Turno("PendienteConfirmacion", DateTime.Now).EstadoVisible);
            Assert.Equal("En atención", Turno("EnAtencion", DateTime.Now).EstadoVisible);
        }
    }
}
