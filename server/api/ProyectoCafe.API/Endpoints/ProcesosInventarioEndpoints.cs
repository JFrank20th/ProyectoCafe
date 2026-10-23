using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Inventario.ProcesosInventario;

namespace ProyectoCafe.API.Endpoints;

public static class ProcesosInventarioEndpoints
{
    public static IEndpointRouteBuilder MapProcesosInventarioEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/procesos-inventario");
        Map(endpoints, "/api/Inventario/ProcesosInventario");
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("ProcesosInventario");

        group.MapGet("/", async ([FromServices] IProcesosInventarioService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProcesos, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IProcesosInventarioService service,
            CancellationToken ct) =>
        {
            var item = await service.GetByIdAsync(id, ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProcesos, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] ProcesoInventarioCreateRequest request,
            [FromServices] IProcesosInventarioService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

            ProcesoInventarioCreateResult result;
            try
            {
                result = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var location = $"{httpContext.Request.Path}/{result.ProcesoInventarioId}";
            return Results.Created(location, result);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProcesos, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] IProcesosInventarioService service,
            CancellationToken ct) =>
        {
            bool canceled;
            try
            {
                canceled = await service.CancelAsync(id, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return canceled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProcesos, Permissions.Actions.Eliminar));

        group.MapDelete("/{id:int}", async (
            int id,
            [FromServices] IProcesosInventarioService service,
            CancellationToken ct) =>
        {
            bool canceled;
            try
            {
                canceled = await service.CancelAsync(id, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return canceled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.InventarioProcesos, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(ProcesoInventarioCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.RecepcionCafeId <= 0)
        {
            errors[nameof(request.RecepcionCafeId)] = ["RecepcionCafeId es requerido."];
        }

        if (request.ProductoSalidaId <= 0)
        {
            errors[nameof(request.ProductoSalidaId)] = ["ProductoSalidaId es requerido."];
        }

        if (!request.UsaLoteCompleto && (!request.CantidadOrigenConsumida.HasValue || request.CantidadOrigenConsumida.Value <= 0))
        {
            errors[nameof(request.CantidadOrigenConsumida)] = ["CantidadOrigenConsumida debe ser mayor a 0 cuando el lote no se usa completo."];
        }

        if (request.CantidadSalida <= 0)
        {
            errors[nameof(request.CantidadSalida)] = ["CantidadSalida debe ser mayor a 0."];
        }

        if (request.CostoProceso < 0)
        {
            errors[nameof(request.CostoProceso)] = ["CostoProceso no puede ser negativo."];
        }

        var codigoLoteSalida = (request.CodigoLoteSalida ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(codigoLoteSalida) && codigoLoteSalida.Length > 50)
        {
            errors[nameof(request.CodigoLoteSalida)] = ["CodigoLoteSalida no puede exceder 50 caracteres."];
        }

        var observaciones = (request.Observaciones ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(observaciones) && observaciones.Length > 250)
        {
            errors[nameof(request.Observaciones)] = ["Observaciones no puede exceder 250 caracteres."];
        }

        return errors;
    }

    private static void Normalize(ProcesoInventarioCreateRequest request)
    {
        var codigoLoteSalida = (request.CodigoLoteSalida ?? string.Empty).Trim().ToUpperInvariant();
        request.CodigoLoteSalida = string.IsNullOrWhiteSpace(codigoLoteSalida) ? null : codigoLoteSalida;

        var observaciones = (request.Observaciones ?? string.Empty).Trim();
        request.Observaciones = string.IsNullOrWhiteSpace(observaciones) ? null : observaciones;
    }
}