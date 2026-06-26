using System.ComponentModel;

namespace ComercialPerezGonzales.Models;

public class Producto : INotifyPropertyChanged
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal PrecioVenta { get; set; }
    public decimal PrecioCosto { get; set; }
    public decimal Stock { get; set; }
    public decimal StockMinimo { get; set; }
    public int? CategoriaId { get; set; }
    public string UnidadMedida { get; set; } = "UND";
    public bool Activo { get; set; } = true;

    private string? _imagenPath;
    private byte[]? _imagenData;

    public string? ImagenPath
    {
        get => _imagenPath;
        set { _imagenPath = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImagenPath))); }
    }

    public byte[]? ImagenData
    {
        get => _imagenData;
        set { _imagenData = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImagenData))); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public string? CategoriaNombre { get; set; }

    public override string ToString() => $"[{Codigo}] {Nombre}";
}
