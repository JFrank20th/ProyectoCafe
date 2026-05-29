using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Contabilidad.Costos;

namespace ProyectoCafe.API.Endpoints;

public static class CostosEndpoints
{
    public static IEndpointRouteBuilder MapCostosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/costos");
        Map(endpoints, "/api/Costos"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Costos");

        group.MapGet("/", async ([FromServices] ICostosService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadCostos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] ICostosService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadCostos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] CostoRequest request,
            [FromServices] ICostosService service,
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
            var basePath = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            var location = $"{basePath}/{newId}";
            return Results.Created(location, new { id = newId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadCostos, Permissions.Actions.Crear));

        group.MapPut("/{id:int}", async (
            int id,
            [FromBody] CostoRequest request,
            [FromServices] ICostosService service,
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
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadCostos, Permissions.Actions.Editar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] ICostosService service,
            CancellationToken ct) =>
        {
            var deleted = await service.DeleteAsync(id, ct);
            return deleted ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.ContabilidadCostos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(CostoRequest request)
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

        var tipoCosto = (request.TipoCosto ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(tipoCosto) && tipoCosto.Length > 30)
        {
            errors[nameof(request.TipoCosto)] = ["TipoCosto no puede exceder 30 caracteres."];
        }

        var fuenteModulo = (request.FuenteModulo ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(fuenteModulo) && fuenteModulo.Length > 120)
        {
            errors[nameof(request.FuenteModulo)] = ["FuenteModulo no puede exceder 120 caracteres."];
        }

        if (request.Monto < 0)
        {
            errors[nameof(request.Monto)] = ["Monto no puede ser negativo."];
        }

        if (request.ReferenciaId.HasValue && request.ReferenciaId.Value <= 0)
        {
            errors[nameof(request.ReferenciaId)] = ["ReferenciaId debe ser mayor a 0."];
        }

        if (request.ParcelaId.HasValue && request.ParcelaId.Value <= 0)
        {
            errors[nameof(request.ParcelaId)] = ["ParcelaId debe ser mayor a 0."];
        }

        if (request.LoteId.HasValue && request.LoteId.Value <= 0)
        {
            errors[nameof(request.LoteId)] = ["LoteId debe ser mayor a 0."];
        }

        return errors;
    }

    private static void Normalize(CostoRequest request)
    {
        request.Nombre = (request.Nombre ?? string.Empty).Trim();
        request.TipoCosto = string.IsNullOrWhiteSpace(request.TipoCosto) ? null : request.TipoCosto.Trim();
        request.FuenteModulo = string.IsNullOrWhiteSpace(request.FuenteModulo) ? null : request.FuenteModulo.Trim();
    }
}
