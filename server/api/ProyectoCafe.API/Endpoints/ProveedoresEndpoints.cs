using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Compras.Proveedores;

namespace ProyectoCafe.API.Endpoints;

public static class ProveedoresEndpoints
{
    public static IEndpointRouteBuilder MapProveedoresEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/compras/proveedores");
        Map(endpoints, "/api/Compras/Proveedores");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Proveedores");

        group.MapGet("/", async ([FromServices] IProveedoresService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasProveedores, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IProveedoresService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasProveedores, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] ProveedorRequest request,
            [FromServices] IProveedoresService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasProveedores, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] ProveedorRequest request,
            [FromServices] IProveedoresService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasProveedores, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IProveedoresService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ComprasProveedores, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(ProveedorRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }

        return errors;
    }
}
