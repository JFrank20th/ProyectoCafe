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
            [FromServices] VentaService service,
            CancellationToken ct) =>
        {
            var list = await service.GetVentasAsync(ct);
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
            CancellationToken ct) =>
        {
            var result = await service.CreateVentaAsync(request, ct);
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