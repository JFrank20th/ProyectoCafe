using NLog.Web;
using ProyectoCafe.API.Extensions;
using ProyectoCafe.API.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddProyectoCafeApi(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors("Portal");

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
