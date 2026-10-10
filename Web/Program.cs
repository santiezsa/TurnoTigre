using AccesoDatos;

var builder = WebApplication.CreateBuilder(args);

// Configuramos la cadena de conexión para ConexionDB desde appsettings / variables de entorno
string? connectionString = builder.Configuration.GetConnectionString("TurnoTigreDB");
if (!string.IsNullOrEmpty(connectionString))
{
    ConexionDB.CadenaConexionDefault = connectionString;
}



// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();

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

app.UseAuthorization();

app.MapRazorPages();

app.Run();
