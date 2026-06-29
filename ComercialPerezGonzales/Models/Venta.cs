namespace ComercialPerezGonzales.Models;

public class Venta
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Descuento { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public string MetodoPago { get; set; } = "EFECTIVO";
    public decimal MontoRecibido { get; set; }
    public decimal Cambio { get; set; }
    public decimal PagoEfectivo { get; set; }
    public decimal PagoTarjeta { get; set; }
    public decimal PagoTransferencia { get; set; }
    public string? ReferenciaTransferencia { get; set; }
    public string Estado { get; set; } = "COMPLETADA";
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; }

    public string? ClienteNombre { get; set; }
    public List<DetalleVenta> Detalles { get; set; } = new();
}
