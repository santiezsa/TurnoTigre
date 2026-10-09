using Microsoft.Data.SqlClient;
using System.Data;

namespace AccesoDatos
{
    public class ConexionDB
    {
        private SqlConnection conexion;
        private SqlCommand comando;
        private SqlDataReader lector;
        public SqlDataReader Lector
        {
            get { return lector; }
        }

        public ConexionDB()
        {
            //PONER LA CONTRASEÑA DE CADA UNO 
            conexion = new SqlConnection();
            conexion.ConnectionString = "Server=localhost,1433;Database=TurnoTigre;User Id=sa;Password=TU_CONTRASEÑA;TrustServerCertificate=True";
            comando = new SqlCommand();
        }

        public void setearConsulta(string consulta)
        {
            comando.Parameters.Clear();  
            comando.CommandType = CommandType.Text;
            comando.CommandText = consulta;
        }

        public void setearParametro(string nombre, object valor)
        {
            comando.Parameters.AddWithValue(nombre, valor ?? DBNull.Value);
        }

        public void ejecutarLectura()
        {
            comando.Connection = conexion;
            conexion.Open();
            lector = comando.ExecuteReader();
        }

        public void ejecutarAccion()
        {
            comando.Connection = conexion;
            conexion.Open();
            comando.ExecuteNonQuery();
            conexion.Close();
        }

        public int obtenerId()
        {
            comando.Connection = conexion;
            conexion.Open();
            int id = Convert.ToInt32(comando.ExecuteScalar());
            conexion.Close();
            return id;
        }


        public void cerrarConexion()
        {
            if (lector != null && !lector.IsClosed)
                lector.Close();
            if (conexion.State == ConnectionState.Open)
                conexion.Close();

            comando.Parameters.Clear();
        }
    }
}
