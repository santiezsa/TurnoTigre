namespace Dominio
{
    public class PacienteFicha
    {
        public int IdPaciente { get; set; }
        public int? IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellido { get; set; } = "";
        public string TipoDocumento { get; set; } = "DNI";
        public string Dni { get; set; } = "";
        public string Email { get; set; } = "";
        public string Celular { get; set; } = "";
        public string Fijo { get; set; } = "";
        public DateTime FechaNacimiento { get; set; }
        public string Sexo { get; set; } = "";
        public string Calle { get; set; } = "";
        public string Altura { get; set; } = "";
        public string Localidad { get; set; } = "";
        public string Provincia { get; set; } = "Buenos Aires";
        public string CodigoPostal { get; set; } = "";
        public int? IdObraSocial { get; set; }
        public string ObraSocial { get; set; } = "";
        public string NroAfiliado { get; set; } = "";
        public int? IdTitular { get; set; }
        public string Parentesco { get; set; } = "";
        public bool TieneFoto { get; set; }
        public string Completo => $"{Apellido}, {Nombre}";
        public string Direccion => $"{Calle} {Altura}".Trim();
        public string Tel => !string.IsNullOrEmpty(Celular) ? Celular : Fijo;
        public string Iniciales => (Nombre.Length > 0 && Apellido.Length > 0) ? $"{Nombre[0]}{Apellido[0]}".ToUpper() : "?";

        public int Edad
        {
            get
            {
                var hoy = DateTime.Today; int e = hoy.Year - FechaNacimiento.Year;
                if (FechaNacimiento.Date > hoy.AddYears(-e)) e--;
                return e;
            }
        }
    }
}
