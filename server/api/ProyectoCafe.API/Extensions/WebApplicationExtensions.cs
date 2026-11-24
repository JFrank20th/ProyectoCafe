using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ProyectoCafe.API.Auth;
using ProyectoCafe.API.Endpoints;

namespace ProyectoCafe.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapAdminEndpoints();
        app.MapInventarioCafeEndpoints();
        app.MapVentasEndpoints();

        // Liveness: solo confirma que el proceso responde. No consulta la BD.
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .AllowAnonymous();

        // Readiness: comprueba dependencias (SQL Server). No expone descripciones,
        // excepciones ni cadenas de conexión; solo el estado agregado.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthResponse,
        })
            .WithTags("Health")
            .AllowAnonymous();

        // El catálogo de permisos se construye a partir de los endpoints ya registrados.
        app.Services.GetRequiredService<PermissionCatalogService>()
            .Attach(((IEndpointRouteBuilder)app).DataSources);

        return app;
    }

    private static Task WriteHealthResponse(HttpContext context, HealthReport report) =>
        context.Response.WriteAsJsonAsync(new { status = report.Status.ToString() });
}
