using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class ObraSocialNegocio
    {
        public List<ObraSocial> Listar() =>
            CacheCatalogos.ObtenerOCrear(CacheCatalogos.KeyObrasSociales, () => new ObraSocialDatos().Listar());
    }
}
