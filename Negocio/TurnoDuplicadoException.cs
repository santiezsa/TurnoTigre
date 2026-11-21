namespace Negocio
{
    /// <summary>
    /// RF3: el paciente ya tiene un turno activo de esa especialidad. No es un error: la pantalla le informa
    /// y le pregunta si quiere reservar otro de todas formas (si dice que sí, se vuelve a reservar con aceptaDuplicado).
    /// </summary>
    public class TurnoDuplicadoException : Exception
    {
        public TurnoDuplicadoException(string mensaje) : base(mensaje) { }
    }
}
