using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Negocio.Ventas;

namespace ProyectoCafe.API.Endpoints;

public static class VentasEndpoints
{
    public static IEndpointRouteBuilder MapVentasEndpoints(this IEndpointRouteBuilder endpoints)
    {
        Map(endpoints, "/api/v1/ventas");
        Map(endpoints, "/api/Ventas"); // compatibilidad con Berry
        return endpoints;
    }

    private static void Map(IEndpointRouteBuilder endpoints, string prefix)
    {
        var group = endpoints
            .MapGroup(prefix)
            .WithTags("Ventas");

        group.MapGet("/", async ([FromServices] IVentasService service, CancellationToken ct) =>
        {
            var list = await service.GetAllAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasVentas, Permissions.Actions.Ver));

        group.MapGet("/{id:int}", async (
            int id,
            [FromServices] IVentasService service,
            CancellationToken ct) =>
        {
            var view = await service.GetViewAsync(id, ct);
            return view is null ? Results.NotFound() : Results.Ok(view);
        })
        .WithName($"{prefix}:GetById")
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasVentas, Permissions.Actions.Ver));

        group.MapPost("/", async (
            [FromBody] VentaCreateRequest request,
            [FromServices] IVentasService service,
            HttpContext httpContext,
            CancellationToken ct) =>
        {
            var errors = Validate(request);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            Normalize(request);

            int ventaId;
            try
            {
                ventaId = await service.CreateAsync(request, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            var basePath = httpContext.Request.Path.Value?.TrimEnd('/') ?? string.Empty;
            var location = $"{basePath}/{ventaId}";
            return Results.Created(location, new { ventaId });
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasVentas, Permissions.Actions.Crear));

        group.MapPost("/{id:int}/anular", async (
            int id,
            [FromServices] IVentasService service,
            CancellationToken ct) =>
        {
            bool annulled;
            try
            {
                annulled = await service.AnnulAsync(id, ct);
            }
            catch (ArgumentException ex)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["request"] = [ex.Message] });
            }

            return annulled ? Results.NoContent() : Results.NotFound();
        })
        .RequireAuthorization(Permissions.Policy(Permissions.Modulos.VentasVentas, Permissions.Actions.Eliminar));
    }

    private static Dictionary<string, string[]> Validate(VentaCreateRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.ClienteId <= 0)
        {
            errors[nameof(request.ClienteId)] = ["ClienteId es requerido."];
        }

        var estadoPago = (request.EstadoPago ?? string.Empty).Trim();
        if (estadoPago.Length > 20)
        {
            errors[nameof(request.EstadoPago)] = ["EstadoPago no puede exceder 20 caracteres."];
        }

        if (request.MetodoPagoId.HasValue && request.MetodoPagoId.Value <= 0)
        {
            errors[nameof(request.MetodoPagoId)] = ["MetodoPagoId es inválido."];
        }

        var tipoPrecio = (request.TipoPrecio ?? string.Empty).Trim();
        if (tipoPrecio.Length > 30)
        {
            errors[nameof(request.TipoPrecio)] = ["TipoPrecio no puede exceder 30 caracteres."];
        }

        if (request.Detalles is null || request.Detalles.Count == 0)
        {
            errors[nameof(request.Detalles)] = ["Detalles son requeridos."];
        }
        else
        {
            for (var i = 0; i < request.Detalles.Count; i++)
            {
                var d = request.Detalles[i];

                if (d.ProductoId <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(d.ProductoId)}"] = ["ProductoId es requerido."];
                }

                if (d.LoteId.HasValue && d.LoteId.Value <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(d.LoteId)}"] = ["LoteId es inválido."];
                }

                if (d.Cantidad <= 0)
                {
                    errors[$"Detalles[{i}].{nameof(d.Cantidad)}"] = ["Cantidad debe ser mayor a 0."];
                }

                if (d.PrecioUnitario.HasValue && d.PrecioUnitario.Value < 0)
                {
                    errors[$"Detalles[{i}].{nameof(d.PrecioUnitario)}"] = ["PrecioUnitario no puede ser negativo."];
                }
            }
        }

        return errors;
    }

    private static void Normalize(VentaCreateRequest request)
    {
        request.EstadoPago = NormalizeOrDefault(request.EstadoPago, "Pagado");
        request.TipoPrecio = NormalizeOrDefault(request.TipoPrecio, "Normal");
    }

    private static string NormalizeOrDefault(string? value, string @default)
    {
        var v = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(v) ? @default : v;
    }
}
