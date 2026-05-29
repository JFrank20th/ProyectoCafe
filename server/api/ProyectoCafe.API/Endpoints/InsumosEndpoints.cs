using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.Insumos;

namespace ProyectoCafe.API.Endpoints;

public static class InsumosEndpoints
{
    public static IEndpointRouteBuilder MapInsumosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/insumos");
        Map(endpoints, "/api/Insumos"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Insumos");

        group.MapGet("/", async ([FromServices] IInsumosService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioInsumos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IInsumosService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioInsumos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] InsumoRequest request,
            [FromServices] IInsumosService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

            int newId;
            try
            {
                newId = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioInsumos, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] InsumoRequest request,
            [FromServices] IInsumosService service,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

            bool updated;
            try
            {
                updated = await service.UpdateAsync(id, request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return updated ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioInsumos, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IInsumosService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioInsumos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(InsumoRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            errors[nameof(request.Nombre)] = ["Nombre es requerido."];
        }
        else if (nombre.Length > 150)
        {
            errors[nameof(request.Nombre)] = ["Nombre no puede exceder 150 caracteres."];
        }

        var descripcion = (request.Descripcion ?? string.Empty).Trim();
        if (!string.IsNullOrEmpty(descripcion) && descripcion.Length > 250)
        {
            errors[nameof(request.Descripcion)] = ["Descripcion no puede exceder 250 caracteres."];
        }

        if (request.Cantidad is not null && request.Cantidad < 0)
        {
            errors[nameof(request.Cantidad)] = ["Cantidad no puede ser negativa."];
        }

        if (request.CostoUnitario is not null && request.CostoUnitario < 0)
        {
            errors[nameof(request.CostoUnitario)] = ["CostoUnitario no puede ser negativo."];
        }

        var unidad = (request.UnidadMedida ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(unidad) && unidad.Length > 20)
        {
            errors[nameof(request.UnidadMedida)] = ["UnidadMedida no puede exceder 20 caracteres."];
        }

        return errors;
    }

    private static void Normalize(InsumoRequest request)
    {
        request.Nombre = (request.Nombre ?? string.Empty).Trim();

        var descripcion = (request.Descripcion ?? string.Empty).Trim();
        request.Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion;

        var unidad = (request.UnidadMedida ?? string.Empty).Trim().ToUpperInvariant();
        request.UnidadMedida = string.IsNullOrWhiteSpace(unidad) ? null : unidad;
    }
}
