using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Categorias;

namespace ProyectoCafe.API.Endpoints;

public static class CategoriasEndpoints
{
    public static IEndpointRouteBuilder MapCategoriasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/categorias");
        Map(endpoints, "/api/Categorias"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Categorias");

        group.MapGet("/", async ([FromServices] ICategoriasService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioCategorias, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ICategoriasService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioCategorias, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] CategoriaRequest request,
            [FromServices] ICategoriasService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre es requerido."] });
            }

            var newId = await service.CreateAsync(request, ct);
            var location = $"{httpContext.Request.Path}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioCategorias, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] CategoriaRequest request,
            [FromServices] ICategoriasService service,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Nombre))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { [nameof(request.Nombre)] = ["Nombre es requerido."] });
            }

            var updated = await service.UpdateAsync(id, request, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioCategorias, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ICategoriasService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioCategorias, Permissions.Actions.Eliminar));
    }
}
