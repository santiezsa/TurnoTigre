using AccesoDatos;
using Dominio;
using System.Text.RegularExpressions;

namespace Negocio
{
    public class PacienteNegocio
    {
        private readonly PacienteDatos pacienteDatos = new PacienteDatos();
        private readonly UsuarioDatos usuarioDatos = new UsuarioDatos();
        private readonly TurnoDatos turnoDatos = new TurnoDatos();

        // ---------- Registro (el paciente crea su cuenta) ----------
        public int Agregar(Paciente paciente, string password)
        {
            if (paciente.Usuario == null || string.IsNullOrWhiteSpace(paciente.Usuario.Email))
                throw new Exception("El correo electrónico es obligatorio.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new Exception("La contraseña debe tener al menos 8 caracteres.");

            paciente.Nombre = Catalogos.Normalizar(paciente.Nombre);
            paciente.Apellido = Catalogos.Normalizar(paciente.Apellido);
            paciente.Dni = paciente.Dni.Trim();
            paciente.Usuario.Email = paciente.Usuario.Email.Trim().ToLower();
            if (!Regex.IsMatch(paciente.Dni, @"^\d{7,8}$")) throw new Exception("El DNI debe tener 7 u 8 números.");
            if (!Catalogos.LocalidadValida(paciente.Localidad))
                throw new Exception("El sistema atiende a vecinos del partido de Tigre. Elegí una localidad de la lista.");
            paciente.Localidad = Catalogos.NormalizarLocalidad(paciente.Localidad);
            if (paciente.FechaNacimiento.Date > DateTime.Today || paciente.FechaNacimiento.Year < 1900)
                throw new Exception("La fecha de nacimiento no es válida.");

            if (usuarioDatos.ExisteEmail(paciente.Usuario.Email))
                throw new Exception("Ese correo ya está registrado.");

            string hash = BCrypt.Net.BCrypt.HashPassword(password);

            // ¿El personal ya había cargado su ficha? Se vincula si coinciden DNI y fecha de nacimiento.
            var ficha = pacienteDatos.PorDni(paciente.Dni);
            if (ficha != null)
            {
                if (ficha.IdUsuario.HasValue) throw new Exception("Ya existe un paciente registrado con ese DNI.");
                if (ficha.IdTitular.HasValue) throw new Exception("Ese DNI figura como familiar a cargo de otra cuenta. Pedile a tu familiar que te dé de baja o acercate a un centro de salud.");
                if (ficha.FechaNacimiento.Date != paciente.FechaNacimiento.Date)
                    throw new Exception("Ese DNI ya está cargado en el sistema pero la fecha de nacimiento no coincide. Acercate a un centro de salud para corregirlo.");

                ficha.Nombre = paciente.Nombre; ficha.Apellido = paciente.Apellido; ficha.Sexo = paciente.Sexo;
                ficha.Calle = paciente.Calle; ficha.Altura = paciente.Altura; ficha.Localidad = paciente.Localidad;
                ficha.Celular = paciente.Telefono; ficha.Email = paciente.Usuario.Email;
                pacienteDatos.VincularUsuario(ficha.IdPaciente, ficha, hash);
                return ficha.IdPaciente;
            }
            if (pacienteDatos.ExisteDni(paciente.Dni)) throw new Exception("Ya existe una persona registrada con ese DNI.");

            paciente.Usuario.PasswordHash = hash;
            return pacienteDatos.AgregarPaciente(paciente);
        }

        // ---------- Consultas ----------
        public PacienteFicha? Obtener(int id) => pacienteDatos.Obtener(id);

        // ---------- Foto de perfil ----------
        public const int FotoMaxBytes = 2 * 1024 * 1024;

        public void GuardarFoto(int idPaciente, byte[] contenido)
        {
            if (contenido == null || contenido.Length == 0) throw new Exception("Elegí una foto.");
            if (contenido.Length > FotoMaxBytes) throw new Exception("La foto pesa más de 2 MB. Elegí una más liviana.");
            string tipo = TipoDeImagen(contenido) ?? throw new Exception("La foto tiene que ser un archivo JPG o PNG.");
            pacienteDatos.GuardarFoto(idPaciente, contenido, tipo);
        }

        public void QuitarFoto(int idPaciente) => pacienteDatos.QuitarFoto(idPaciente);

        public (byte[] Contenido, string Tipo)? ObtenerFoto(int idPaciente) => pacienteDatos.ObtenerFoto(idPaciente);

        // Se mira el contenido real del archivo (no solo el nombre) para aceptar únicamente JPG y PNG
        private static string? TipoDeImagen(byte[] b)
        {
            if (b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF) return "image/jpeg";
            if (b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 && b[4] == 0x0D && b[5] == 0x0A && b[6] == 0x1A && b[7] == 0x0A) return "image/png";
            return null;
        }
        public PacienteFicha? DeUsuario(int idUsuario) => pacienteDatos.PorUsuario(idUsuario);
        public List<PacienteFicha> Buscar(string? texto) => pacienteDatos.Buscar(texto);
        public List<PacienteFicha> Familiares(int idTitular) => pacienteDatos.Familiares(idTitular);

        /// <summary>Titular + familiares (para elegir "¿para quién es el turno?").</summary>
        public List<PacienteFicha> GrupoFamiliar(int idTitular)
        {
            var lista = new List<PacienteFicha>();
            var yo = pacienteDatos.Obtener(idTitular);
            if (yo != null) lista.Add(yo);
            lista.AddRange(pacienteDatos.Familiares(idTitular));
            return lista;
        }

        // ---------- Validación y normalización ----------
        private void ValidarFicha(PacienteFicha f, bool exigirContacto)
        {
            f.Nombre = Catalogos.Normalizar(f.Nombre);
            f.Apellido = Catalogos.Normalizar(f.Apellido);
            f.Dni = (f.Dni ?? "").Trim();
            f.Email = (f.Email ?? "").Trim().ToLowerInvariant();
            f.Celular = (f.Celular ?? "").Trim();
            f.Fijo = (f.Fijo ?? "").Trim();
            f.Calle = Catalogos.Normalizar(f.Calle);
            f.Altura = (f.Altura ?? "").Trim();
            f.NroAfiliado = (f.NroAfiliado ?? "").Trim();
            f.CodigoPostal = (f.CodigoPostal ?? "").Trim();
            if (string.IsNullOrWhiteSpace(f.TipoDocumento)) f.TipoDocumento = "DNI";
            f.Provincia = "Buenos Aires";

            if (f.Nombre == "" || f.Apellido == "") throw new Exception("Completá nombre y apellido.");
            if (f.TipoDocumento == "DNI" ? !Regex.IsMatch(f.Dni, @"^\d{7,8}$") : !Regex.IsMatch(f.Dni, @"^[A-Za-z0-9]{5,15}$"))
                throw new Exception(f.TipoDocumento == "DNI" ? "El DNI debe tener 7 u 8 números." : "El número de documento no es válido.");
            if (f.FechaNacimiento == default || f.FechaNacimiento.Date > DateTime.Today || f.FechaNacimiento.Year < 1900)
                throw new Exception("La fecha de nacimiento no es válida.");
            if (!Catalogos.Sexos.Contains(f.Sexo)) throw new Exception("Elegí el sexo.");
            if (f.Calle == "" || f.Altura == "") throw new Exception("Completá la calle y la altura del domicilio.");
            if (!Catalogos.LocalidadValida(f.Localidad))
                throw new Exception("El domicilio debe estar en el partido de Tigre. Elegí una localidad de la lista.");
            f.Localidad = Catalogos.NormalizarLocalidad(f.Localidad);
            if (f.Email != "" && !Regex.IsMatch(f.Email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$")) throw new Exception("El correo no es válido.");
            if (exigirContacto && f.Celular == "" && f.Fijo == "") throw new Exception("Ingresá al menos un teléfono de contacto.");
            if (f.IdObraSocial.HasValue && f.NroAfiliado == "") throw new Exception("Ingresá el número de afiliado de la obra social.");
            if (!f.IdObraSocial.HasValue) f.NroAfiliado = "";
        }

        // ---------- Personal administrativo ----------
        public int CrearPorPersonal(PacienteFicha f)
        {
            ValidarFicha(f, true);
            if (pacienteDatos.ExisteDni(f.Dni)) throw new Exception("Ya existe una persona cargada con ese DNI.");
            f.IdTitular = null; f.Parentesco = "";
            return pacienteDatos.Crear(f);
        }

        public void EditarPorPersonal(PacienteFicha f)
        {
            var actual = pacienteDatos.Obtener(f.IdPaciente) ?? throw new Exception("El paciente no existe.");
            f.Dni = actual.Dni;                // el DNI no se edita
            f.IdTitular = actual.IdTitular; f.Parentesco = actual.Parentesco;
            if (actual.IdUsuario.HasValue) f.Email = actual.Email;   // el correo de la cuenta se cambia desde la cuenta
            ValidarFicha(f, true);
            pacienteDatos.Actualizar(f);
        }

        // ---------- El propio paciente ----------
        public void GuardarMisDatos(PacienteFicha f)
        {
            var actual = pacienteDatos.Obtener(f.IdPaciente) ?? throw new Exception("No se encontró tu ficha.");
            f.Dni = actual.Dni; f.Email = actual.Email; f.TipoDocumento = actual.TipoDocumento;
            f.IdTitular = actual.IdTitular; f.Parentesco = actual.Parentesco;
            ValidarFicha(f, true);
            pacienteDatos.ActualizarContacto(f);
        }

        // ---------- Familiares ----------
        public int AgregarFamiliar(int idTitular, PacienteFicha f)
        {
            var titular = pacienteDatos.Obtener(idTitular) ?? throw new Exception("No se encontró tu ficha.");
            if (pacienteDatos.ContarFamiliares(idTitular) >= Catalogos.MaxFamiliares)
                throw new Exception($"Podés tener hasta {Catalogos.MaxFamiliares} familiares a cargo.");
            if (!Catalogos.Parentescos.Contains(f.Parentesco)) throw new Exception("Elegí el parentesco.");

            // Los datos de domicilio y teléfono se heredan del titular
            f.Calle = titular.Calle; f.Altura = titular.Altura; f.Localidad = titular.Localidad;
            f.Celular = titular.Celular; f.Fijo = titular.Fijo; f.CodigoPostal = titular.CodigoPostal;
            f.TipoDocumento = "DNI"; f.Email = ""; f.IdObraSocial = titular.IdObraSocial; f.NroAfiliado = titular.NroAfiliado ?? "";
            if (!f.IdObraSocial.HasValue) f.NroAfiliado = "";
            ValidarFicha(f, false);

            var existente = pacienteDatos.PorDni(f.Dni);
            if (existente != null)
            {
                if (existente.IdUsuario.HasValue) throw new Exception("Ese DNI pertenece a un usuario registrado. Cada persona con cuenta gestiona sus propios turnos.");
                if (existente.IdTitular.HasValue && existente.IdTitular != idTitular) throw new Exception("Ese DNI ya figura como familiar de otra cuenta.");
                if (existente.IdTitular == idTitular && pacienteDatos.Familiares(idTitular).Any(x => x.IdPaciente == existente.IdPaciente))
                    throw new Exception("Ese familiar ya está en tu lista.");
                if (existente.FechaNacimiento.Date != f.FechaNacimiento.Date)
                    throw new Exception("Ese DNI ya está cargado en el sistema con otra fecha de nacimiento. Acercate a un centro de salud.");
                pacienteDatos.AsignarTitular(existente.IdPaciente, idTitular, f.Parentesco);
                return existente.IdPaciente;
            }
            if (pacienteDatos.ExisteDni(f.Dni)) throw new Exception("Ya existe una persona registrada con ese DNI.");

            f.IdTitular = idTitular;
            return pacienteDatos.Crear(f);
        }

        public void QuitarFamiliar(int idTitular, int idFamiliar)
        {
            var fam = pacienteDatos.Obtener(idFamiliar);
            if (fam == null || fam.IdTitular != idTitular) throw new Exception("Ese familiar no está en tu lista.");
            if (turnoDatos.TieneTurnoFuturo(idFamiliar))
                throw new Exception($"{fam.Nombre} tiene turnos próximos. Cancelalos antes de quitarlo de tu lista.");
            pacienteDatos.QuitarFamiliar(idFamiliar);
        }
    }
}
