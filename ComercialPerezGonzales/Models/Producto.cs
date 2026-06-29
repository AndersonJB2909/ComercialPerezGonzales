using System;
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
    private int? _categoriaId;
    public int? CategoriaId
    {
        get => _categoriaId;
        set { _categoriaId = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CategoriaId))); }
    }
    public string UnidadMedida { get; set; } = "UND";
    public bool Activo { get; set; } = true;

    private DateTime? _fechaCaducidad;
    public DateTime? FechaCaducidad
    {
        get => _fechaCaducidad;
        set { _fechaCaducidad = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FechaCaducidad))); }
    }

    public int DiasParaVencer => FechaCaducidad.HasValue ? (FechaCaducidad.Value.Date - DateTime.Today).Days : 9999;
    public bool EstaVencido => FechaCaducidad.HasValue && DiasParaVencer < 0;
    public bool PorVencer => FechaCaducidad.HasValue && DiasParaVencer >= 0 && FechaCaducidad.Value.Date <= DateTime.Today.AddMonths(3);
    public string AlertaTexto => EstaVencido ? "Vencido" : $"Vence en {DiasParaVencer} días";
    public string AlertaColor => EstaVencido ? "#EF4444" : "#F59E0B";

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

    public ProductoConversion? Conversion { get; set; }

    public decimal StockEfectivo => Conversion != null
        ? Math.Floor(Conversion.StockBase / Conversion.Factor)
        : Stock;

    public bool EsDerivado => Conversion != null;
    public bool TieneStock => StockEfectivo > 0;

    public override string ToString() => $"[{Codigo}] {Nombre}";
}
