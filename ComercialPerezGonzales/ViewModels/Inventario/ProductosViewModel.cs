using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Inventario;

public class ProductosViewModel : ViewModelBase
{
    private readonly ProductoService _service;
    private string _searchText = string.Empty;
    private Producto? _selected;
    private bool _modoEdicion;

    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<Categoria> Categorias { get; } = new();

    public Producto? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); Buscar(); }
    }

    public bool ModoEdicion
    {
        get => _modoEdicion;
        set => SetProperty(ref _modoEdicion, value);
    }

    public Producto? ProductoEdit { get; set; }

    public RelayCommand NuevoCommand { get; }
    public RelayCommand EditarCommand { get; }
    public RelayCommand EliminarCommand { get; }
    public RelayCommand GuardarCommand { get; }
    public RelayCommand CancelarCommand { get; }
    public RelayCommand SeleccionarImagenCommand { get; }
    public RelayCommand QuitarImagenCommand { get; }

    public ProductosViewModel(ProductoService service)
    {
        _service = service;
        NuevoCommand = new RelayCommand(Nuevo);
        EditarCommand = new RelayCommand(Editar, () => Selected != null);
        EliminarCommand = new RelayCommand(Eliminar, () => Selected != null);
        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => ModoEdicion = false);
        SeleccionarImagenCommand = new RelayCommand(SeleccionarImagen);
        QuitarImagenCommand = new RelayCommand(QuitarImagen, () => !string.IsNullOrEmpty(ProductoEdit?.ImagenPath));
        Cargar();
    }

    private void Cargar()
    {
        Productos.Clear();
        var lista = string.IsNullOrWhiteSpace(_searchText) ? _service.GetAll() : _service.Search(_searchText);
        foreach (var p in lista) Productos.Add(p);

        Categorias.Clear();
        foreach (var c in _service.GetCategorias()) Categorias.Add(c);
    }

    private void Buscar() => Cargar();

    private void Nuevo()
    {
        ProductoEdit = new Producto();
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Editar()
    {
        if (Selected == null) return;
        ProductoEdit = new Producto
        {
            Id = Selected.Id, Codigo = Selected.Codigo, Nombre = Selected.Nombre,
            Descripcion = Selected.Descripcion, PrecioVenta = Selected.PrecioVenta,
            PrecioCosto = Selected.PrecioCosto, Stock = Selected.Stock,
            StockMinimo = Selected.StockMinimo, CategoriaId = Selected.CategoriaId,
            UnidadMedida = Selected.UnidadMedida, Activo = Selected.Activo
        };
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Guardar()
    {
        if (ProductoEdit == null) return;
        try
        {
            _service.Guardar(ProductoEdit);
            ModoEdicion = false;
            Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SeleccionarImagen()
    {
        if (ProductoEdit == null) return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Seleccionar imagen del producto",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*",
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        var info = new FileInfo(dlg.FileName);

        if (info.Length >= 1_048_576)
        {
            MessageBox.Show(
                $"La imagen pesa {info.Length / 1024} KB. No se permiten imágenes de 1 MB o más.\nSelecciona una imagen más pequeña.",
                "Imagen demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        byte[] bytes = File.ReadAllBytes(dlg.FileName);

        if (info.Length > 204_800)
        {
            bytes = ComprimirImagen(bytes, 200_000);
            MessageBox.Show(
                $"La imagen fue comprimida automáticamente a {bytes.Length / 1024} KB para optimizar el almacenamiento.",
                "Imagen comprimida", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ProductoEdit.ImagenData = bytes;
        ProductoEdit.ImagenPath = dlg.FileName;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private static byte[] ComprimirImagen(byte[] original, int maxBytes)
    {
        // Intenta reducir calidad JPEG primero
        for (int calidad = 80; calidad >= 30; calidad -= 10)
        {
            var comprimido = CodificarJpeg(original, calidad);
            if (comprimido.Length <= maxBytes)
                return comprimido;
        }

        // Si aún es grande, reduce resolución a la mitad y reintenta
        try
        {
            using var ms = new MemoryStream(original);
            var src = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            int w = src.PixelWidth / 2;
            int h = src.PixelHeight / 2;
            var scaled = new TransformedBitmap(src, new System.Windows.Media.ScaleTransform(0.5, 0.5));
            var encoder = new JpegBitmapEncoder { QualityLevel = 60 };
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            using var out2 = new MemoryStream();
            encoder.Save(out2);
            return out2.ToArray();
        }
        catch
        {
            return CodificarJpeg(original, 30);
        }
    }

    private static byte[] CodificarJpeg(byte[] original, int calidad)
    {
        using var msIn = new MemoryStream(original);
        var bitmap = BitmapFrame.Create(msIn, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var encoder = new JpegBitmapEncoder { QualityLevel = calidad };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var msOut = new MemoryStream();
        encoder.Save(msOut);
        return msOut.ToArray();
    }

    private void QuitarImagen()
    {
        if (ProductoEdit == null) return;
        ProductoEdit.ImagenData = null;
        ProductoEdit.ImagenPath = null;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Eliminar()
    {
        if (Selected == null) return;
        var r = MessageBox.Show($"¿Eliminar '{Selected.Nombre}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            _service.Eliminar(Selected.Id);
            Cargar();
        }
    }
}
