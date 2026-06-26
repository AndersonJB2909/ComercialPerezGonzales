using System.Collections.ObjectModel;
using System.Windows;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.POS;

public class PosViewModel : ViewModelBase
{
    private readonly ProductoService _productoService;
    private readonly ClienteService _clienteService;
    private readonly VentaService _ventaService;
    private readonly ConfiguracionRepository _configRepo;

    private string _searchText = string.Empty;
    private string _clienteSearch = string.Empty;
    private ItemCarrito? _selectedItem;
    private Cliente? _clienteSeleccionado;
    private string _metodoPago = "EFECTIVO";
    private decimal _montoRecibido;
    private decimal _descuentoGlobal;

    public ObservableCollection<ItemCarrito> CartItems { get; } = new();
    public ObservableCollection<Producto> ProductosFiltrados { get; } = new();
    public ObservableCollection<Cliente> ClientesFiltrados { get; } = new();

    public bool MostrarClientesSugeridos => ClientesFiltrados.Count > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetProperty(ref _searchText, value);
            BuscarProductos();
        }
    }

    public string ClienteSearch
    {
        get => _clienteSearch;
        set
        {
            SetProperty(ref _clienteSearch, value);
            BuscarClientes();
        }
    }

    public ItemCarrito? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public Cliente? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        set
        {
            SetProperty(ref _clienteSeleccionado, value);
            OnPropertyChanged(nameof(ClienteNombreDisplay));
        }
    }

    public string ClienteNombreDisplay => _clienteSeleccionado?.NombreCompleto ?? "Cliente General";

    public string MetodoPago
    {
        get => _metodoPago;
        set => SetProperty(ref _metodoPago, value);
    }

    public decimal MontoRecibido
    {
        get => _montoRecibido;
        set
        {
            SetProperty(ref _montoRecibido, value);
            OnPropertyChanged(nameof(Cambio));
        }
    }

    public decimal DescuentoGlobal
    {
        get => _descuentoGlobal;
        set
        {
            SetProperty(ref _descuentoGlobal, value);
            RecalcularTotales();
        }
    }

    public decimal Subtotal => CartItems.Sum(i => i.Subtotal);
    public decimal Total => Math.Max(0, Subtotal - _descuentoGlobal);
    public decimal Cambio => Math.Max(0, MontoRecibido - Total);
    public int CantidadItems => CartItems.Sum(i => (int)i.Cantidad);

    public event Action<PagoViewModel>? SolicitarPago;
    public event Action<ReciboViewModel>? SolicitarRecibo;

    public RelayCommand AgregarProductoCommand { get; }
    public RelayCommand IncrementarItemCommand { get; }
    public RelayCommand QuitarItemCommand { get; }
    public RelayCommand LimpiarCarritoCommand { get; }
    public RelayCommand ProcesarVentaCommand { get; }
    public RelayCommand SeleccionarClienteCommand { get; }
    public RelayCommand LimpiarClienteCommand { get; }

    public PosViewModel(ProductoService productoService, ClienteService clienteService, VentaService ventaService, ConfiguracionRepository configRepo)
    {
        _productoService = productoService;
        _clienteService = clienteService;
        _ventaService = ventaService;
        _configRepo = configRepo;

        AgregarProductoCommand = new RelayCommand(param => AgregarProducto(param as Producto));
        IncrementarItemCommand = new RelayCommand(param => IncrementarItem(param as ItemCarrito));
        QuitarItemCommand = new RelayCommand(param => QuitarItem(param as ItemCarrito));
        LimpiarCarritoCommand = new RelayCommand(LimpiarCarrito);
        ProcesarVentaCommand = new RelayCommand(ProcesarVenta, () => CartItems.Any());
        SeleccionarClienteCommand = new RelayCommand(param => SeleccionarCliente(param as Cliente));
        LimpiarClienteCommand = new RelayCommand(() => { ClienteSeleccionado = null; ClienteSearch = string.Empty; });

        CargarProductos();
    }

    private void CargarProductos()
    {
        ProductosFiltrados.Clear();
        foreach (var p in _productoService.GetAll().Take(50))
            ProductosFiltrados.Add(p);
    }

    private void BuscarProductos()
    {
        ProductosFiltrados.Clear();
        var productos = string.IsNullOrWhiteSpace(_searchText)
            ? _productoService.GetAll().Take(50)
            : _productoService.Search(_searchText);

        foreach (var p in productos)
            ProductosFiltrados.Add(p);
    }

    private void BuscarClientes()
    {
        ClientesFiltrados.Clear();
        if (!string.IsNullOrWhiteSpace(_clienteSearch))
            foreach (var c in _clienteService.Search(_clienteSearch))
                ClientesFiltrados.Add(c);
        OnPropertyChanged(nameof(MostrarClientesSugeridos));
    }

    public void AgregarProductoPorCodigo(string codigo)
    {
        var producto = _productoService.GetByCodigo(codigo);
        if (producto != null)
        {
            AgregarProducto(producto);
            SearchText = string.Empty;
        }
    }

    private void AgregarProducto(Producto? producto)
    {
        if (producto == null) return;

        var existente = CartItems.FirstOrDefault(i => i.ProductoId == producto.Id);
        if (existente != null)
        {
            existente.Cantidad++;
            existente.OnCantidadChanged();
        }
        else
        {
            CartItems.Add(new ItemCarrito
            {
                ProductoId = producto.Id,
                Nombre = producto.Nombre,
                Codigo = producto.Codigo,
                Cantidad = 1,
                PrecioUnit = producto.PrecioVenta,
                UnidadMedida = producto.UnidadMedida
            });
        }
        RecalcularTotales();
    }

    private void IncrementarItem(ItemCarrito? item)
    {
        if (item == null) return;
        item.Cantidad++;
        item.OnCantidadChanged();
        RecalcularTotales();
    }

    private void QuitarItem(ItemCarrito? item)
    {
        if (item == null) return;
        if (item.Cantidad > 1)
        {
            item.Cantidad--;
            item.OnCantidadChanged();
        }
        else
        {
            CartItems.Remove(item);
        }
        RecalcularTotales();
    }

    private void LimpiarCarrito()
    {
        CartItems.Clear();
        DescuentoGlobal = 0;
        MontoRecibido = 0;
        RecalcularTotales();
    }

    private void ProcesarVenta()
    {
        var pagoVm = new PagoViewModel
        {
            TotalVenta = Total
        };
        foreach (var item in CartItems)
            pagoVm.CartItems.Add(item);

        SolicitarPago?.Invoke(pagoVm);

        if (!pagoVm.Confirmado) return;

        try
        {
            var venta = _ventaService.ProcesarVenta(
                CartItems.ToList(),
                ClienteSeleccionado?.Id,
                pagoVm.MetodoPago,
                pagoVm.MontoPagado,
                DescuentoGlobal);

            var reciboVm = new ReciboViewModel
            {
                NumeroVenta   = venta.Numero,
                FechaVenta    = DateTime.Now,
                TotalVenta    = venta.Total,
                MontoPagado   = pagoVm.MontoPagado,
                MetodoPago    = pagoVm.MetodoPago,
                NombreCliente = ClienteSeleccionado?.NombreCompleto ?? "Cliente General",
                OrdenId       = venta.Id,
                NombreNegocio = _configRepo.GetValor("negocio_nombre")    ?? string.Empty,
                Direccion     = _configRepo.GetValor("negocio_direccion") ?? string.Empty,
                Telefono      = _configRepo.GetValor("negocio_telefono")  ?? string.Empty,
                Rnc           = _configRepo.GetValor("negocio_rut")       ?? string.Empty,
                NombreUsuario = _configRepo.GetValor("usuario_nombre")    ?? string.Empty,
                Encabezado    = _configRepo.GetValor("imp_encabezado")     ?? string.Empty,
                PiePagina     = _configRepo.GetValor("imp_pie")           ?? string.Empty,
                MonedaSimbolo = _configRepo.GetValor("moneda_simbolo")    ?? "$",
                ImpNombreImpresora = _configRepo.GetValor("imp_impresora")           ?? string.Empty,
                ImpTipoPapel       = _configRepo.GetValor("imp_papel")               ?? "80mm",
                ImpCopias          = int.TryParse(_configRepo.GetValor("imp_copias"), out var cp) ? cp : 1,
                ImpMargenArriba    = int.TryParse(_configRepo.GetValor("imp_margen_arriba"),    out var ma) ? ma : 0,
                ImpMargenAbajo     = int.TryParse(_configRepo.GetValor("imp_margen_abajo"),     out var mb) ? mb : 0,
                ImpMargenIzquierda = int.TryParse(_configRepo.GetValor("imp_margen_izquierda"), out var mi) ? mi : 0,
                ImpMargenDerecha   = int.TryParse(_configRepo.GetValor("imp_margen_derecha"),   out var mr) ? mr : 0,
                ImpFuenteFamilia   = _configRepo.GetValor("imp_fuente_familia")      ?? string.Empty,
                ImpFuenteTamano    = int.TryParse(_configRepo.GetValor("imp_fuente_tamano"), out var ft) ? ft : 100,
            };
            foreach (var item in CartItems)
                reciboVm.CartItems.Add(item);

            SolicitarRecibo?.Invoke(reciboVm);

            LimpiarCarrito();
            ClienteSeleccionado = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void SeleccionarCliente(Cliente? cliente)
    {
        if (cliente == null) return;
        ClienteSeleccionado = cliente;
        ClientesFiltrados.Clear();
        _clienteSearch = string.Empty;
        OnPropertyChanged(nameof(ClienteSearch));
        OnPropertyChanged(nameof(MostrarClientesSugeridos));
    }

    private void RecalcularTotales()
    {
        OnPropertyChanged(nameof(Subtotal));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(Cambio));
        OnPropertyChanged(nameof(CantidadItems));
    }
}
