using Dominio;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Negocio;
using System.ComponentModel.DataAnnotations;

namespace Web.Pages
{
    public class RegistroModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu nombre.")]
        public string Nombre { get; set; } = string.Empty;  

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu apellido.")]
        public string Apellido { get; set; } = string.Empty;  

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu DNI.")]
        [RegularExpression("[0-9]{7,8}", ErrorMessage = "El DNI debe tener 7 u 8 números, sin puntos.")]
        public string Dni { get; set; } = string.Empty;

        [BindProperty]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Ingresá tu fecha de nacimiento.")]
        public DateTime? FechaNacimiento { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Seleccioná el sexo.")]
        public string Sexo { get; set; } = string.Empty;       

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu localidad.")]
        public string Localidad { get; set; } = string.Empty;     

        [BindProperty]
        [Required(ErrorMessage = "Ingresá la calle.")]
        public string Calle { get; set; } = string.Empty;     

        [BindProperty]
        [Required(ErrorMessage = "Ingresá la altura.")]
        public string Altura { get; set; } = string.Empty;

        [BindProperty]
        public bool AceptaDomicilio { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Ingresá un teléfono.")]
        public string Telefono { get; set; } = string.Empty;       

        [BindProperty]
        [Required(ErrorMessage = "Ingresá tu correo.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Correo { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Ingresá una contraseña.")]
        [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
        public string Contrasena { get; set; } = string.Empty;

        [BindProperty]
        [Compare(nameof(Contrasena), ErrorMessage = "Las contraseñas no coinciden.")]
        public string Confirmacion { get; set; } = string.Empty;   

        public string? Error { get; set; }

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {

            if (!AceptaDomicilio)
                ModelState.AddModelError(nameof(AceptaDomicilio), "Tenés que confirmar que tu domicilio está en el partido de Tigre.");

            if (!ModelState.IsValid)
                return Page();

            
            Paciente paciente = new Paciente
            {
                Nombre = Nombre.Trim(),
                Apellido = Apellido.Trim(),
                Dni = Dni,
                FechaNacimiento = FechaNacimiento!.Value,
                Sexo = Sexo,
                Localidad = Localidad.Trim(),
                Calle = Calle.Trim(),
                Altura = Altura.Trim(),
                Telefono = Telefono.Trim(),
                Usuario = new Usuario { Email = Correo }
            };

            try
            {
                PacienteNegocio negocio = new PacienteNegocio();
                negocio.Agregar(paciente, Contrasena);

                TempData["Mensaje"] = "¡Tu cuenta se creó correctamente! Ya podés ingresar.";
                return RedirectToPage("/Ingresar");
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                return Page();
            }
        }

    }
}