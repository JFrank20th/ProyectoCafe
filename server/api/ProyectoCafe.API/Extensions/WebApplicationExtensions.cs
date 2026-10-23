using Microsoft.Extensions.DependencyInjection;
using ProyectoCafe.API.Auth;
using ProyectoCafe.API.Endpoints;

namespace ProyectoCafe.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapAdminEndpoints();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .AllowAnonymous();

        // El catálogo de permisos se construye a partir de los endpoints ya registrados.
        app.Services.GetRequiredService<PermissionCatalogService>()
            .Attach(((IEndpointRouteBuilder)app).DataSources);

        return app;
    }
}
