using ProyectoCafe.API.Endpoints;

namespace ProyectoCafe.API.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapAuthEndpoints();
        app.MapAdminEndpoints();

        app.MapCostosEndpoints();
        app.MapGastosEndpoints();
        app.MapPasivosEndpoints();

        app.MapCategoriasEndpoints();
        app.MapUnidadesMedidaEndpoints();
        app.MapProductosEndpoints();
        app.MapPresentacionesEndpoints();
        app.MapLotesEndpoints();
        app.MapInsumosEndpoints();
        app.MapFincasEndpoints();
        app.MapParcelasEndpoints();
        app.MapOperacionesAgricolasEndpoints();
        app.MapTransformacionesCafeEndpoints();
        app.MapEmpaquesCafeEndpoints();
        app.MapProveedoresEndpoints();
        app.MapRecepcionesCafeEndpoints();
        app.MapDashboardEndpoints();
        app.MapClientesEndpoints();
        app.MapTiposVentaEndpoints();
        app.MapMetodosPagoEndpoints();
        app.MapVentasEndpoints();

        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithTags("Health")
            .AllowAnonymous();

        return app;
    }
}
