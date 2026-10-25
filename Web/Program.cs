using AccesoDatos;
using Microsoft.AspNetCore.Authentication.Cookies;
using Web.Servicios;

var builder = WebApplication.CreateBuilder(args);

//
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Ingresar";
        options.AccessDeniedPath = "/Ingresar";
    });

// Configuramos la cadena de conexión para ConexionDB desde appsettings / variables de entorno
string? connectionString = builder.Configuration.GetConnectionString("TurnoTigreDB");
if (!string.IsNullOrEmpty(connectionString))
{
    ConexionDB.CadenaConexionDefault = connectionString;
}

// Add services to the container.
builder.Services.AddMemoryCache();
builder.Services.AddRazorPages();
builder.Services.AddSingleton<CorreoService>();
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
