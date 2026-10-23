namespace ProyectoCafe.Application.Contabilidad.Costos;

public static class TiposCostoContable
{
    public const string Manual = "Manual";
    public const string OperacionAgricola = "OperacionAgricola";
    public const string CosechaCafe = "CosechaCafe";
    public const string ProcesoInventario = "ProcesoInventario";

    public static readonly string[] All = [Manual, OperacionAgricola, CosechaCafe, ProcesoInventario];

    public static string Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();

        if (normalized.Equals(OperacionAgricola, StringComparison.OrdinalIgnoreCase))
        {
            return OperacionAgricola;
        }

        if (normalized.Equals(CosechaCafe, StringComparison.OrdinalIgnoreCase))
        {
            return CosechaCafe;
        }

        if (normalized.Equals(ProcesoInventario, StringComparison.OrdinalIgnoreCase))
        {
            return ProcesoInventario;
        }

        return Manual;
    }
}
