namespace Negocio
{
    /// <summary>
    /// Punto único para informar errores que la capa de negocio no puede propagar (porque no deben impedir mostrar una pantalla o cancelar un turno).
    /// La capa Web conecta <see cref="Error"/> con su logger al arrancar.
    /// </summary>
    public static class RegistroErrores
    {
        public static Action<string, Exception>? Error { get; set; }

        public static void Informar(string contexto, Exception ex)
        {
            try { Error?.Invoke(contexto, ex); }
            catch (Exception) { /* registrar el error nunca debe producir otro */ }
        }
    }
}
