using System.Security.Cryptography;
using System.Text;

namespace AccesoDatos
{
    /// <summary>
    /// Cifrado de los datos sensibles (SRS: "cifrado de los datos sensibles"): AES-256-GCM, que además de ocultar el contenido detecta si alguien lo alteró.
    /// Texto: se guarda como "enc:v1:" + base64(nonce | tag | cifrado). Archivos: bytes "TTENC1" + nonce + tag + cifrado.
    /// Lo que no tiene esa marca se considera dato viejo sin cifrar y se devuelve tal cual (así las bases anteriores siguen andando).
    /// La clave se configura una vez al arrancar (<see cref="Configurar"/>); si se pierde, los datos cifrados no se pueden recuperar.
    /// </summary>
    public class Cifrador
    {
        /// <summary>Clave pública de desarrollo: solo para correr en tu máquina. En producción la aplicación no arranca con esta clave.</summary>
        public const string ClaveDeDesarrollo = "/CO3XcqMDBs1BO2cMG6EJQyapENECuda6PRbQXDQWhA=";

        private const string Prefijo = "enc:v1:";
        private static readonly byte[] Marca = Encoding.ASCII.GetBytes("TTENC1");
        private const int LargoNonce = 12, LargoTag = 16;

        private static Cifrador? actual;

        /// <summary>El cifrador configurado al arrancar. Falla si no se configuró: nunca se guardan datos sensibles sin cifrar por olvido.</summary>
        public static Cifrador Actual => actual ?? throw new InvalidOperationException("No se configuró la clave de cifrado (Cifrado:Clave).");

        /// <param name="claveBase64">32 bytes en base64.</param>
        public static void Configurar(string claveBase64) => actual = new Cifrador(claveBase64);

        private readonly byte[] clave;

        public Cifrador(string claveBase64)
        {
            byte[] k;
            try { k = Convert.FromBase64String(claveBase64 ?? ""); }
            catch (FormatException) { throw new ArgumentException("La clave de cifrado tiene que estar en base64."); }
            if (k.Length != 32) throw new ArgumentException("La clave de cifrado tiene que tener 32 bytes (256 bits).");
            clave = k;
        }

        public static bool EstaCifrado(string? texto) => texto != null && texto.StartsWith(Prefijo, StringComparison.Ordinal);
        public static bool EstaCifrado(byte[]? datos) => datos != null && datos.Length >= Marca.Length && datos.AsSpan(0, Marca.Length).SequenceEqual(Marca);

        /// <summary>Cifra un texto. Vacío o null queda igual; lo que ya está cifrado no se vuelve a cifrar.</summary>
        public string? Cifrar(string? texto)
        {
            if (string.IsNullOrEmpty(texto) || EstaCifrado(texto)) return texto;
            return Prefijo + Convert.ToBase64String(Sellar(Encoding.UTF8.GetBytes(texto)));
        }

        /// <summary>Descifra un texto; si no tiene la marca de cifrado, es un dato viejo y se devuelve tal cual.</summary>
        public string Descifrar(string? texto)
        {
            if (!EstaCifrado(texto)) return texto ?? "";
            return Encoding.UTF8.GetString(Abrir(Convert.FromBase64String(texto![Prefijo.Length..])));
        }

        public byte[] CifrarBytes(byte[] datos)
        {
            if (EstaCifrado(datos)) return datos;
            var sellado = Sellar(datos);
            var r = new byte[Marca.Length + sellado.Length];
            Marca.CopyTo(r, 0);
            sellado.CopyTo(r, Marca.Length);
            return r;
        }

        public byte[] DescifrarBytes(byte[] datos) => EstaCifrado(datos) ? Abrir(datos.AsSpan(Marca.Length).ToArray()) : datos;

        // nonce (12) | tag (16) | cifrado
        private byte[] Sellar(byte[] claro)
        {
            var r = new byte[LargoNonce + LargoTag + claro.Length];
            var nonce = r.AsSpan(0, LargoNonce);
            RandomNumberGenerator.Fill(nonce);
            using var aes = new AesGcm(clave, LargoTag);
            aes.Encrypt(nonce, claro, r.AsSpan(LargoNonce + LargoTag), r.AsSpan(LargoNonce, LargoTag));
            return r;
        }

        /// <exception cref="CryptographicException">Si el dato fue alterado o se cifró con otra clave.</exception>
        private byte[] Abrir(byte[] sellado)
        {
            if (sellado.Length < LargoNonce + LargoTag) throw new CryptographicException("El dato cifrado está incompleto.");
            var claro = new byte[sellado.Length - LargoNonce - LargoTag];
            using var aes = new AesGcm(clave, LargoTag);
            aes.Decrypt(sellado.AsSpan(0, LargoNonce), sellado.AsSpan(LargoNonce + LargoTag), sellado.AsSpan(LargoNonce, LargoTag), claro);
            return claro;
        }
    }
}
