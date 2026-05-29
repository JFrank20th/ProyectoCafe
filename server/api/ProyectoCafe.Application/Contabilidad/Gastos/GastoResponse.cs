namespace ProyectoCafe.Application.Contabilidad.Gastos;

public sealed class GastoResponse
{
    public int GastoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; }
}
