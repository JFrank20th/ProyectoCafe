using Microsoft.AspNetCore.Mvc;
using ProyectoCafe.API.Auth;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Application.Services;

namespace ProyectoCafe.API.Endpoints;

public static class VentasEndpoints
{
    public static IEndpointRouteBuilder MapVentasEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/v1/ventas")
            .WithTags("Ventas");

        MapPrecios(group);
        MapClientes(group);
        MapVentas(group);

        return endpoints;
    }

    private static void MapPrecios(IEndpointRouteBuilder group)
    {
        var precios = group.MapGroup("/precios");

        precios.MapGet("", async (
            [FromServices] PrecioVentaService service,
            CancellationToken ct) =>
        {
            var list = await service.GetPreciosAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasPrecios,
            Permissions.Actions.Ver));

        precios.MapGet("/{id:int}", async (
            int id,
            [FromServices] PrecioVentaService service,
            CancellationToken ct) =>
        {
            var result = await service.GetPrecioAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasPrecios,
            Permissions.Actions.Ver));

        precios.MapPost("", async (
            [FromBody] UpsertPrecioVentaRequest request,
            [FromServices] PrecioVentaService service,
            CancellationToken ct) =>
        {
            var result = await service.CreatePrecioAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/ventas/precios/{result.Value}",
                new { precioVentaId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasPrecios,
            Permissions.Actions.Crear));

        precios.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpsertPrecioVentaRequest request,
            [FromServices] PrecioVentaService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdatePrecioAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasPrecios,
            Permissions.Actions.Editar));
    }

    private static void MapClientes(IEndpointRouteBuilder group)
    {
        var clientes = group.MapGroup("/clientes");

        clientes.MapGet("", async (
            [FromServices] ClienteService service,
            CancellationToken ct) =>
        {
            var list = await service.GetClientesAsync(ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasClientes,
            Permissions.Actions.Ver));

        clientes.MapGet("/{id:int}", async (
            int id,
            [FromServices] ClienteService service,
            CancellationToken ct) =>
        {
            var result = await service.GetClienteAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasClientes,
            Permissions.Actions.Ver));

        clientes.MapPost("", async (
            [FromBody] UpsertClienteRequest request,
            [FromServices] ClienteService service,
            CancellationToken ct) =>
        {
            var result = await service.CreateClienteAsync(request, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/ventas/clientes/{result.Value}",
                new { clienteId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasClientes,
            Permissions.Actions.Crear));

        clientes.MapPut("/{id:int}", async (
            int id,
            [FromBody] UpsertClienteRequest request,
            [FromServices] ClienteService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateClienteAsync(id, request, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasClientes,
            Permissions.Actions.Editar));
    }

    private static void MapVentas(IEndpointRouteBuilder group)
    {
        var ventas = group.MapGroup("/ventas");

        ventas.MapGet("", async (
            int? numeroVenta,
            int? clienteId,
            int? estado,
            int? estadoEntrega,
            int? estadoPago,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            [FromServices] VentaService service,
            CancellationToken ct) =>
        {
            var filtro = new VentaFiltro
            {
                NumeroVenta = numeroVenta,
                ClienteId = clienteId,
                Estado = estado,
                EstadoEntrega = estadoEntrega,
                EstadoPago = estadoPago,
                FechaDesde = fechaDesde,
                FechaHasta = fechaHasta,
            };

            var list = await service.GetVentasAsync(filtro, ct);
            return Results.Ok(list);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Ver));

        ventas.MapGet("/{id:int}", async (
            int id,
            [FromServices] VentaService service,
            CancellationToken ct) =>
        {
            var result = await service.GetVentaAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Ver));

        ventas.MapPost("", async (
            [FromBody] CreateVentaRequest request,
            [FromServices] VentaService service,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uid = http.User.FindFirst("uid")?.Value;
            int? usuarioId = int.TryParse(uid, out var parsedUsuarioId)
                ? parsedUsuarioId
                : null;

            var result = await service.CreateVentaAsync(request, usuarioId, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/ventas/ventas/{result.Value}",
                new { ventaId = result.Value });
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Crear));

        ventas.MapGet("/{id:int}/pagos", async (
            int id,
            [FromServices] PagoVentaService service,
            CancellationToken ct) =>
        {
            var result = await service.GetPagosAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Ver));

        ventas.MapGet("/{id:int}/resumen-financiero", async (
            int id,
            [FromServices] PagoVentaService service,
            CancellationToken ct) =>
        {
            var result = await service.GetResumenFinancieroAsync(id, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Ver));

        ventas.MapPost("/{id:int}/pagos", async (
            int id,
            [FromBody] CreatePagoVentaRequest request,
            [FromServices] PagoVentaService service,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uid = http.User.FindFirst("uid")?.Value;
            int? usuarioId = int.TryParse(uid, out var parsedUsuarioId)
                ? parsedUsuarioId
                : null;

            var result = await service.RegisterPagoAsync(id, request, usuarioId, ct);
            if (!result.IsSuccess)
            {
                return Failure(result);
            }

            return Results.Created(
                $"/api/v1/ventas/ventas/{id}/pagos/{result.Value!.PagoVentaId}",
                result.Value);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Crear));

        ventas.MapPut("/{id:int}/estado-entrega", async (
            int id,
            [FromBody] UpdateEstadoEntregaVentaRequest request,
            [FromServices] VentaService service,
            CancellationToken ct) =>
        {
            var result = await service.UpdateEstadoEntregaAsync(id, request.EstadoEntrega, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Editar));

        ventas.MapPost("/{id:int}/cancelar", async (
            int id,
            [FromBody] CancelVentaRequest request,
            [FromServices] VentaService service,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uid = http.User.FindFirst("uid")?.Value;
            int? usuarioId = int.TryParse(uid, out var parsedUsuarioId)
                ? parsedUsuarioId
                : null;

            var result = await service.CancelVentaAsync(id, request.Motivo ?? string.Empty, usuarioId, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Cancelar));

        ventas.MapPost("/{id:int}/anular", async (
            int id,
            [FromBody] AnulateVentaRequest request,
            [FromServices] VentaService service,
            HttpContext http,
            CancellationToken ct) =>
        {
            var uid = http.User.FindFirst("uid")?.Value;
            int? usuarioId = int.TryParse(uid, out var parsedUsuarioId)
                ? parsedUsuarioId
                : null;

            var result = await service.AnulateVentaAsync(id, request.Motivo ?? string.Empty, usuarioId, ct);
            return result.IsSuccess ? Results.NoContent() : Failure(result);
        })
        .RequireAuthorization(Permissions.Policy(
            Permissions.Modulos.VentasVentas,
            Permissions.Actions.Anular));
    }

    private static IResult Failure(OperationResult result) =>
        result.Status switch
        {
            OperationStatus.NotFound => Results.NotFound(),
            OperationStatus.ValidationFailed => Results.ValidationProblem(
                result.Errors.ToDictionary(pair => pair.Key, pair => pair.Value)),
            OperationStatus.Conflict => Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflicto de concurrencia",
                detail: "El precio fue modificado por otra operación. Vuelva a consultar e intentar de nuevo."),
            _ => Results.Problem(),
        };
}