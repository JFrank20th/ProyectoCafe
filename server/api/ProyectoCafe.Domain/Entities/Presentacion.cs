using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoCafe.Domain.Entities;

public sealed class Presentacion
{
    public int PresentacionId { get; set; }

    public required string Codigo { get; set; }

    public required string Nombre { get; set; }

    public decimal Cantidad { get; set; }

    public required string UnidadMedida { get; set; }

    public decimal PesoGramosLegacy { get; set; }

    [NotMapped]
    public decimal PesoGramos
    {
        get => Cantidad;
        set => Cantidad = value;
    }

    public bool? Estado { get; set; }
}