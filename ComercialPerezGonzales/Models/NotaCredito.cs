using System;

namespace ComercialPerezGonzales.Models;

public class NotaCredito
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public int? ClienteId { get; set; }
    public decimal MontoInicial { get; set; }
    public decimal MontoDisponible { get; set; }
    public string Estado { get; set; } = "ACTIVA"; // "ACTIVA", "USADA", "VENCIDA"
    public DateTime FechaEmision { get; set; } = DateTime.Now;
    public DateTime FechaVencimiento { get; set; }

    public string? ClienteNombre { get; set; }
}
