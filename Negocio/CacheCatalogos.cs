using Microsoft.Extensions.Caching.Memory;

namespace Negocio
{
    /// <summary>
    /// Gestión de caché en memoria para catálogos estáticos o de baja mutabilidad (Centros, Especialidades, Obras Sociales).
    /// Evita viajes reiterados a la base de datos en pantallas de reservas, filtros y altas.
    /// </summary>
    public static class CacheCatalogos
    {
        private static readonly IMemoryCache Cache = new MemoryCache(new MemoryCacheOptions());
        private static readonly TimeSpan ExpiracionDefault = TimeSpan.FromMinutes(30);

        public const string KeyCentros = "Catalogo_CentrosSalud";
        public const string KeyEspecialidades = "Catalogo_Especialidades";
        public const string KeyObrasSociales = "Catalogo_ObrasSociales";

        public static T ObtenerOCrear<T>(string key, Func<T> factory, TimeSpan? expiracion = null)
        {
            return Cache.GetOrCreate(key, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = expiracion ?? ExpiracionDefault;
                return factory();
            })!;
        }

        public static void Invalidar(string key)
        {
            Cache.Remove(key);
        }

        public static void LimpiarTodo()
        {
            Cache.Remove(KeyCentros);
            Cache.Remove(KeyEspecialidades);
            Cache.Remove(KeyObrasSociales);
        }
    }
}
