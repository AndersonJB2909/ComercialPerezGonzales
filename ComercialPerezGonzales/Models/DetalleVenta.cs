namespace ComercialPerezGonzales.Models;

public class DetalleVenta
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnit { get; set; }
    public decimal Descuento { get; set; }
    public decimal Subtotal { get; set; }

    public string? ProductoNombre { get; set; }
    public string? ProductoCodigo { get; set; }
}
