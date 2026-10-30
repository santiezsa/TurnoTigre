using AccesoDatos;
using System.Security.Cryptography;
using Xunit;

namespace TurnoTigre.Tests
{
    public class CifradorTests
    {
        /// <summary>Clave fija para las pruebas (la usan también las de integración; nunca usarla fuera de los tests).</summary>
        public const string ClaveDePrueba = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=";

        private readonly Cifrador c = new Cifrador(ClaveDePrueba);

        [Fact]
        public void Texto_IdaYVueltaConAcentos()
        {
            string original = "Alergia a la penicilina; diagnóstico: migraña crónica";
            Assert.Equal(original, c.Descifrar(c.Cifrar(original)));
        }

        [Fact]
        public void Texto_LoGuardadoNoMuestraElOriginal()
        {
            string cifrado = c.Cifrar("Penicilina")!;
            Assert.StartsWith("enc:v1:", cifrado);
            Assert.DoesNotContain("Penicilina", cifrado);
        }

        [Fact]
        public void Texto_ElMismoTextoSeCifraDistintoCadaVez()
        {
            Assert.NotEqual(c.Cifrar("Diabetes"), c.Cifrar("Diabetes"));    // así no se puede adivinar comparando
        }

        [Fact]
        public void Texto_UnDatoViejoSinCifrarSeDevuelveTalCual()
        {
            Assert.Equal("Polen", c.Descifrar("Polen"));
        }

        [Fact]
        public void Texto_VacioONullQuedaIgual()
        {
            Assert.Null(c.Cifrar(null));
            Assert.Equal("", c.Cifrar(""));
            Assert.Equal("", c.Descifrar(null));
        }

        [Fact]
        public void Texto_NoSeCifraDosVeces()
        {
            string una = c.Cifrar("Asma")!;
            Assert.Equal(una, c.Cifrar(una));
        }

        [Fact]
        public void Texto_SiAlguienLoAlteraSeDetecta()
        {
            string cifrado = c.Cifrar("Hipertensión")!;
            var bytes = Convert.FromBase64String(cifrado["enc:v1:".Length..]);
            bytes[^1] ^= 0x01;                                                  // cambia un bit del contenido
            string alterado = "enc:v1:" + Convert.ToBase64String(bytes);

            Assert.ThrowsAny<CryptographicException>(() => c.Descifrar(alterado));
        }

        [Fact]
        public void Texto_ConOtraClaveNoSePuedeLeer()
        {
            var otra = new Cifrador(Convert.ToBase64String(new byte[32]));
            string cifrado = c.Cifrar("Hipertensión")!;

            Assert.ThrowsAny<CryptographicException>(() => otra.Descifrar(cifrado));
        }

        [Fact]
        public void Archivo_IdaYVueltaConMarca()
        {
            byte[] original = { 0x25, 0x50, 0x44, 0x46, 1, 2, 3, 4, 5 };
            byte[] cifrado = c.CifrarBytes(original);

            Assert.True(Cifrador.EstaCifrado(cifrado));
            Assert.NotEqual(original, cifrado);
            Assert.Equal(original, c.DescifrarBytes(cifrado));
            Assert.Equal(cifrado, c.CifrarBytes(cifrado));                      // no se cifra dos veces
        }

        [Fact]
        public void Archivo_UnArchivoViejoSinCifrarSeDevuelveTalCual()
        {
            byte[] viejo = { 1, 2, 3 };
            Assert.Equal(viejo, c.DescifrarBytes(viejo));
        }

        [Theory]
        [InlineData("no es base64!")]
        [InlineData("AAECAwQFBgcICQoLDA0ODw==")]                                // 16 bytes: corta
        public void Clave_InvalidaNoSeAcepta(string clave)
        {
            Assert.Throws<ArgumentException>(() => new Cifrador(clave));
        }
    }
}
