using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Produccion.Fincas;

namespace ProyectoCafe.API.Endpoints;

public static class FincasEndpoints
{
    public static IEndpointRouteBuilder MapFincasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/produccion/fincas");
        Map(endpoints, "/api/Produccion/Fincas");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Fincas");

        group.MapGet("/", async ([FromServices] IFincasService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionFincas, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IFincasService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionFincas, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] FincaRequest request,
            [FromServices] IFincasService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var newId = await service.CreateAsync(request, ct);
            var location = $"{httpContext.Request.Path}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionFincas, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] FincaRequest request,
            [FromServices] IFincasService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var updated = await service.UpdateAsync(id, request, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionFincas, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IFincasService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ProduccionFincas, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(FincaRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }

        if (request.AreaHectareas.HasValue && request.AreaHectareas.Value < 0)
        {
            errors[nameof(request.AreaHectareas)] = ["AreaHectareas no puede ser negativa."];
        }

        return errors;
    }
}
