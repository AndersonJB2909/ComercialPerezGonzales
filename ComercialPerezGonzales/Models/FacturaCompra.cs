using System;

namespace ComercialPerezGonzales.Models;

public class FacturaCompra
{
    public int Id { get; set; }
    public int? OrdenCompraId { get; set; }
    public int ProveedorId { get; set; }
    public string NumeroFactura { get; set; } = string.Empty;
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    
    public decimal Subtotal { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Total { get; set; }
    public decimal SaldoPendiente { get; set; }
    
    public string Estado { get; set; } = "PENDIENTE"; // PAGADA, PENDIENTE, VENCIDA
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Helpers
    public string ProveedorNombre { get; set; } = string.Empty;
    public string? OrdenCompraNumero { get; set; }
}
