using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Proveedores;

public class ProveedoresViewModel : ViewModelBase
{
    private readonly ProveedorService _proveedorService;
    private readonly OrdenCompraService _ordenService;
    private readonly FacturaCompraService _facturaService;
    private readonly ProductoService _productoService;

    // ── Estado general ────────────────────────────────────────────────────────────
    private string _searchText = "";
    private Proveedor? _selected;
    private bool _modoDetalle;

    // ── Expediente ────────────────────────────────────────────────────────────────
    private Proveedor? _proveedorEdit;

    // ── Órdenes ───────────────────────────────────────────────────────────────────
    private bool _modoFormOrden;
    private OrdenCompra? _selectedOrden;
    private Producto? _productoParaAgregar;
    private decimal _cantidadAgregar = 1;
    private decimal _costoAgregar;
    private string _notasOrden = "";

    // ── Recepción ─────────────────────────────────────────────────────────────────
    private OrdenCompra? _ordenParaRecibir;
    private string _numeroFacturaRecepcion = "";

    // ── Estado de Cuenta ─────────────────────────────────────────────────────────
    private FacturaCompra? _selectedFactura;
    private bool _modoFormPago;
    private decimal _montoPago;
    private string _metodoPago = "EFECTIVO";
    private string _referenciaPago = "";

    // ── Collections ───────────────────────────────────────────────────────────────
    public ObservableCollection<Proveedor> Proveedores { get; } = new();
    public ObservableCollection<OrdenCompra> Ordenes { get; } = new();
    public ObservableCollection<OrdenCompra> OrdenesAbiertas { get; } = new();
    public ObservableCollection<FacturaCompra> Facturas { get; } = new();
    public ObservableCollection<DetalleOrdenCompra> DetallesNuevaOrden { get; } = new();
    public ObservableCollection<DetalleOrdenCompra> DetallesOrdenRecibir { get; } = new();
    public ObservableCollection<Producto> ProductosBajoStock { get; } = new();
    public ObservableCollection<Producto> TodosProductos { get; } = new();

    // ── Properties ────────────────────────────────────────────────────────────────
    public Proveedor? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); CargarProveedores(); }
    }

    public bool ModoDetalle
    {
        get => _modoDetalle;
        set => SetProperty(ref _modoDetalle, value);
    }

    public Proveedor? ProveedorEdit
    {
        get => _proveedorEdit;
        set => SetProperty(ref _proveedorEdit, value);
    }

    public bool ModoFormOrden
    {
        get => _modoFormOrden;
        set => SetProperty(ref _modoFormOrden, value);
    }

    public OrdenCompra? SelectedOrden
    {
        get => _selectedOrden;
        set => SetProperty(ref _selectedOrden, value);
    }

    public Producto? ProductoParaAgregar
    {
        get => _productoParaAgregar;
        set
        {
            SetProperty(ref _productoParaAgregar, value);
            if (value != null) { CostoAgregar = value.PrecioCosto; OnPropertyChanged(nameof(CostoAgregar)); }
        }
    }

    public decimal CantidadAgregar
    {
        get => _cantidadAgregar;
        set => SetProperty(ref _cantidadAgregar, value);
    }

    public decimal CostoAgregar
    {
        get => _costoAgregar;
        set => SetProperty(ref _costoAgregar, value);
    }

    public string NotasOrden
    {
        get => _notasOrden;
        set => SetProperty(ref _notasOrden, value);
    }

    public OrdenCompra? OrdenParaRecibir
    {
        get => _ordenParaRecibir;
        set
        {
            SetProperty(ref _ordenParaRecibir, value);
            DetallesOrdenRecibir.Clear();
            if (value != null)
                foreach (var d in value.Detalles) DetallesOrdenRecibir.Add(d);
        }
    }

    public string NumeroFacturaRecepcion
    {
        get => _numeroFacturaRecepcion;
        set => SetProperty(ref _numeroFacturaRecepcion, value);
    }

    public FacturaCompra? SelectedFactura
    {
        get => _selectedFactura;
        set => SetProperty(ref _selectedFactura, value);
    }

    public bool ModoFormPago
    {
        get => _modoFormPago;
        set => SetProperty(ref _modoFormPago, value);
    }

    public decimal MontoPago
    {
        get => _montoPago;
        set => SetProperty(ref _montoPago, value);
    }

    public string MetodoPago
    {
        get => _metodoPago;
        set => SetProperty(ref _metodoPago, value);
    }

    public string ReferenciaPago
    {
        get => _referenciaPago;
        set => SetProperty(ref _referenciaPago, value);
    }

    // ── Commands ──────────────────────────────────────────────────────────────────
    public RelayCommand NuevoCommand { get; }
    public RelayCommand EliminarCommand { get; }
    public RelayCommand GuardarProveedorCommand { get; }
    public RelayCommand VerDetalleCommand { get; }
    public RelayCommand VolverCommand { get; }

    public RelayCommand NuevaOrdenCommand { get; }
    public RelayCommand GuardarOrdenCommand { get; }
    public RelayCommand CancelarOrdenCommand { get; }
    public RelayCommand AgregarProductoCommand { get; }
    public RelayCommand QuitarProductoCommand { get; }
    public RelayCommand AgregarSugeridoCommand { get; }

    public RelayCommand RecibirMercanciaCommand { get; }

    public RelayCommand IniciarPagoCommand { get; }
    public RelayCommand GuardarPagoCommand { get; }
    public RelayCommand CancelarPagoCommand { get; }

    public ProveedoresViewModel(
        ProveedorService proveedorService,
        OrdenCompraService ordenService,
        FacturaCompraService facturaService,
        ProductoService productoService)
    {
        _proveedorService = proveedorService;
        _ordenService = ordenService;
        _facturaService = facturaService;
        _productoService = productoService;

        NuevoCommand = new RelayCommand(NuevoProveedor);
        EliminarCommand = new RelayCommand(Eliminar, () => _selected != null);
        GuardarProveedorCommand = new RelayCommand(GuardarProveedor);
        VerDetalleCommand = new RelayCommand(VerDetalle, () => _selected != null);
        VolverCommand = new RelayCommand(Volver);

        NuevaOrdenCommand = new RelayCommand(NuevaOrden);
        GuardarOrdenCommand = new RelayCommand(GuardarOrden);
        CancelarOrdenCommand = new RelayCommand(() => ModoFormOrden = false);
        AgregarProductoCommand = new RelayCommand(AgregarProducto, () => _productoParaAgregar != null && _cantidadAgregar > 0);
        QuitarProductoCommand = new RelayCommand(p => { if (p is DetalleOrdenCompra d) DetallesNuevaOrden.Remove(d); });
        AgregarSugeridoCommand = new RelayCommand(AgregarSugerido);

        RecibirMercanciaCommand = new RelayCommand(RecibirMercancia,
            () => _ordenParaRecibir != null && !string.IsNullOrWhiteSpace(_numeroFacturaRecepcion));

        IniciarPagoCommand = new RelayCommand(IniciarPago, () => _selectedFactura?.SaldoPendiente > 0);
        GuardarPagoCommand = new RelayCommand(GuardarPago);
        CancelarPagoCommand = new RelayCommand(() => ModoFormPago = false);

        CargarProveedores();
        CargarTodosProductos();
    }

    // ── Private methods ───────────────────────────────────────────────────────────
    private void CargarProveedores()
    {
        Proveedores.Clear();
        var lista = string.IsNullOrWhiteSpace(_searchText)
            ? _proveedorService.GetAll()
            : _proveedorService.Search(_searchText);
        foreach (var p in lista) Proveedores.Add(p);
    }

    private void CargarTodosProductos()
    {
        TodosProductos.Clear();
        foreach (var p in _productoService.GetAll()) TodosProductos.Add(p);
    }

    private void CargarDatosProveedor()
    {
        if (_selected == null) return;

        Ordenes.Clear();
        OrdenesAbiertas.Clear();
        foreach (var o in _ordenService.ObtenerPorProveedor(_selected.Id))
        {
            Ordenes.Add(o);
            if (o.Estado is "BORRADOR" or "ENVIADA" or "RECIBIDA_PARCIAL")
                OrdenesAbiertas.Add(o);
        }

        Facturas.Clear();
        foreach (var f in _facturaService.ObtenerPorProveedor(_selected.Id)) Facturas.Add(f);

        ProductosBajoStock.Clear();
        foreach (var p in _productoService.GetAll().Where(p => p.Stock <= p.StockMinimo))
            ProductosBajoStock.Add(p);

        ProveedorEdit = CloneProveedor(_selected);
        OnPropertyChanged(nameof(ProveedorEdit));
    }

    private static Proveedor CloneProveedor(Proveedor p) => new()
    {
        Id = p.Id, Documento = p.Documento, DocumentoFiscal = p.DocumentoFiscal, Nombre = p.Nombre,
        Telefono = p.Telefono, Email = p.Email, Direccion = p.Direccion,
        ContactoNombre = p.ContactoNombre, ContactoTelefono = p.ContactoTelefono, ContactoEmail = p.ContactoEmail,
        DiasCredito = p.DiasCredito, LimiteCredito = p.LimiteCredito, CondicionesPago = p.CondicionesPago,
        MetodoPagoPreferido = p.MetodoPagoPreferido, Activo = p.Activo
    };

    private void NuevoProveedor()
    {
        Selected = null;
        ProveedorEdit = new Proveedor();
        OnPropertyChanged(nameof(ProveedorEdit));
        Ordenes.Clear(); OrdenesAbiertas.Clear(); Facturas.Clear(); ProductosBajoStock.Clear();
        ModoFormOrden = false; ModoFormPago = false;
        ModoDetalle = true;
    }

    private void VerDetalle()
    {
        if (_selected == null) return;
        CargarDatosProveedor();
        ModoFormOrden = false; ModoFormPago = false;
        ModoDetalle = true;
    }

    private void Volver()
    {
        ModoDetalle = false;
        ModoFormOrden = false;
        ModoFormPago = false;
        CargarProveedores();
    }

    private void GuardarProveedor()
    {
        if (ProveedorEdit == null) return;
        try
        {
            var id = _proveedorService.Guardar(ProveedorEdit);
            Selected = _proveedorService.GetById(id);
            CargarDatosProveedor();
            AppDialog.Show("Proveedor guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Eliminar()
    {
        if (_selected == null) return;
        if (AppDialog.Show($"¿Eliminar al proveedor '{_selected.Nombre}'?", "Confirmar",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        try
        {
            _proveedorService.Eliminar(_selected.Id);
            Volver();
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void NuevaOrden()
    {
        DetallesNuevaOrden.Clear();
        NotasOrden = "";
        ModoFormOrden = true;
    }

    private void GuardarOrden()
    {
        if (_selected == null || !DetallesNuevaOrden.Any())
        {
            AppDialog.Show("Agregue al menos un producto a la orden.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            _ordenService.GuardarOrden(new OrdenCompra
            {
                ProveedorId = _selected.Id,
                Estado = "BORRADOR",
                Notas = _notasOrden,
                Detalles = DetallesNuevaOrden.ToList()
            });
            ModoFormOrden = false;
            CargarDatosProveedor();
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void AgregarProducto()
    {
        if (_productoParaAgregar == null) return;
        var existing = DetallesNuevaOrden.FirstOrDefault(d => d.ProductoId == _productoParaAgregar.Id);
        if (existing != null)
        {
            var idx = DetallesNuevaOrden.IndexOf(existing);
            existing.CantidadSolicitada += _cantidadAgregar;
            DetallesNuevaOrden.RemoveAt(idx);
            DetallesNuevaOrden.Insert(idx, existing);
        }
        else
        {
            DetallesNuevaOrden.Add(new DetalleOrdenCompra
            {
                ProductoId = _productoParaAgregar.Id,
                ProductoNombre = _productoParaAgregar.Nombre,
                ProductoCodigo = _productoParaAgregar.Codigo,
                CantidadSolicitada = _cantidadAgregar,
                CostoUnitario = _costoAgregar > 0 ? _costoAgregar : _productoParaAgregar.PrecioCosto
            });
        }
        ProductoParaAgregar = null;
        CantidadAgregar = 1;
        CostoAgregar = 0;
        OnPropertyChanged(nameof(ProductoParaAgregar));
    }

    private void AgregarSugerido(object? param)
    {
        if (param is not Producto p) return;
        if (!ModoFormOrden) { NuevaOrden(); }
        if (DetallesNuevaOrden.Any(d => d.ProductoId == p.Id)) return;
        decimal sugerido = Math.Max(1, (p.StockMinimo * 2) - p.Stock);
        DetallesNuevaOrden.Add(new DetalleOrdenCompra
        {
            ProductoId = p.Id, ProductoNombre = p.Nombre, ProductoCodigo = p.Codigo,
            CantidadSolicitada = sugerido, CostoUnitario = p.PrecioCosto
        });
    }

    private void RecibirMercancia()
    {
        if (_ordenParaRecibir == null || string.IsNullOrWhiteSpace(_numeroFacturaRecepcion)) return;
        // sync editable CantidadRecibida from DetallesOrdenRecibir back to the orden
        foreach (var d in DetallesOrdenRecibir)
        {
            var orig = _ordenParaRecibir.Detalles.FirstOrDefault(x => x.Id == d.Id);
            if (orig != null) orig.CantidadRecibida = d.CantidadRecibida;
        }
        try
        {
            _facturaService.RecibirMercanciaYGenerarFactura(_ordenParaRecibir, _numeroFacturaRecepcion);
            NumeroFacturaRecepcion = "";
            OrdenParaRecibir = null;
            CargarDatosProveedor();
            AppDialog.Show("Mercancía recibida. Factura generada y stock actualizado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void IniciarPago()
    {
        if (_selectedFactura == null) return;
        MontoPago = _selectedFactura.SaldoPendiente;
        MetodoPago = "EFECTIVO";
        ReferenciaPago = "";
        ModoFormPago = true;
    }

    private void GuardarPago()
    {
        if (_selectedFactura == null) return;
        try
        {
            _facturaService.RegistrarPago(_selectedFactura.Id, _montoPago, _metodoPago, _referenciaPago, "Usuario");
            ModoFormPago = false;
            CargarDatosProveedor();
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
