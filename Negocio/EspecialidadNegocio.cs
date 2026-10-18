using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class EspecialidadNegocio
    {
        private readonly EspecialidadDatos datos = new EspecialidadDatos();
        private readonly EspecialidadAdminDatos admin = new EspecialidadAdminDatos();

        public List<Especialidad> Listar()
        {
            return datos.Listar();
        }

        public List<(Especialidad Esp, int Profesionales)> ListarConUso() => admin.ListarConUso();

        public void Guardar(int? id, string codigo, string descripcion)
        {
            codigo = (codigo ?? "").Trim().ToUpperInvariant();
            descripcion = Catalogos.Normalizar(descripcion);
            if (codigo == "" || descripcion == "") throw new Exception("Completá el código y el nombre.");
            if (codigo.Length > 20) throw new Exception("El código admite hasta 20 caracteres.");
            if (admin.ExisteCodigo(codigo, id)) throw new Exception("Ya existe una especialidad con ese código.");
            if (id.HasValue) admin.Actualizar(id.Value, codigo, descripcion); else admin.Crear(codigo, descripcion);
        }

        public void Eliminar(int id)
        {
            if (admin.EnUso(id)) throw new Exception("La especialidad está en uso (profesionales, agendas o lista de espera): no se puede eliminar.");
            admin.Eliminar(id);
        }
    }
}
