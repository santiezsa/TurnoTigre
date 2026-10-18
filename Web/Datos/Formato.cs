using System.Text;

namespace Web.Datos
{
    public static class Formato
    {
        public static readonly string[] MesesLargo = { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };
        public static readonly string[] DiasLargo = { "domingo", "lunes", "martes", "miércoles", "jueves", "viernes", "sábado" };
        public static readonly string[] DiasCorto = { "Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb" };

        public static string FechaLarga(DateTime d)
        {
            string dia = DiasLargo[(int)d.DayOfWeek];
            return $"{char.ToUpper(dia[0])}{dia[1..]} {d.Day} de {MesesLargo[d.Month - 1]}";
        }

        /// <summary>Clase CSS de un estado ("En atención" -> "enatencion").</summary>
        public static string ClaseEstado(string estado) =>
            estado.ToLowerInvariant().Replace(" ", "").Replace("ó", "o");

        /// <summary>Interpreta una fecha yyyy-MM-dd de la URL; si no es válida devuelve el valor por defecto.</summary>
        public static DateTime Fecha(string? texto, DateTime? defecto = null) =>
            DateTime.TryParse(texto, out var f) ? f.Date : (defecto ?? DateTime.Today);
    }
}
