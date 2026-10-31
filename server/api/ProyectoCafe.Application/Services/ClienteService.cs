using ProyectoCafe.Application.Abstractions.Persistence;
using ProyectoCafe.Application.Common;
using ProyectoCafe.Application.DTOs.Ventas;
using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Services;

public sealed class ClienteService(
    IClienteRepository clientes,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private readonly IClienteRepository _clientes = clientes;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<IReadOnlyList<ClienteDto>> GetClientesAsync(CancellationToken ct)
    {
        var list = await _clientes.ListAsync(ct);
        return list.Select(Map).ToArray();
    }

    public async Task<OperationResult<ClienteDto>> GetClienteAsync(int clienteId, CancellationToken ct)
    {
        var cliente = await _clientes.FindAsync(clienteId, ct);
        return cliente is null
            ? OperationResult<ClienteDto>.NotFound()
            : OperationResult<ClienteDto>.Success(Map(cliente));
    }

    public async Task<OperationResult<int>> CreateClienteAsync(
        UpsertClienteRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalize(request, out var data, out var errors))
        {
            return OperationResult<int>.Validation(errors);
        }

        if (await _clientes.NumeroIdentificacionExistsAsync(data.NumeroIdentificacion, null, ct))
        {
            return OperationResult<int>.Validation(Error(
                nameof(UpsertClienteRequest.NumeroIdentificacion),
                "Ya existe un cliente con ese número de identificación."));
        }

        var cliente = new Cliente
        {
            TipoIdentificacion = data.TipoIdentificacion,
            NumeroIdentificacion = data.NumeroIdentificacion,
            Nombre = data.Nombre,
            Telefono = data.Telefono,
            Email = data.Email,
            Direccion = data.Direccion,
            Observacion = data.Observacion,
            Estado = data.Estado ?? EstadoCatalogoCafe.Activo,
            CreadoEn = now,
        };

        try
        {
            await _clientes.AddAsync(cliente, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult<int>.Success(cliente.ClienteId);
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult<int>.Conflict();
        }
    }

    public async Task<OperationResult> UpdateClienteAsync(
        int clienteId,
        UpsertClienteRequest request,
        CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (!TryNormalize(request, out var data, out var errors))
        {
            return OperationResult.Validation(errors);
        }

        var cliente = await _clientes.FindAsync(clienteId, ct);
        if (cliente is null)
        {
            return OperationResult.NotFound();
        }

        if (await _clientes.NumeroIdentificacionExistsAsync(data.NumeroIdentificacion, clienteId, ct))
        {
            return OperationResult.Validation(Error(
                nameof(UpsertClienteRequest.NumeroIdentificacion),
                "Ya existe un cliente con ese número de identificación."));
        }

        cliente.TipoIdentificacion = data.TipoIdentificacion;
        cliente.NumeroIdentificacion = data.NumeroIdentificacion;
        cliente.Nombre = data.Nombre;
        cliente.Telefono = data.Telefono;
        cliente.Email = data.Email;
        cliente.Direccion = data.Direccion;
        cliente.Observacion = data.Observacion;
        if (data.Estado is { } estado)
        {
            cliente.Estado = estado;
        }
        cliente.ActualizadoEn = now;

        try
        {
            await _unitOfWork.SaveChangesAsync(ct);
            return OperationResult.Success();
        }
        catch (ConcurrencyConflictException)
        {
            return OperationResult.Conflict();
        }
    }

    private static bool TryNormalize(
        UpsertClienteRequest request,
        out ClienteNormalizado data,
        out Dictionary<string, string[]> errors)
    {
        data = new ClienteNormalizado();
        errors = new Dictionary<string, string[]>();

        var tipo = (request.TipoIdentificacion ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(tipo))
        {
            AddError(errors, nameof(UpsertClienteRequest.TipoIdentificacion), "El tipo de identificación es requerido.");
        }
        else if (tipo.Length > 10)
        {
            AddError(errors, nameof(UpsertClienteRequest.TipoIdentificacion), "El tipo de identificación no puede exceder 10 caracteres.");
        }
        else
        {
            data.TipoIdentificacion = tipo;
        }

        var numero = (request.NumeroIdentificacion ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(numero))
        {
            AddError(errors, nameof(UpsertClienteRequest.NumeroIdentificacion), "El número de identificación es requerido.");
        }
        else if (numero.Length > 30)
        {
            AddError(errors, nameof(UpsertClienteRequest.NumeroIdentificacion), "El número de identificación no puede exceder 30 caracteres.");
        }
        else
        {
            data.NumeroIdentificacion = numero;
        }

        var nombre = (request.Nombre ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nombre))
        {
            AddError(errors, nameof(UpsertClienteRequest.Nombre), "El nombre o razón social es requerido.");
        }
        else if (nombre.Length > 150)
        {
            AddError(errors, nameof(UpsertClienteRequest.Nombre), "El nombre no puede exceder 150 caracteres.");
        }
        else
        {
            data.Nombre = nombre;
        }

        var telefono = NormalizeOptional(request.Telefono);
        if (telefono is { Length: > 30 })
        {
            AddError(errors, nameof(UpsertClienteRequest.Telefono), "El teléfono no puede exceder 30 caracteres.");
        }
        else
        {
            data.Telefono = telefono;
        }

        var email = NormalizeOptional(request.Email);
        if (email is { Length: > 100 })
        {
            AddError(errors, nameof(UpsertClienteRequest.Email), "El correo no puede exceder 100 caracteres.");
        }
        else if (email is not null && !email.Contains('@'))
        {
            AddError(errors, nameof(UpsertClienteRequest.Email), "El correo no es válido.");
        }
        else
        {
            data.Email = email;
        }

        var direccion = NormalizeOptional(request.Direccion);
        if (direccion is { Length: > 200 })
        {
            AddError(errors, nameof(UpsertClienteRequest.Direccion), "La dirección no puede exceder 200 caracteres.");
        }
        else
        {
            data.Direccion = direccion;
        }

        var observacion = NormalizeOptional(request.Observacion);
        if (observacion is { Length: > 250 })
        {
            AddError(errors, nameof(UpsertClienteRequest.Observacion), "La observación no puede exceder 250 caracteres.");
        }
        else
        {
            data.Observacion = observacion;
        }

        if (request.Estado is { } estado)
        {
            if (!Enum.IsDefined(estado))
            {
                AddError(errors, nameof(UpsertClienteRequest.Estado), "El estado no es válido.");
            }
            else if (estado == EstadoCatalogoCafe.Eliminado)
            {
                AddError(errors, nameof(UpsertClienteRequest.Estado), "Use la operación de eliminación para retirar el cliente.");
            }
            else
            {
                data.Estado = estado;
            }
        }

        return errors.Count == 0;
    }

    private static string? NormalizeOptional(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return normalized.Length == 0 ? null : normalized;
    }

    private static ClienteDto Map(Cliente cliente) =>
        new()
        {
            ClienteId = cliente.ClienteId,
            TipoIdentificacion = cliente.TipoIdentificacion,
            NumeroIdentificacion = cliente.NumeroIdentificacion,
            Nombre = cliente.Nombre,
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            Direccion = cliente.Direccion,
            Observacion = cliente.Observacion,
            Estado = cliente.Estado,
            CreadoEn = cliente.CreadoEn,
            ActualizadoEn = cliente.ActualizadoEn,
        };

    private static Dictionary<string, string[]> Error(string key, string message) =>
        new() { [key] = [message] };

    private static void AddError(Dictionary<string, string[]> errors, string key, string message)
    {
        if (errors.TryGetValue(key, out var current))
        {
            errors[key] = [.. current, message];
            return;
        }

        errors[key] = [message];
    }

    private sealed class ClienteNormalizado
    {
        public string TipoIdentificacion { get; set; } = string.Empty;
        public string NumeroIdentificacion { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Email { get; set; }
        public string? Direccion { get; set; }
        public string? Observacion { get; set; }
        public EstadoCatalogoCafe? Estado { get; set; }
    }
}