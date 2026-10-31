using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.DTOs.Ventas;

public sealed class UpsertClienteRequest
{
    public string? TipoIdentificacion { get; set; }

    public string? NumeroIdentificacion { get; set; }

    public string? Nombre { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public string? Observacion { get; set; }

    public EstadoCatalogoCafe? Estado { get; set; }
}

public sealed class ClienteDto
{
    public required int ClienteId { get; set; }
    public required string TipoIdentificacion { get; set; }
    public required string NumeroIdentificacion { get; set; }
    public required string Nombre { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Observacion { get; set; }
    public required EstadoCatalogoCafe Estado { get; set; }
    public required DateTime CreadoEn { get; set; }
    public DateTime? ActualizadoEn { get; set; }
}