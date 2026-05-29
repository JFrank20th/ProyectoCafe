namespace ProyectoCafe.Application.Contabilidad.Pasivos;

public sealed class PasivoResponse
{
    public int PasivoId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public decimal Monto { get; set; }

    public DateTime? Fecha { get; set; }

    public bool Estado { get; set; }
}
