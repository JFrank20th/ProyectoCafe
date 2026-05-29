using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Presentaciones;

namespace ProyectoCafe.API.Endpoints;

public static class PresentacionesEndpoints
{
    public static IEndpointRouteBuilder MapPresentacionesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/presentaciones");
        Map(endpoints, "/api/v1/presentaciones-cafe");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Presentaciones");

        group.MapGet("/", async ([FromServices] IPresentacionesService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IPresentacionesService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] PresentacionRequest request,
            [FromServices] IPresentacionesService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);
            var id = await service.CreateAsync(request, ct);
            var location = $"{httpContext.Request.Path}/{id}";
            return Results.Created(location, new { id });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] PresentacionRequest request,
            [FromServices] IPresentacionesService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);
            var updated = await service.UpdateAsync(id, request, ct);
            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IPresentacionesService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioPresentaciones, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(PresentacionRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var codigo = (request.Codigo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(codigo))
        {
            errors[nameof(request.Codigo)] = ["Codigo es requerido."];
        }
        else if (codigo.Length > 30)
        {
            errors[nameof(request.Codigo)] = ["Codigo no puede exceder 30 caracteres."];
        }

        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }
        else if (nombre.Length > 120)
        {
            errors[nameof(request.Nombre)] = ["Nombre no puede exceder 120 caracteres."];
        }

        if (request.Cantidad <= 0)
        {
            errors[nameof(request.Cantidad)] = ["Cantidad debe ser mayor a 0."];
        }

        var unidadMedida = (request.UnidadMedida ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(unidadMedida))
        {
            errors[nameof(request.UnidadMedida)] = ["UnidadMedida es requerida."];
        }
        else if (unidadMedida.Length > 20)
        {
            errors[nameof(request.UnidadMedida)] = ["UnidadMedida no puede exceder 20 caracteres."];
        }

        return errors;
    }

    private static void Normalize(PresentacionRequest request)
    {
        request.Codigo = (request.Codigo ?? string.Empty).Trim();
        request.Nombre = (request.Nombre ?? string.Empty).Trim();
        request.UnidadMedida = (request.UnidadMedida ?? string.Empty).Trim().ToUpperInvariant();
    }
}