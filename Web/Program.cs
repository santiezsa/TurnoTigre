using AccesoDatos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using Negocio;
using System.Security.Claims;
using Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

//
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Ingresar";
        options.AccessDeniedPath = "/Ingresar";

        // En cada pedido se confirma que el usuario siga activo y con el mismo rol (con un minuto de caché para no consultar la base en cada clic).
        options.Events.OnValidatePrincipal = async context =>
        {
            var principal = context.Principal;
            string rol = principal?.FindFirst(ClaimTypes.Role)?.Value ?? "";
            if (!int.TryParse(principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int idUsuario))
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return;
            }

            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
            string clave = $"sesion:{idUsuario}:{rol}";
            if (cache.TryGetValue(clave, out _)) return;

            bool vigente;
            try { vigente = new UsuarioNegocio().SesionVigente(idUsuario, rol); }
            catch (Exception ex)
            {
                // Si la base no responde no se corta la sesión: las pantallas fallarían igual y se reintenta en el próximo pedido.
                RegistroErrores.Informar("Revalidar la sesión de un usuario", ex);
                return;
            }

            if (vigente)
            {
                cache.Set(clave, true, TimeSpan.FromMinutes(1));
                return;
            }
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        };
    });

// Configuramos la cadena de conexión para ConexionDB desde appsettings / variables de entorno
string? connectionString = builder.Configuration.GetConnectionString("TurnoTigreDB");
if (!string.IsNullOrEmpty(connectionString))
{
    ConexionDB.CadenaConexionDefault = connectionString;
}

// Clave para cifrar los datos de salud (SRS: cifrado de los datos sensibles). En producción va por variable de entorno (Cifrado__Clave),
// nunca en el repo; si falta o es la de desarrollo, la aplicación no arranca. En desarrollo, si no hay ninguna, se usa la pública de desarrollo.
string claveCifrado = builder.Configuration["Cifrado:Clave"] ?? "";
if (!builder.Environment.IsDevelopment() && (claveCifrado == "" || claveCifrado == Cifrador.ClaveDeDesarrollo))
    throw new InvalidOperationException("Falta configurar una clave propia en Cifrado:Clave (variable de entorno Cifrado__Clave, 32 bytes en base64).");
Cifrador.Configurar(claveCifrado == "" ? Cifrador.ClaveDeDesarrollo : claveCifrado);

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<CorreoService>();
builder.Services.AddSingleton<ProteccionIngreso>();
builder.Services.AddHostedService<MantenimientoTurnosService>();

var app = builder.Build();

// Los errores que Negocio no puede propagar (para no romper una pantalla) quedan en el log.
var logNegocio = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Negocio");
Negocio.RegistroErrores.Error = (contexto, ex) => logNegocio.LogError(ex, "{Contexto}", contexto);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Si el usuario tiene una contraseña provisoria, solo puede ir a cambiarla (o cerrar sesión)
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    if (context.User.Identity?.IsAuthenticated == true
        && context.User.FindFirst("DebeCambiarPassword")?.Value == "1"
        && !path.StartsWithSegments("/CambiarPassword")
        && !path.StartsWithSegments("/Salir")
        && !Path.HasExtension(path.Value))
    {
        context.Response.Redirect("/CambiarPassword");
        return;
    }
    await next();
});

app.MapRazorPages();

app.Run();
