using System;

namespace ComercialPerezGonzales.Models;

public class PagoProveedor
{
    public int Id { get; set; }
    public int FacturaCompraId { get; set; }
    public int? CierreCajaId { get; set; } // Opcional: si salió de la caja diaria
    public DateTime FechaPago { get; set; } = DateTime.Now;
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string? Referencia { get; set; }
    public string? UsuarioNombre { get; set; }
}
