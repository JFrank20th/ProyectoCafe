using ProyectoCafe.Domain.Entities;

namespace ProyectoCafe.Application.Abstractions.Persistence;

public interface ICatalogoCafeRepository
{
    Task<IReadOnlyList<PresentacionCafe>> ListPresentacionesAsync(CancellationToken ct);

    Task<PresentacionCafe?> FindPresentacionAsync(int presentacionCafeId, CancellationToken ct);

    Task<bool> PresentacionCodigoExistsAsync(
        string codigo,
        int? excludePresentacionCafeId,
        CancellationToken ct);

    Task<bool> PresentacionNombreExistsAsync(
        string nombre,
        int? excludePresentacionCafeId,
        CancellationToken ct);

    Task AddPresentacionAsync(PresentacionCafe presentacion, CancellationToken ct);

    Task<bool> PresentacionHasExistenciasAsync(int presentacionCafeId, CancellationToken ct);

    Task<bool> PresentacionHasStockAsync(int presentacionCafeId, CancellationToken ct);

    Task<IReadOnlyList<TipoCafe>> ListTiposCafeAsync(CancellationToken ct);

    Task<TipoCafe?> FindTipoCafeAsync(int tipoCafeId, CancellationToken ct);

    Task<bool> TipoCafeCodigoExistsAsync(string codigo, int? excludeTipoCafeId, CancellationToken ct);

    Task<bool> TipoCafeNombreExistsAsync(string nombre, int? excludeTipoCafeId, CancellationToken ct);

    Task AddTipoCafeAsync(TipoCafe tipoCafe, CancellationToken ct);

    Task<bool> TipoCafeHasStockAsync(int tipoCafeId, CancellationToken ct);

    Task<IReadOnlyList<VariedadCafe>> ListVariedadesAsync(CancellationToken ct);

    Task<VariedadCafe?> FindVariedadAsync(int variedadCafeId, CancellationToken ct);

    Task<bool> VariedadCodigoExistsAsync(string codigo, int? excludeVariedadCafeId, CancellationToken ct);

    Task<bool> VariedadNombreExistsAsync(string nombre, int? excludeVariedadCafeId, CancellationToken ct);

    Task AddVariedadAsync(VariedadCafe variedadCafe, CancellationToken ct);

    Task<IReadOnlyList<TipoTuesteCafe>> ListTiposTuesteAsync(CancellationToken ct);

    Task<TipoTuesteCafe?> FindTipoTuesteAsync(int tipoTuesteCafeId, CancellationToken ct);

    Task<bool> TipoTuesteCodigoExistsAsync(string codigo, int? excludeTipoTuesteCafeId, CancellationToken ct);

    Task<bool> TipoTuesteNombreExistsAsync(string nombre, int? excludeTipoTuesteCafeId, CancellationToken ct);

    Task AddTipoTuesteAsync(TipoTuesteCafe tipoTuesteCafe, CancellationToken ct);

    Task<IReadOnlyList<TipoProcesoCafe>> ListTiposProcesoAsync(CancellationToken ct);

    Task<TipoProcesoCafe?> FindTipoProcesoAsync(int tipoProcesoCafeId, CancellationToken ct);

    Task<bool> TipoProcesoCodigoExistsAsync(string codigo, int? excludeTipoProcesoCafeId, CancellationToken ct);

    Task<bool> TipoProcesoNombreExistsAsync(string nombre, int? excludeTipoProcesoCafeId, CancellationToken ct);

    Task AddTipoProcesoAsync(TipoProcesoCafe tipoProcesoCafe, CancellationToken ct);
}
