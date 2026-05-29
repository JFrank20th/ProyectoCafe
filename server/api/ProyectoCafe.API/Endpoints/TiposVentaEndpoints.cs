using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Negocio.TiposVenta;

namespace ProyectoCafe.API.Endpoints;

public static class TiposVentaEndpoints
{
    public static IEndpointRouteBuilder MapTiposVentaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/tipos-venta");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("TiposVenta");

        group.MapGet("/", async ([FromServices] ITiposVentaService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasTiposVenta, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ITiposVentaService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasTiposVenta, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] TipoVentaRequest request,
            [FromServices] ITiposVentaService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasTiposVenta, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] TipoVentaRequest request,
            [FromServices] ITiposVentaService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasTiposVenta, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ITiposVentaService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasTiposVenta, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(TipoVentaRequest request)
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
        else if (nombre.Length > 100)
        {
            errors[nameof(request.Nombre)] = ["Nombre no puede exceder 100 caracteres."];
        }

        if (request.Orden.HasValue && request.Orden.Value < 0)
        {
            errors[nameof(request.Orden)] = ["Orden no puede ser negativo."];
        }

        return errors;
    }

    private static void Normalize(TipoVentaRequest request)
    {
        request.Codigo = (request.Codigo ?? string.Empty).Trim();
        request.Nombre = (request.Nombre ?? string.Empty).Trim();
    }
}
