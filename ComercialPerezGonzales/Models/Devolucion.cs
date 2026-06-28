using System;
using System.Collections.Generic;

namespace ComercialPerezGonzales.Models;

public class Devolucion
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int CierreCajaId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Motivo { get; set; } = string.Empty;
    public decimal MontoSubtotal { get; set; }
    public decimal MontoDescuento { get; set; }
    public decimal MontoImpuesto { get; set; }
    public decimal MontoTotal { get; set; }
    public string MetodoReembolso { get; set; } = "EFECTIVO";
    public string SupervisorAutorizo { get; set; } = string.Empty;
    public string CajeroSolicito { get; set; } = string.Empty;
    public string? NotaCreditoCodigo { get; set; }

    // Navigation and helpers
    public string? VentaNumero { get; set; }
    public decimal? NotaCreditoDisponible { get; set; }
    public List<DetalleDevolucion> Detalles { get; set; } = new();
}

public class DetalleDevolucion
{
    public int Id { get; set; }
    public int DevolucionId { get; set; }
    public int ProductoId { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnit { get; set; }
    public decimal Subtotal { get; set; }
    public string EstadoProducto { get; set; } = "STOCK"; // "STOCK" or "MERMA"

    public string? ProductoNombre { get; set; }
    public string? ProductoCodigo { get; set; }
}
