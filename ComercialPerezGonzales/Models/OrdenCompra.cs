using System;
using System.Collections.Generic;

namespace ComercialPerezGonzales.Models;

public class OrdenCompra
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string Estado { get; set; } = "BORRADOR"; // BORRADOR, ENVIADA, RECIBIDA_PARCIAL, RECIBIDA_COMPLETA, CANCELADA
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public DateTime? FechaEsperada { get; set; }
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<DetalleOrdenCompra> Detalles { get; set; } = new();

    // Helpers
    public Proveedor? Proveedor { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
}

public class DetalleOrdenCompra
{
    public int Id { get; set; }
    public int OrdenCompraId { get; set; }
    public int ProductoId { get; set; }
    public decimal CantidadSolicitada { get; set; }
    public decimal CantidadRecibida { get; set; }
    public decimal CostoUnitario { get; set; }

    // Helpers
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoCodigo { get; set; } = string.Empty;
    public decimal Subtotal => CantidadSolicitada * CostoUnitario;
}
