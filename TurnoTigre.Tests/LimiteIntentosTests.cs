using Negocio;
using Xunit;

namespace TurnoTigre.Tests
{
    public class LimiteIntentosTests
    {
        private DateTimeOffset ahora = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        private readonly TimeSpan ventana = TimeSpan.FromMinutes(15);

        private LimiteIntentos Limite(int max = 5) => new LimiteIntentos(max, ventana, () => ahora);

        private static void Fallar(LimiteIntentos l, string clave, int veces)
        {
            for (int i = 0; i < veces; i++) l.RegistrarFallo(clave);
        }

        [Fact]
        public void SinFallosNoHayBloqueo()
        {
            Assert.Null(Limite().TiempoRestante("a"));
        }

        [Fact]
        public void BloqueaAlLlegarAlMaximoYNoAntes()
        {
            var l = Limite(5);
            Fallar(l, "a", 4);
            Assert.Null(l.TiempoRestante("a"));

            l.RegistrarFallo("a");

            Assert.Equal(ventana, l.TiempoRestante("a"));
        }

        [Fact]
        public void ElTiempoRestanteVaBajando()
        {
            var l = Limite(3);
            Fallar(l, "a", 3);

            ahora += TimeSpan.FromMinutes(10);

            Assert.Equal(TimeSpan.FromMinutes(5), l.TiempoRestante("a"));
        }

        [Fact]
        public void SeDesbloqueaCuandoTerminaLaVentana()
        {
            var l = Limite(3);
            Fallar(l, "a", 3);

            ahora += ventana;

            Assert.Null(l.TiempoRestante("a"));
        }

        [Fact]
        public void DespuesDeDesbloquearseEmpiezaDeCero()
        {
            var l = Limite(3);
            Fallar(l, "a", 3);
            ahora += ventana;
            Assert.Null(l.TiempoRestante("a"));

            Fallar(l, "a", 2);

            Assert.Null(l.TiempoRestante("a"));
        }

        [Fact]
        public void LosFallosViejosNoSeAcumulan()
        {
            var l = Limite(3);
            Fallar(l, "a", 2);

            ahora += ventana;       // la ventana de esos fallos ya terminó
            l.RegistrarFallo("a");

            Assert.Null(l.TiempoRestante("a"));
        }

        [Fact]
        public void UnIngresoCorrectoReiniciaElContador()
        {
            var l = Limite(3);
            Fallar(l, "a", 2);

            l.Reiniciar("a");
            Fallar(l, "a", 2);

            Assert.Null(l.TiempoRestante("a"));
        }

        [Fact]
        public void LasClavesSonIndependientes()
        {
            var l = Limite(3);
            Fallar(l, "a", 3);

            Assert.NotNull(l.TiempoRestante("a"));
            Assert.Null(l.TiempoRestante("b"));
        }

        [Fact]
        public void UnFalloMasEstandoBloqueadaNoLaLibera()
        {
            // Un bloqueo se levanta solo con el paso del tiempo.
            var l = Limite(3);
            Fallar(l, "a", 3);

            l.RegistrarFallo("a");   // otro intento mientras está bloqueada no la libera

            Assert.NotNull(l.TiempoRestante("a"));
        }
    }
}
