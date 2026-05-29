using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Negocio.MetodosPago;

namespace ProyectoCafe.API.Endpoints;

public static class MetodosPagoEndpoints
{
    public static IEndpointRouteBuilder MapMetodosPagoEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/metodos-pago");
        Map(endpoints, "/api/MetodosPago"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("MetodosPago");

        group.MapGet("/", async ([FromServices] IMetodosPagoService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasMetodosPago, Permissions.Actions.Ver));
    }
}
