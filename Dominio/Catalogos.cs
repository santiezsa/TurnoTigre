namespace Dominio
{
    public static class Catalogos
    {
        public static readonly string[] LocalidadesTigre =
        {
            "Benavídez", "Delta de Tigre", "Dique Luján", "Don Torcuato", "El Talar", "General Pacheco",
            "Nordelta", "Ricardo Rojas", "Rincón de Milberg", "Tigre", "Troncos del Talar", "Villa La Ñata"
        };
        public static readonly string[] Parentescos = { "Hijo/a", "Cónyuge", "Padre/Madre", "Hermano/a", "Otro" };
        public static readonly string[] GruposSanguineos = { "A+", "A-", "B+", "B-", "AB+", "AB-", "0+", "0-" };
        public static readonly string[] Sexos = { "Femenino", "Masculino", "X" };
        public const int MaxFamiliares = 6;

        public static bool LocalidadValida(string? l) => LocalidadesTigre.Any(x => string.Equals(x, l?.Trim(), StringComparison.OrdinalIgnoreCase));
        public static string NormalizarLocalidad(string l) => LocalidadesTigre.First(x => string.Equals(x, l.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>"juan CARLOS" -> "Juan Carlos"</summary>
        public static string Normalizar(string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            var partes = texto.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => char.ToUpper(p[0]) + p[1..].ToLower());
            return string.Join(' ', partes);
        }
    }
}
