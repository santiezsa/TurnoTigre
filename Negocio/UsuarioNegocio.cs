using Dominio;
using AccesoDatos;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Negocio
{
    public class UsuarioNegocio
    {
        private readonly UsuarioDatos usuarioDatos = new UsuarioDatos();

        /// <summary>Ingreso del personal (Administrativo, Médico, Super Admin) con nombre de usuario.</summary>
        public Usuario ValidarPersonal(string nombreUsuario, string contrasena)
        {
            var usuario = usuarioDatos.BuscarUsuarioPorNombre((nombreUsuario ?? "").Trim().ToLowerInvariant());
            if (usuario == null || usuario.Rol == Roles.Paciente || !BCrypt.Net.BCrypt.Verify(contrasena, usuario.PasswordHash))
                throw new Exception("El usuario o la contraseña no son correctos.");
            if (!usuario.Activo) throw new Exception("El usuario está deshabilitado.");
            return usuario;
        }

        public Usuario ValidarUsuario(string email, string contrasena)
        {

            email = email.Trim().ToLower();
            Usuario usuario = usuarioDatos.BuscarUsuarioPorEmail(email);

            if (usuario == null)
            {
                throw new Exception("El Email o la contraseña no son correctas.");
            }

            if (!BCrypt.Net.BCrypt.Verify(contrasena, usuario.PasswordHash))
            {
                throw new Exception("El Email o la contraseña no son correctas.");
            }

            if (!usuario.Activo)
            {
                throw new Exception("El usuario está deshabilitado.");
            }


            return usuario;

        }

        // ---------- Gestión de usuarios (Super Admin) ----------

        public const int UsuariosPorPagina = 10;

        public List<UsuarioListado> Listar(string? texto, string? rol, bool? activo)
        {
            return usuarioDatos.Listar(texto, rol, activo);
        }

        public UsuarioListado? ObtenerPorId(int idUsuario)
        {
            return usuarioDatos.ObtenerListadoPorId(idUsuario);
        }

        // Normaliza a minúsculas, sin tildes ni símbolos: "María José" -> "mariajose"
        private static string Normalizar(string texto)
        {
            string descompuesto = texto.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in descompuesto)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
            }
            return sb.ToString();
        }

        // Patrón nombre.apellido; si ya existe se agrega un número al final (nombre.apellido2, 3...)
        public string GenerarNombreUsuario(string nombre, string apellido)
        {
            string n = Normalizar(nombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "");
            string a = Normalizar(apellido);
            if (n == "" || a == "")
                throw new Exception("No se pudo generar el nombre de usuario: revisá nombre y apellido.");

            string baseUsuario = n + "." + a;
            string candidato = baseUsuario;
            int numero = 2;
            while (usuarioDatos.ExisteNombreUsuario(candidato))
            {
                candidato = baseUsuario + numero;
                numero++;
            }
            return candidato;
        }

        // Patrón: "Tigre" + 4 dígitos (2-9) + 1 mayúscula + 1 minúscula. Ej: Tigre4728Kp
        public static string GenerarPasswordProvisoria()
        {
            const string digitos = "23456789";
            const string mayus = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string minus = "abcdefghjkmnpqrstuvwxyz";

            var sb = new StringBuilder("Tigre");
            for (int i = 0; i < 4; i++) sb.Append(digitos[RandomNumberGenerator.GetInt32(digitos.Length)]);
            sb.Append(mayus[RandomNumberGenerator.GetInt32(mayus.Length)]);
            sb.Append(minus[RandomNumberGenerator.GetInt32(minus.Length)]);
            return sb.ToString();
        }

        public static bool PasswordEsValida(string password, out string mensaje)
        {
            mensaje = "";
            if (password.Length < 8) mensaje = "La contraseña debe tener al menos 8 caracteres.";
            else if (!Regex.IsMatch(password, "[A-Z]")) mensaje = "La contraseña debe tener al menos una mayúscula.";
            else if (!Regex.IsMatch(password, "[a-z]")) mensaje = "La contraseña debe tener al menos una minúscula.";
            else if (!Regex.IsMatch(password, "[0-9]")) mensaje = "La contraseña debe tener al menos un número.";
            return mensaje == "";
        }

        public class UsuarioCreado
        {
            public int IdUsuario { get; set; }
            public string NombreUsuario { get; set; } = "";
            public string Email { get; set; } = "";
            public string PasswordProvisoria { get; set; } = "";
        }

        // Crea el usuario con usuario y contraseña generados. La contraseña provisoria se devuelve
        // para que la capa web la envíe por mail; en la base queda solo el hash.
        public UsuarioCreado CrearUsuarioInterno(string rol, string nombre, string apellido, string dni,
                                                 string email, string telefono, string? matricula, int? idEspecialidad)
        {
            nombre = nombre.Trim();
            apellido = apellido.Trim();
            dni = dni.Trim();
            email = email.Trim().ToLowerInvariant();
            telefono = telefono.Trim();

            if (rol == Roles.Paciente || !Roles.Todos.Contains(rol))
                throw new Exception("El rol seleccionado no es válido. Los pacientes se registran solos.");
            if (nombre == "" || apellido == "") throw new Exception("Completá nombre y apellido.");
            if (!Regex.IsMatch(dni, @"^\d{7,8}$")) throw new Exception("El DNI debe tener 7 u 8 números.");
            if (usuarioDatos.ExisteEmail(email)) throw new Exception("Ya existe un usuario con ese correo.");

            // Si el profesional ya estaba cargado (sin cuenta), se le crea el usuario sobre esa misma ficha.
            ProfesionalVista? existente = rol == Roles.Medico
                ? new AccesoDatos.ProfesionalDatos().SinUsuario().FirstOrDefault(p => p.Dni == dni)
                : null;
            if (existente == null && usuarioDatos.ExisteDniPersona(dni)) throw new Exception("Ya existe una persona con ese DNI.");

            if (existente != null)
            {
                string nu = GenerarNombreUsuario(existente.Nombre, existente.Apellido);
                string prov = GenerarPasswordProvisoria();
                var u = new Usuario { NombreUsuario = nu, Email = email, Rol = rol, PasswordHash = BCrypt.Net.BCrypt.HashPassword(prov), DebeCambiarPassword = true };
                int idU = usuarioDatos.AgregarUsuarioParaPersona(u, existente.IdMedico);
                return new UsuarioCreado { IdUsuario = idU, NombreUsuario = nu, Email = email, PasswordProvisoria = prov };
            }

            if (rol == Roles.Medico)
            {
                if (string.IsNullOrWhiteSpace(matricula)) throw new Exception("Ingresá la matrícula del médico.");
                if (idEspecialidad == null) throw new Exception("Elegí la especialidad del médico.");
                matricula = matricula.Trim();
                if (usuarioDatos.ExisteMatricula(matricula)) throw new Exception("Ya existe un médico con esa matrícula.");
            }
            else
            {
                matricula = null;
                idEspecialidad = null;
            }

            string nombreUsuario = GenerarNombreUsuario(nombre, apellido);
            string provisoria = GenerarPasswordProvisoria();

            var usuario = new Usuario
            {
                NombreUsuario = nombreUsuario,
                Email = email,
                Rol = rol,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(provisoria),
                DebeCambiarPassword = true
            };

            int id = usuarioDatos.AgregarUsuarioInterno(usuario, dni, nombre, apellido, telefono, matricula, idEspecialidad);

            return new UsuarioCreado { IdUsuario = id, NombreUsuario = nombreUsuario, Email = email, PasswordProvisoria = provisoria };
        }

        public void EditarUsuario(int idUsuario, string email, string nombre, string apellido, string telefono)
        {
            email = email.Trim().ToLowerInvariant();
            if (nombre.Trim() == "" || apellido.Trim() == "") throw new Exception("Completá nombre y apellido.");
            if (usuarioDatos.ExisteEmailDeOtroUsuario(email, idUsuario)) throw new Exception("Ya existe otro usuario con ese correo.");
            usuarioDatos.ActualizarUsuario(idUsuario, email, nombre.Trim(), apellido.Trim(), telefono.Trim());
        }

        // Reglas: no podés deshabilitarte a vos mismo ni al último Super Admin activo.
        public void CambiarEstado(int idUsuario, bool activo, int idUsuarioActual)
        {
            Usuario? objetivo = usuarioDatos.BuscarUsuarioPorId(idUsuario);
            if (objetivo == null) throw new Exception("El usuario no existe.");

            if (!activo)
            {
                if (idUsuario == idUsuarioActual)
                    throw new Exception("No podés deshabilitar tu propio usuario.");
                if (objetivo.Rol == Roles.SuperAdmin && objetivo.Activo && usuarioDatos.ContarSuperAdminsActivos() <= 1)
                    throw new Exception("No se puede deshabilitar al último Super Admin activo.");
            }
            usuarioDatos.CambiarEstado(idUsuario, activo);
        }

        // Genera una nueva contraseña provisoria (obliga a cambiarla en el próximo ingreso)
        public string RestablecerPassword(int idUsuario)
        {
            if (usuarioDatos.BuscarUsuarioPorId(idUsuario) == null) throw new Exception("El usuario no existe.");
            string provisoria = GenerarPasswordProvisoria();
            usuarioDatos.ActualizarPassword(idUsuario, BCrypt.Net.BCrypt.HashPassword(provisoria), true);
            return provisoria;
        }

        public void CambiarPassword(int idUsuario, string actual, string nueva)
        {
            Usuario? usuario = usuarioDatos.BuscarUsuarioPorId(idUsuario);
            if (usuario == null) throw new Exception("El usuario no existe.");
            if (!BCrypt.Net.BCrypt.Verify(actual, usuario.PasswordHash))
                throw new Exception("La contraseña actual no es correcta.");
            if (actual == nueva) throw new Exception("La nueva contraseña debe ser distinta de la actual.");
            if (!PasswordEsValida(nueva, out string mensaje)) throw new Exception(mensaje);
            usuarioDatos.ActualizarPassword(idUsuario, BCrypt.Net.BCrypt.HashPassword(nueva), false);
        }

    
        // ---------- Recuperar contraseña ("Olvidé mi contraseña") ----------

        public class SolicitudRecuperacion
        {
            public string Email { get; set; } = "";
            public string NombreUsuario { get; set; } = "";
            public string Token { get; set; } = "";
        }

        private static string HashToken(string token)
        {
            byte[] h = SHA256.HashData(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(h).ToLowerInvariant();
        }

        /// <summary>
        /// Prepara el enlace de recuperación. Devuelve null cuando no hay que mandar nada (cuenta inexistente, deshabilitada,
        /// de otro tipo o con demasiados pedidos). Quien llama responde SIEMPRE lo mismo, exista o no la cuenta,
        /// para que nadie pueda averiguar qué correos o usuarios están registrados.
        /// El enlace se manda únicamente al correo que figura en la cuenta: no se puede indicar otro destino.
        /// </summary>
        public SolicitudRecuperacion? SolicitarRecuperacion(string identificador, bool esPersonal)
        {
            identificador = (identificador ?? "").Trim().ToLowerInvariant();
            if (identificador.Length < 3 || identificador.Length > 100) return null;

            Usuario? u = esPersonal ? usuarioDatos.BuscarUsuarioPorNombre(identificador) : usuarioDatos.BuscarUsuarioPorEmail(identificador);
            if (u == null || !u.Activo) return null;
            if ((u.Rol == Roles.Paciente) == esPersonal) return null;          // cada pantalla atiende a sus propios usuarios
            if (usuarioDatos.ContarRecuperacionesRecientes(u.IdUsuario) >= 3) return null;   // freno ante abusos

            string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)).Replace('+', '-').Replace('/', '_').TrimEnd('=');
            usuarioDatos.CrearRecuperacion(u.IdUsuario, HashToken(token));
            return new SolicitudRecuperacion { Email = u.Email, NombreUsuario = u.NombreUsuario, Token = token };
        }

        public bool EnlaceDeRecuperacionVigente(string? token) =>
            !string.IsNullOrWhiteSpace(token) && token.Length <= 100 && usuarioDatos.RecuperacionVigente(HashToken(token));

        /// <summary>Cambia la contraseña con el enlace. Devuelve true si la cuenta era del personal.</summary>
        public bool RestablecerConEnlace(string? token, string nueva)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 100) throw new Exception("El enlace no es válido. Pedí uno nuevo.");
            if (!PasswordEsValida(nueva, out string mensaje)) throw new Exception(mensaje);
            int r = usuarioDatos.CompletarRecuperacion(HashToken(token), BCrypt.Net.BCrypt.HashPassword(nueva));
            if (r == 0) throw new Exception("El enlace venció o ya fue usado. Pedí uno nuevo desde \"¿Olvidaste tu contraseña?\".");
            return r == 2;
        }
}
}
