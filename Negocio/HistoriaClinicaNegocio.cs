using AccesoDatos;
using Dominio;

namespace Negocio
{
    public class HistoriaClinicaNegocio
    {
        public const int MaxArchivos = 5;
        public const int MaxBytes = 5 * 1024 * 1024;
        private static readonly string[] Extensiones = { ".pdf", ".jpg", ".jpeg", ".png" };

        private readonly HistoriaClinicaDatos datos = new HistoriaClinicaDatos();
        private readonly PacienteDatos pacientes = new PacienteDatos();
        private readonly TurnoDatos turnos = new TurnoDatos();

        private readonly AccesoHistoriaDatos acceso = new AccesoHistoriaDatos();

        public const string AccionVer = "VerHistoria";
        public const string AccionGuardar = "GuardarHistoria";
        public const string AccionDescargar = "DescargarArchivo";
        public const string AccionDenegado = "AccesoDenegado";

        /// <summary>
        /// Decide si un usuario del personal puede acceder a la historia clínica de un paciente y deja constancia (también cuando se deniega).
        /// SuperAdmin accede a todas; un médico solo a las de pacientes con los que tiene relación de atención; el resto no accede.
        /// Si no se puede registrar el acceso, falla: no se muestra una historia sin dejar constancia.
        /// </summary>
        /// <param name="idMedico">Ficha de profesional de quien accede (null si no es médico).</param>
        public bool AutorizarAcceso(int idUsuario, string rol, int? idMedico, int idPaciente, string accion, string? detalle = null)
        {
            bool permitido = rol switch
            {
                Roles.SuperAdmin => true,
                Roles.Medico => idMedico.HasValue && acceso.MedicoAtiende(idMedico.Value, idPaciente),
                _ => false
            };
            if (permitido) RegistrarAcceso(idUsuario, rol, idPaciente, accion, detalle);
            else RegistrarAcceso(idUsuario, rol, idPaciente, AccionDenegado, (accion + " " + detalle).Trim());
            return permitido;
        }

        /// <summary>Deja constancia de un acceso que ya se autorizó por otra vía (por ejemplo, un paciente a sus propios archivos).</summary>
        public void RegistrarAcceso(int idUsuario, string rol, int idPaciente, string accion, string? detalle = null)
        {
            if (detalle != null && detalle.Length > 200) detalle = detalle[..200];
            acceso.Registrar(idUsuario, rol, idPaciente, accion, detalle);
        }

        public HistoriaClinicaVista Obtener(int idPaciente) => datos.Obtener(idPaciente);

        public void GuardarDatosClinicos(HistoriaClinicaVista h)
        {
            if (pacientes.Obtener(h.IdPaciente) == null) throw new Exception("El paciente no existe.");
            if (!string.IsNullOrWhiteSpace(h.GrupoSanguineo) && !Catalogos.GruposSanguineos.Contains(h.GrupoSanguineo))
                throw new Exception("Grupo sanguíneo inválido.");
            if (h.Peso.HasValue && (h.Peso < 1 || h.Peso > 400)) throw new Exception("El peso debe estar entre 1 y 400 kg.");
            if (h.AlturaCm.HasValue && (h.AlturaCm < 30 || h.AlturaCm > 250)) throw new Exception("La altura debe estar entre 30 y 250 cm.");
            foreach (var t in new[] { h.Alergias, h.EnfermedadesCronicas, h.Patologias })
                if ((t ?? "").Length > 120) throw new Exception("Alergias, enfermedades crónicas y patologías admiten hasta 120 caracteres cada una.");
            datos.GuardarDatosClinicos(h);
        }

        public int RegistrarConsulta(int idMedico, int idPaciente, int? idTurno, ConsultaVista c, List<(string Nombre, string Tipo, byte[] Contenido)> archivos)
        {
            if (pacientes.Obtener(idPaciente) == null) throw new Exception("El paciente no existe.");
            c.Observaciones = (c.Observaciones ?? "").Trim();
            c.Diagnostico = (c.Diagnostico ?? "").Trim();
            if (c.Observaciones == "") throw new Exception("Ingresá las observaciones de la consulta.");
            if (c.Diagnostico == "") throw new Exception("Ingresá el diagnóstico.");
            foreach (var t in new[] { c.Observaciones, c.Diagnostico, c.Tratamientos ?? "", c.ProximosPasos ?? "" })
                if (t.Length > 800) throw new Exception("Cada campo de la consulta admite hasta 800 caracteres.");
            if (archivos.Count > MaxArchivos) throw new Exception($"Se pueden adjuntar hasta {MaxArchivos} archivos.");
            foreach (var a in archivos)
            {
                if (!Extensiones.Contains(Path.GetExtension(a.Nombre).ToLowerInvariant())) throw new Exception($"\"{a.Nombre}\": solo se aceptan PDF, JPG o PNG.");
                if (a.Contenido.Length > MaxBytes) throw new Exception($"\"{a.Nombre}\" supera los 5 MB.");
                if (a.Contenido.Length == 0) throw new Exception($"\"{a.Nombre}\" está vacío.");
            }
            if (idTurno.HasValue)
            {
                var t = turnos.Obtener(idTurno.Value);
                if (t == null || t.IdPaciente != idPaciente || t.IdMedico != idMedico) idTurno = null;
            }
            return datos.AgregarConsulta(idPaciente, idMedico, idTurno, c, archivos);
        }

        public (string Nombre, string Tipo, byte[] Contenido, int IdPaciente)? ObtenerArchivo(int idArchivo) => datos.ObtenerArchivo(idArchivo);
    }
}
