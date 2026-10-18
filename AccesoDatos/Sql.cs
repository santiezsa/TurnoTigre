using Microsoft.Data.SqlClient;

namespace AccesoDatos
{
    /// <summary>Atajos para consultas con parámetros sobre ConexionDB.</summary>
    public static class Sql
    {
        public static List<T> Lista<T>(string consulta, Func<SqlDataReader, T> mapear, params (string, object?)[] parametros)
        {
            var datos = new ConexionDB();
            try
            {
                datos.setearConsulta(consulta);
                foreach (var (n, v) in parametros) datos.setearParametro(n, v);
                datos.ejecutarLectura();
                var lista = new List<T>();
                while (datos.Lector.Read()) lista.Add(mapear(datos.Lector));
                return lista;
            }
            finally { datos.cerrarConexion(); }
        }

        public static T? Uno<T>(string consulta, Func<SqlDataReader, T> mapear, params (string, object?)[] parametros) where T : class
            => Lista(consulta, mapear, parametros).FirstOrDefault();

        public static void Ejecutar(string consulta, params (string, object?)[] parametros)
        {
            var datos = new ConexionDB();
            try
            {
                datos.setearConsulta(consulta);
                foreach (var (n, v) in parametros) datos.setearParametro(n, v);
                datos.ejecutarAccion();
            }
            finally { datos.cerrarConexion(); }
        }

        /// <summary>Devuelve el primer valor numérico de la consulta (0 si no hay resultado).</summary>
        public static int Numero(string consulta, params (string, object?)[] parametros)
        {
            var datos = new ConexionDB();
            try
            {
                datos.setearConsulta(consulta);
                foreach (var (n, v) in parametros) datos.setearParametro(n, v);
                return datos.obtenerEscalarEntero();
            }
            finally { datos.cerrarConexion(); }
        }

        public static bool Existe(string consulta, params (string, object?)[] parametros) => Numero(consulta, parametros) > 0;

        // ---- lectura segura de columnas ----
        public static string Txt(SqlDataReader r, string col) => r[col] is DBNull or null ? "" : Convert.ToString(r[col]) ?? "";
        public static int Ent(SqlDataReader r, string col) => r[col] is DBNull or null ? 0 : Convert.ToInt32(r[col]);
        public static int? EntN(SqlDataReader r, string col) => r[col] is DBNull or null ? null : Convert.ToInt32(r[col]);
        public static bool Bool(SqlDataReader r, string col) => r[col] is not DBNull and not null && Convert.ToBoolean(r[col]);
        public static DateTime Fecha(SqlDataReader r, string col) => r[col] is DBNull or null ? DateTime.MinValue : Convert.ToDateTime(r[col]);
        public static DateTime? FechaN(SqlDataReader r, string col) => r[col] is DBNull or null ? null : Convert.ToDateTime(r[col]);
        public static decimal? DecN(SqlDataReader r, string col) => r[col] is DBNull or null ? null : Convert.ToDecimal(r[col]);

        public static string F(DateTime d) => d.ToString("yyyyMMdd HH:mm:ss");
    }
}
