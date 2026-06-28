using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using ComercialPerezGonzales.Helpers;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Inventario;

public class ProductosViewModel : ViewModelBase
{
    private readonly ProductoService _service;
    private readonly ProductoConversionService _convService;
    private readonly ImageSearchService _imgSearch;
    private readonly UnidadMedidaService _unidadService;
    private string _searchText = string.Empty;
    private Producto? _selected;
    private bool _modoEdicion;
    private bool _esDerivado;
    private bool _descargandoImagenes;
    private string _estadoDescarga = string.Empty;
    private ProductoConversion? _conversionEdit;
    private bool _isCardView;
    private bool _modoNuevaCategoria;
    private string _nuevaCategoriaNombre = string.Empty;
    private bool _modoNuevaCategoriaFiltro;
    private string _nuevaCategoriaFiltroNombre = string.Empty;
    private int _paginaActual = 1;
    private int _pageSize = 40;
    private int _totalProductos;
    private int _totalPaginas;
    private bool _modoNuevaUnidad;
    private string _nuevaUnidadNombre = string.Empty;
    private bool _modoNuevaUnidadFiltro;
    private string _nuevaUnidadFiltroNombre = string.Empty;

    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<Categoria> Categorias { get; } = new();
    public ObservableCollection<Producto> ProductosBase { get; } = new();
    public ObservableCollection<CategoriaFiltro> CategoriasFiltro { get; } = new();
    public ObservableCollection<UnidadMedida> UnidadesMedida { get; } = new();
    public ObservableCollection<UnidadFiltro> UnidadesFiltro { get; } = new();

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

    public bool EsDerivado
    {
        get => _esDerivado;
        set
        {
            if (SetProperty(ref _esDerivado, value))
            {
                if (_esDerivado && ConversionEdit == null)
                {
                    ConversionEdit = new ProductoConversion();
                }
            }
        }
    }

    public int? SelectedProductoBaseId
    {
        get => ConversionEdit?.ProductoBaseId == 0 ? null : ConversionEdit?.ProductoBaseId;
        set
        {
            if (ConversionEdit != null && value.HasValue)
            {
                ConversionEdit.ProductoBaseId = value.Value;
                OnPropertyChanged(nameof(SelectedProductoBaseId));
                
                var baseProduct = ProductosBase.FirstOrDefault(p => p.Id == value.Value);
                if (baseProduct != null && ProductoEdit != null)
                {
                    ProductoEdit.CategoriaId = baseProduct.CategoriaId;
                }
            }
        }
    }

    public ProductoConversion? ConversionEdit
    {
        get => _conversionEdit;
        set => SetProperty(ref _conversionEdit, value);
    }

    public bool DescargandoImagenes
    {
        get => _descargandoImagenes;
        set => SetProperty(ref _descargandoImagenes, value);
    }

    public string EstadoDescarga
    {
        get => _estadoDescarga;
        set => SetProperty(ref _estadoDescarga, value);
    }

    public bool IsCardView
    {
        get => _isCardView;
        set => SetProperty(ref _isCardView, value);
    }

    public bool ModoNuevaCategoria
    {
        get => _modoNuevaCategoria;
        set => SetProperty(ref _modoNuevaCategoria, value);
    }

    public string NuevaCategoriaNombre
    {
        get => _nuevaCategoriaNombre;
        set => SetProperty(ref _nuevaCategoriaNombre, value);
    }

    public bool ModoNuevaCategoriaFiltro
    {
        get => _modoNuevaCategoriaFiltro;
        set => SetProperty(ref _modoNuevaCategoriaFiltro, value);
    }

    public string NuevaCategoriaFiltroNombre
    {
        get => _nuevaCategoriaFiltroNombre;
        set => SetProperty(ref _nuevaCategoriaFiltroNombre, value);
    }

    public bool ModoNuevaUnidad
    {
        get => _modoNuevaUnidad;
        set => SetProperty(ref _modoNuevaUnidad, value);
    }

    public string NuevaUnidadNombre
    {
        get => _nuevaUnidadNombre;
        set => SetProperty(ref _nuevaUnidadNombre, value);
    }

    public bool ModoNuevaUnidadFiltro
    {
        get => _modoNuevaUnidadFiltro;
        set => SetProperty(ref _modoNuevaUnidadFiltro, value);
    }

    public string NuevaUnidadFiltroNombre
    {
        get => _nuevaUnidadFiltroNombre;
        set => SetProperty(ref _nuevaUnidadFiltroNombre, value);
    }

    public int PaginaActual
    {
        get => _paginaActual;
        set
        {
            if (SetProperty(ref _paginaActual, value))
            {
                Cargar();
                PaginaAnteriorCommand.RaiseCanExecuteChanged();
                PaginaSiguienteCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public int TotalProductos
    {
        get => _totalProductos;
        set => SetProperty(ref _totalProductos, value);
    }

    public int TotalPaginas
    {
        get => _totalPaginas;
        set => SetProperty(ref _totalPaginas, value);
    }

    public string TotalProductosText => $"{TotalProductos} productos";
    public string PaginaActualText => $"Página {PaginaActual}";
    public string PaginaDetalleText => $"Página {PaginaActual} de {TotalPaginas}";

    public Producto? ProductoEdit { get; set; }

    public RelayCommand NuevoCommand { get; }
    public RelayCommand EditarCommand { get; }
    public RelayCommand EliminarCommand { get; }
    public RelayCommand GuardarCommand { get; }
    public RelayCommand CancelarCommand { get; }
    public RelayCommand SeleccionarImagenCommand { get; }
    public RelayCommand QuitarImagenCommand { get; }
    public RelayCommand DescargarImagenesCommand { get; }
    public RelayCommand ModoListaCommand { get; }
    public RelayCommand ModoTarjetaCommand { get; }
    public RelayCommand ActivarNuevaCategoriaCommand { get; }
    public RelayCommand CancelarNuevaCategoriaCommand { get; }
    public RelayCommand GuardarNuevaCategoriaCommand { get; }
    public RelayCommand ActivarNuevaCategoriaFiltroCommand { get; }
    public RelayCommand CancelarNuevaCategoriaFiltroCommand { get; }
    public RelayCommand GuardarNuevaCategoriaFiltroCommand { get; }
    public RelayCommand ActivarNuevaUnidadCommand { get; }
    public RelayCommand CancelarNuevaUnidadCommand { get; }
    public RelayCommand GuardarNuevaUnidadCommand { get; }
    public RelayCommand ActivarNuevaUnidadFiltroCommand { get; }
    public RelayCommand CancelarNuevaUnidadFiltroCommand { get; }
    public RelayCommand GuardarNuevaUnidadFiltroCommand { get; }
    public RelayCommand MarcarTodasCategoriasCommand { get; }
    public RelayCommand DesmarcarTodasCategoriasCommand { get; }
    public RelayCommand MarcarTodasUnidadesCommand { get; }
    public RelayCommand DesmarcarTodasUnidadesCommand { get; }
    public RelayCommand PaginaAnteriorCommand { get; }
    public RelayCommand PaginaSiguienteCommand { get; }

    public ProductosViewModel(ProductoService service, ProductoConversionService convService, ImageSearchService imgSearch, UnidadMedidaService unidadService)
    {
        _service = service;
        _convService = convService;
        _imgSearch = imgSearch;
        _unidadService = unidadService;
        NuevoCommand = new RelayCommand(Nuevo);
        EditarCommand = new RelayCommand(Editar, () => Selected != null);
        EliminarCommand = new RelayCommand(Eliminar, () => Selected != null);
        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => ModoEdicion = false);
        SeleccionarImagenCommand = new RelayCommand(SeleccionarImagen);
        QuitarImagenCommand = new RelayCommand(QuitarImagen, () => !string.IsNullOrEmpty(ProductoEdit?.ImagenPath));
        DescargarImagenesCommand = new RelayCommand(async _ => await DescargarImagenesWeb(), _ => !DescargandoImagenes);
        ModoListaCommand = new RelayCommand(() => IsCardView = false);
        ModoTarjetaCommand = new RelayCommand(() => IsCardView = true);
        ActivarNuevaCategoriaCommand = new RelayCommand(() => { ModoNuevaCategoria = true; NuevaCategoriaNombre = string.Empty; });
        CancelarNuevaCategoriaCommand = new RelayCommand(() => ModoNuevaCategoria = false);
        GuardarNuevaCategoriaCommand = new RelayCommand(GuardarNuevaCategoria, () => !string.IsNullOrWhiteSpace(NuevaCategoriaNombre));
        
        ActivarNuevaCategoriaFiltroCommand = new RelayCommand(() => { ModoNuevaCategoriaFiltro = true; NuevaCategoriaFiltroNombre = string.Empty; });
        CancelarNuevaCategoriaFiltroCommand = new RelayCommand(CancelarNuevaCategoriaFiltro);
        GuardarNuevaCategoriaFiltroCommand = new RelayCommand(GuardarNuevaCategoriaFiltro, () => !string.IsNullOrWhiteSpace(NuevaCategoriaFiltroNombre));
        
        ActivarNuevaUnidadCommand = new RelayCommand(() => { ModoNuevaUnidad = true; NuevaUnidadNombre = string.Empty; });
        CancelarNuevaUnidadCommand = new RelayCommand(() => ModoNuevaUnidad = false);
        GuardarNuevaUnidadCommand = new RelayCommand(GuardarNuevaUnidad, () => !string.IsNullOrWhiteSpace(NuevaUnidadNombre));

        ActivarNuevaUnidadFiltroCommand = new RelayCommand(() => { ModoNuevaUnidadFiltro = true; NuevaUnidadFiltroNombre = string.Empty; });
        CancelarNuevaUnidadFiltroCommand = new RelayCommand(CancelarNuevaUnidadFiltro);
        GuardarNuevaUnidadFiltroCommand = new RelayCommand(GuardarNuevaUnidadFiltro, () => !string.IsNullOrWhiteSpace(NuevaUnidadFiltroNombre));
        
        MarcarTodasCategoriasCommand = new RelayCommand(MarcarTodasCategorias);
        DesmarcarTodasCategoriasCommand = new RelayCommand(DesmarcarTodasCategorias);
        MarcarTodasUnidadesCommand = new RelayCommand(MarcarTodasUnidades);
        DesmarcarTodasUnidadesCommand = new RelayCommand(DesmarcarTodasUnidades);

        PaginaAnteriorCommand = new RelayCommand(
            () => PaginaActual--,
            () => PaginaActual > 1
        );
        PaginaSiguienteCommand = new RelayCommand(
            () => PaginaActual++,
            () => PaginaActual < TotalPaginas
        );

        CargarDatos();
    }

    private void MarcarTodasCategorias()
    {
        foreach (var c in CategoriasFiltro)
        {
            c.OnSelectionChanged = null;
            c.IsSelected = true;
            c.OnSelectionChanged = Buscar;
        }
        Buscar();
    }

    private void DesmarcarTodasCategorias()
    {
        foreach (var c in CategoriasFiltro)
        {
            c.OnSelectionChanged = null;
            c.IsSelected = false;
            c.OnSelectionChanged = Buscar;
        }
        Buscar();
    }

    private void MarcarTodasUnidades()
    {
        foreach (var u in UnidadesFiltro)
        {
            u.OnSelectionChanged = null;
            u.IsSelected = true;
            u.OnSelectionChanged = Buscar;
        }
        Buscar();
    }

    private void DesmarcarTodasUnidades()
    {
        foreach (var u in UnidadesFiltro)
        {
            u.OnSelectionChanged = null;
            u.IsSelected = false;
            u.OnSelectionChanged = Buscar;
        }
        Buscar();
    }

    private void GuardarCategoriaBase(string nombre, Action onSuccess)
    {
        try
        {
            var cat = new Categoria { Nombre = nombre };
            var newId = _service.GuardarCategoria(cat);
            cat.Id = newId;
            
            Categorias.Add(cat);
            var cf = new CategoriaFiltro(cat) { OnSelectionChanged = Buscar };
            CategoriasFiltro.Add(cf);
            
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            AppDialog.Show($"Error al guardar categoría: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GuardarNuevaCategoria()
    {
        KeepContextAndGuardarNuevaCategoria();
    }

    private void KeepContextAndGuardarNuevaCategoria()
    {
        GuardarCategoriaBase(NuevaCategoriaNombre, () =>
        {
            if (ProductoEdit != null)
                ProductoEdit.CategoriaId = Categorias.Last().Id;
            ModoNuevaCategoria = false;
        });
    }

    private void CancelarNuevaCategoriaFiltro()
    {
        ModoNuevaCategoriaFiltro = false;
        NuevaCategoriaFiltroNombre = string.Empty;
    }

    private void GuardarNuevaCategoriaFiltro()
    {
        GuardarCategoriaBase(NuevaCategoriaFiltroNombre, () =>
        {
            ModoNuevaCategoriaFiltro = false;
        });
    }

    private void GuardarUnidadBase(string nombre, Action onSuccess)
    {
        try
        {
            var nombreFmt = nombre.Trim().ToUpper();
            if (UnidadesMedida.Any(u => u.Nombre.Equals(nombreFmt, StringComparison.OrdinalIgnoreCase)))
            {
                AppDialog.Show("Ya existe una unidad de medida con ese nombre.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var nueva = new UnidadMedida { Nombre = nombreFmt };
            _unidadService.Insert(nueva);
            
            UnidadesMedida.Add(nueva);
            var uf = new UnidadFiltro(nueva) { OnSelectionChanged = Buscar };
            UnidadesFiltro.Add(uf);
            
            onSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            AppDialog.Show($"Error al guardar unidad: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void GuardarNuevaUnidad()
    {
        GuardarUnidadBase(NuevaUnidadNombre, () =>
        {
            if (ProductoEdit != null)
            {
                ProductoEdit.UnidadMedida = UnidadesMedida.Last().Nombre;
                OnPropertyChanged(nameof(ProductoEdit));
            }
            ModoNuevaUnidad = false;
        });
    }

    private void CancelarNuevaUnidadFiltro()
    {
        ModoNuevaUnidadFiltro = false;
        NuevaUnidadFiltroNombre = string.Empty;
    }

    private void GuardarNuevaUnidadFiltro()
    {
        GuardarUnidadBase(NuevaUnidadFiltroNombre, () =>
        {
            ModoNuevaUnidadFiltro = false;
        });
    }

    private void CargarCategoriasSiEsNecesario()
    {
        if (Categorias.Count == 0)
        {
            foreach (var c in _service.GetCategorias()) 
            {
                Categorias.Add(c);
                var cf = new CategoriaFiltro(c) { OnSelectionChanged = Buscar };
                CategoriasFiltro.Add(cf);
            }
        }
    }

    public void CargarDatos()
    {
        Cargar();
        CargarUnidadesMedida();
    }

    private void Cargar()
    {
        CargarCategoriasSiEsNecesario();

        Productos.Clear();
        
        var categoriasSeleccionadas = CategoriasFiltro.Where(c => c.IsSelected).Select(c => c.Categoria.Id).ToList();
        var unidadesSeleccionadas = UnidadesFiltro.Where(u => u.IsSelected).Select(u => u.Unidad.Nombre).ToList();
        
        var lista = _service.GetPaged(
            PaginaActual, 
            _pageSize, 
            _searchText, 
            categoriasSeleccionadas, 
            unidadesSeleccionadas, 
            out int totalCount
        );
        
        TotalProductos = totalCount;
        TotalPaginas = (int)Math.Ceiling((double)totalCount / _pageSize);
        if (TotalPaginas == 0) TotalPaginas = 1;
        
        OnPropertyChanged(nameof(TotalProductosText));
        OnPropertyChanged(nameof(PaginaActualText));
        OnPropertyChanged(nameof(PaginaDetalleText));

        foreach (var p in lista)
        {
            Productos.Add(p);
        }

        PaginaAnteriorCommand.RaiseCanExecuteChanged();
        PaginaSiguienteCommand.RaiseCanExecuteChanged();
    }

    private void CargarProductosBase(int? excluirId = null)
    {
        ProductosBase.Clear();
        // Solo productos que NO son derivados pueden ser base
        foreach (var p in _service.GetAll())
        {
            if (excluirId.HasValue && p.Id == excluirId.Value) continue;
            if (!p.EsDerivado) ProductosBase.Add(p);
        }
    }

    private void Buscar()
    {
        if (PaginaActual != 1)
        {
            PaginaActual = 1;
        }
        else
        {
            Cargar();
        }
    }

    private void Nuevo()
    {
        ProductoEdit = new Producto();
        ConversionEdit = null;
        EsDerivado = false;
        CargarProductosBase();
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
        OnPropertyChanged(nameof(SelectedProductoBaseId));
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
            UnidadMedida = Selected.UnidadMedida, Activo = Selected.Activo,
            FechaCaducidad = Selected.FechaCaducidad,
            ImagenData = Selected.ImagenData,
            ImagenPath = Selected.ImagenPath
        };

        var conv = Selected.Conversion;
        if (conv != null)
        {
            ConversionEdit = new ProductoConversion
            {
                Id = conv.Id,
                ProductoId = conv.ProductoId,
                ProductoBaseId = conv.ProductoBaseId,
                Factor = conv.Factor
            };
            EsDerivado = true;
        }
        else
        {
            ConversionEdit = null;
            EsDerivado = false;
        }

        CargarProductosBase(ProductoEdit.Id);
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
        OnPropertyChanged(nameof(SelectedProductoBaseId));
    }

    private void Guardar()
    {
        if (ProductoEdit == null) return;

        if (EsDerivado)
        {
            if (ConversionEdit == null || ConversionEdit.ProductoBaseId == 0)
            {
                AppDialog.Show("Selecciona el producto base y el factor de conversión.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (ConversionEdit.Factor <= 0)
            {
                AppDialog.Show("El factor de conversión debe ser mayor a cero.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            // Si es derivado, forzamos stock y stock minimo a 0
            ProductoEdit.Stock = 0;
            ProductoEdit.StockMinimo = 0;
        }

        try
        {
            int productoId = _service.Guardar(ProductoEdit);
            ProductoEdit.Id = productoId;

            if (EsDerivado && ConversionEdit != null)
            {
                ConversionEdit.ProductoId = productoId;
                _convService.Guardar(ConversionEdit);
            }
            else
            {
                _convService.Eliminar(productoId);
            }

            ModoEdicion = false;
            Cargar();
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
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
            AppDialog.Show(
                $"La imagen pesa {info.Length / 1024} KB. No se permiten imágenes de 1 MB o más.\nSelecciona una imagen más pequeña.",
                "Imagen demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        byte[] bytes = File.ReadAllBytes(dlg.FileName);
        var originalLen = bytes.Length;
        bytes = ImageHelper.ComprimirSiHaceFalta(bytes);
        if (bytes.Length < originalLen)
        {
            AppDialog.Show(
                $"La imagen fue comprimida automáticamente a {bytes.Length / 1024} KB para optimizar el almacenamiento.",
                "Imagen comprimida", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ProductoEdit.ImagenData = bytes;
        ProductoEdit.ImagenPath = dlg.FileName;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private async Task DescargarImagenesWeb()
    {
        var pendientes = _service.GetAll()
            .Where(p => p.ImagenData == null || p.ImagenData.Length == 0)
            .ToList();

        if (pendientes.Count == 0)
        {
            AppDialog.Show("Todos los productos ya tienen imagen.", "Listo",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var r = AppDialog.Show(
            $"Se buscarán y descargarán imágenes desde la web para {pendientes.Count} producto(s).\n\n" +
            "• Tarda unos segundos por producto.\n" +
            "• Las imágenes son automáticas y pueden no coincidir con el producto real.\n" +
            "• Revisa cada una y reemplaza manualmente las incorrectas con el botón 'Seleccionar imagen'.\n\n" +
            "¿Continuar?",
            "Descargar imágenes", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        DescargandoImagenes = true;
        int exito = 0, falla = 0, i = 0;

        foreach (var p in pendientes)
        {
            i++;
            EstadoDescarga = $"Descargando {i}/{pendientes.Count}: {p.Nombre}";
            try
            {
                var bytes = await _imgSearch.DescargarPrimera(p.Nombre);
                if (bytes == null) { falla++; continue; }
                var optimizada = ImageHelper.ComprimirSiHaceFalta(bytes);
                _service.GuardarImagen(p.Id, optimizada);
                exito++;
            }
            catch
            {
                falla++;
            }
        }

        DescargandoImagenes = false;
        EstadoDescarga = string.Empty;
        Cargar();

        AppDialog.Show(
            $"Descarga finalizada.\n\nExitosas: {exito}\nFallidas: {falla}\n\n" +
            "Recuerda revisar cada producto: muchas imágenes pueden no coincidir con el real. " +
            "Reemplaza manualmente las que sean incorrectas.",
            "Resultado", MessageBoxButton.OK, MessageBoxImage.Information);
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
        var r = AppDialog.Show($"¿Eliminar '{Selected.Nombre}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            try
            {
                _service.Eliminar(Selected.Id);
                Cargar();
            }
            catch (Exception ex)
            {
                AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }

    private void CargarUnidadesMedida()
    {
        UnidadesMedida.Clear();
        UnidadesFiltro.Clear();
        foreach (var u in _unidadService.GetAll())
        {
            UnidadesMedida.Add(u);
            var uf = new UnidadFiltro(u) { OnSelectionChanged = Buscar };
            UnidadesFiltro.Add(uf);
        }
        if (UnidadesMedida.Count == 0)
        {
            var defaultUnidad = new UnidadMedida { Nombre = "UND" };
            UnidadesMedida.Add(defaultUnidad);
            var uf = new UnidadFiltro(defaultUnidad) { OnSelectionChanged = Buscar };
            UnidadesFiltro.Add(uf);
        }
    }
}
