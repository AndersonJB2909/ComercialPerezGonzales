namespace ComercialPerezGonzales.Models;

public class ProductoConversion
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int ProductoBaseId { get; set; }
    public decimal Factor { get; set; }

    // Cargado por JOIN al consultar
    public decimal StockBase { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoBaseNombre { get; set; } = string.Empty;
}
