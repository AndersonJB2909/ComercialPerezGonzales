namespace ComercialPerezGonzales.Models;

public class MovimientoCaja
{
    public int Id { get; set; }
    public int CierreCajaId { get; set; }
    public DateTime FechaHora { get; set; }
    public string Tipo { get; set; } = "SALIDA";   // ENTRADA | SALIDA
    public string Concepto { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public string? Referencia { get; set; }
    public string? UsuarioNombre { get; set; }
}
