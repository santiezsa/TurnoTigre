using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class CentroSaludNegocio
    {
        private readonly CentroSaludDatos datos = new CentroSaludDatos();
        private readonly CentroAdminDatos admin = new CentroAdminDatos();

        public List<CentroSalud> Listar()
        {
            return datos.Listar();
        }

        public void Guardar(CentroSalud c)
        {
            c.Codigo = (c.Codigo ?? "").Trim().ToUpperInvariant();
            c.Nombre = (c.Nombre ?? "").Trim();
            c.Direccion = (c.Direccion ?? "").Trim();
            if (c.Codigo == "" || c.Nombre == "" || c.Direccion == "") throw new Exception("Completá código, nombre y dirección.");
            if (!Catalogos.LocalidadValida(c.Localidad)) throw new Exception("Elegí una localidad de Tigre.");
            c.Localidad = Catalogos.NormalizarLocalidad(c.Localidad);
            if (admin.ExisteCodigo(c.Codigo, c.IdCentro == 0 ? null : c.IdCentro)) throw new Exception("Ya existe un centro con ese código.");
            if (c.IdCentro == 0) admin.Crear(c); else admin.Actualizar(c);
        }

        public void Eliminar(int id)
        {
            if (admin.EnUso(id)) throw new Exception("El centro tiene agendas o lista de espera: no se puede eliminar.");
            admin.Eliminar(id);
        }
    }
}
