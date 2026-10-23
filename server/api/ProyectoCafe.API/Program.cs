using NLog.Web;
using ProyectoCafe.API.Extensions;
using ProyectoCafe.API.Auth;
using ProyectoCafe.API.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddProyectoCafeApi(builder.Configuration);

var app = builder.Build();

// Valida CORS al arrancar (ya con todos los proveedores de configuración cargados).
// Falla si no hay orígenes, si hay comodines o si algún origen no es válido.
CorsOrigins.Resolve(app.Configuration);

app.UseExceptionHandler();

app.UseCors(CorsPolicyNames.Portal);

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<CsrfMiddleware>();

if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.MapApiEndpoints();

app.Run();

public partial class Program { }
