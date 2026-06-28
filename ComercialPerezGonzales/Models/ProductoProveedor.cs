namespace ComercialPerezGonzales.Models;

public class ProductoProveedor
{
    public int ProductoId { get; set; }
    public int ProveedorId { get; set; }
    public string? CodigoBarraProveedor { get; set; }

    // Helpers
    public string ProveedorNombre { get; set; } = string.Empty;
    public string ProductoNombre { get; set; } = string.Empty;
}
