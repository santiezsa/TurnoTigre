using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class ObraSocialNegocio
    {
        public List<ObraSocial> Listar() => new ObraSocialDatos().Listar();
    }
}
