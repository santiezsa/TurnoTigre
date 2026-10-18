using Dominio;
using static AccesoDatos.Sql;

namespace AccesoDatos
{
    public class ObraSocialDatos
    {
        public List<ObraSocial> Listar() =>
            Lista("SELECT id_obra_social, nombre FROM OBRA_SOCIAL ORDER BY nombre",
                r => new ObraSocial { IdObraSocial = Ent(r, "id_obra_social"), Nombre = Txt(r, "nombre") });
    }

    public class EspecialidadAdminDatos
    {
        public List<(Especialidad Esp, int Profesionales)> ListarConUso() =>
            Lista(@"SELECT e.id_especialidad, e.codigo, e.descripcion,
                           (SELECT COUNT(*) FROM MEDICO_ESPECIALIDAD me WHERE me.id_especialidad = e.id_especialidad) AS cant
                    FROM ESPECIALIDAD e ORDER BY e.descripcion",
                r => (new Especialidad { IdEspecialidad = Ent(r, "id_especialidad"), Codigo = Txt(r, "codigo"), Descripcion = Txt(r, "descripcion") }, Ent(r, "cant")));

        public bool ExisteCodigo(string codigo, int? excluir = null) =>
            Existe("SELECT COUNT(*) FROM ESPECIALIDAD WHERE codigo = @c AND (@x IS NULL OR id_especialidad <> @x)", ("@c", codigo), ("@x", excluir));

        public void Crear(string codigo, string descripcion) =>
            Ejecutar("INSERT INTO ESPECIALIDAD (codigo, descripcion) VALUES (@c, @d)", ("@c", codigo), ("@d", descripcion));

        public void Actualizar(int id, string codigo, string descripcion) =>
            Ejecutar("UPDATE ESPECIALIDAD SET codigo = @c, descripcion = @d WHERE id_especialidad = @id", ("@id", id), ("@c", codigo), ("@d", descripcion));

        public bool EnUso(int id) =>
            Existe(@"SELECT (SELECT COUNT(*) FROM MEDICO_ESPECIALIDAD WHERE id_especialidad = @id)
                          + (SELECT COUNT(*) FROM AGENDA WHERE id_especialidad = @id)
                          + (SELECT COUNT(*) FROM LISTA_ESPERA WHERE id_especialidad = @id)
                          + (SELECT COUNT(*) FROM MEDICO WHERE id_especialidad = @id)", ("@id", id));

        public void Eliminar(int id) => Ejecutar("DELETE FROM ESPECIALIDAD WHERE id_especialidad = @id", ("@id", id));
    }

    public class CentroAdminDatos
    {
        public bool ExisteCodigo(string codigo, int? excluir = null) =>
            Existe("SELECT COUNT(*) FROM CENTRO_SALUD WHERE codigo = @c AND (@x IS NULL OR id_centro <> @x)", ("@c", codigo), ("@x", excluir));

        public void Crear(CentroSalud c) =>
            Ejecutar("INSERT INTO CENTRO_SALUD (codigo, nombre, direccion, localidad) VALUES (@c, @n, @d, @l)",
                ("@c", c.Codigo), ("@n", c.Nombre), ("@d", c.Direccion), ("@l", c.Localidad));

        public void Actualizar(CentroSalud c) =>
            Ejecutar("UPDATE CENTRO_SALUD SET codigo = @c, nombre = @n, direccion = @d, localidad = @l WHERE id_centro = @id",
                ("@id", c.IdCentro), ("@c", c.Codigo), ("@n", c.Nombre), ("@d", c.Direccion), ("@l", c.Localidad));

        public bool EnUso(int id) =>
            Existe("SELECT (SELECT COUNT(*) FROM AGENDA WHERE id_centro = @id) + (SELECT COUNT(*) FROM LISTA_ESPERA WHERE id_centro = @id)", ("@id", id));

        public void Eliminar(int id) => Ejecutar("DELETE FROM CENTRO_SALUD WHERE id_centro = @id", ("@id", id));
    }
}
