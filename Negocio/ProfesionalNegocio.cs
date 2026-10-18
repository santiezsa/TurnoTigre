using AccesoDatos;
using Dominio;
using System.Text.RegularExpressions;

namespace Negocio
{
    public class ProfesionalNegocio
    {
        private readonly ProfesionalDatos datos = new ProfesionalDatos();

        public List<ProfesionalVista> Listar(string? texto = null, int? idEspecialidad = null) => datos.Listar(texto, idEspecialidad);
        public ProfesionalVista? Obtener(int id) => datos.Obtener(id);
        public ProfesionalVista? ObtenerPorUsuario(int idUsuario) => datos.ObtenerPorUsuario(idUsuario);

        private static void Normalizar(ProfesionalVista p)
        {
            p.Nombre = Catalogos.Normalizar(p.Nombre);
            p.Apellido = Catalogos.Normalizar(p.Apellido);
            p.Matricula = (p.Matricula ?? "").Trim().ToUpperInvariant();
            p.Email = (p.Email ?? "").Trim().ToLowerInvariant();
            p.Telefono = (p.Telefono ?? "").Trim();
        }

        private static void Validar(ProfesionalVista p, List<int> especialidades)
        {
            if (p.Nombre == "" || p.Apellido == "") throw new Exception("Completá nombre y apellido.");
            if (p.Matricula == "") throw new Exception("Ingresá la matrícula.");
            if (especialidades.Count == 0) throw new Exception("Elegí al menos una especialidad.");
            if (p.Email != "" && !Regex.IsMatch(p.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) throw new Exception("El correo no es válido.");
        }

        public int Crear(ProfesionalVista p, List<int> especialidades)
        {
            p.Dni = (p.Dni ?? "").Trim();
            Normalizar(p);
            if (!Regex.IsMatch(p.Dni, @"^\d{7,8}$")) throw new Exception("El DNI debe tener 7 u 8 números.");
            Validar(p, especialidades);
            if (datos.ExisteDni(p.Dni)) throw new Exception("Ya existe una persona cargada con ese DNI.");
            if (datos.ExisteMatricula(p.Matricula)) throw new Exception("Ya existe un profesional con esa matrícula.");
            return datos.Crear(p, especialidades.Distinct().ToList());
        }

        public void Editar(ProfesionalVista p, List<int> especialidades)
        {
            var actual = datos.Obtener(p.IdMedico) ?? throw new Exception("El profesional no existe.");
            if (actual.CantAgendas > 0) throw new Exception("El profesional tiene agendas: no se puede modificar. Eliminá primero sus agendas.");
            Normalizar(p);
            Validar(p, especialidades);
            if (datos.ExisteMatricula(p.Matricula, p.IdMedico)) throw new Exception("Ya existe otro profesional con esa matrícula.");
            datos.Actualizar(p, especialidades.Distinct().ToList());
        }

        /// <summary>
        /// Elimina al profesional (deja de figurar en el sistema). Si tenía usuario o ya atendió consultas
        /// se da de baja en lugar de borrarlo, para no perder las historias clínicas, y se deshabilita su usuario.
        /// </summary>
        public void Eliminar(int idMedico)
        {
            var actual = datos.Obtener(idMedico) ?? throw new Exception("El profesional no existe.");
            if (actual.CantAgendas > 0)
                throw new Exception($"{actual.Completo} tiene {actual.CantAgendas} agenda(s) creada(s). Primero eliminá sus agendas desde \"Agendas\".");
            if (actual.TieneUsuario || datos.TieneConsultas(idMedico)) datos.DarDeBaja(idMedico);
            else datos.Eliminar(idMedico);
        }
    }
}
