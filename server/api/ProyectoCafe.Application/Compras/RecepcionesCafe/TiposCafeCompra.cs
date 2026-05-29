namespace ProyectoCafe.Application.Compras.RecepcionesCafe;

public static class TiposCafeCompra
{
    public const string Cereza = "Cereza";
    public const string RecienLavado = "RecienLavado";
    public const string Seco = "Seco";

    public static readonly string[] All = [Cereza, RecienLavado, Seco];

    public static string? Normalize(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (normalized.Equals(Cereza, StringComparison.OrdinalIgnoreCase))
        {
            return Cereza;
        }

        if (normalized.Equals(RecienLavado, StringComparison.OrdinalIgnoreCase))
        {
            return RecienLavado;
        }

        if (normalized.Equals(Seco, StringComparison.OrdinalIgnoreCase))
        {
            return Seco;
        }

        return null;
    }
}
