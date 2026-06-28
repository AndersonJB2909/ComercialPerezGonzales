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
    private readonly CierreCajaService _cierreService;

    private string _searchText = string.Empty;
    private string _clienteSearch = string.Empty;
    private ItemCarrito? _selectedItem;
    private Cliente? _clienteSeleccionado;
    private string _metodoPago = "EFECTIVO";
    private decimal _montoRecibido;
    private decimal _descuentoGlobal;
    private int _paginaActual = 1;
    private int _pageSize = 40;
    private int _totalProductos;
    private int _totalPaginas;

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

    public int PaginaActual
    {
        get => _paginaActual;
        set
        {
            if (SetProperty(ref _paginaActual, value))
            {
                CargarProductos();
                PaginaAnteriorCommand.RaiseCanExecuteChanged();
                PaginaSiguienteCommand.RaiseCanExecuteChanged();
                PaginaPrimeraCommand.RaiseCanExecuteChanged();
                PaginaUltimaCommand.RaiseCanExecuteChanged();
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
        set
        {
            if (SetProperty(ref _totalPaginas, value))
            {
                OnPropertyChanged(nameof(PaginaTexto));
            }
        }
    }

    public string PaginaTexto => $"Página {PaginaActual} de {TotalPaginas}";

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
    public event Action? NavigarFlujoCaja;

    public RelayCommand AgregarProductoCommand { get; }
    public RelayCommand IncrementarItemCommand { get; }
    public RelayCommand QuitarItemCommand { get; }
    public RelayCommand LimpiarCarritoCommand { get; }
    public RelayCommand ProcesarVentaCommand { get; }
    public RelayCommand CotizarCommand { get; }
    public RelayCommand SeleccionarClienteCommand { get; }
    public RelayCommand LimpiarClienteCommand { get; }
    public RelayCommand PaginaAnteriorCommand { get; }
    public RelayCommand PaginaSiguienteCommand { get; }
    public RelayCommand PaginaPrimeraCommand { get; }
    public RelayCommand PaginaUltimaCommand { get; }
    public RelayCommand SeleccionarItemCommand { get; }
    public RelayCommand AplicarDescuentoCommand { get; }
    public RelayCommand IrFlujoCajaCommand { get; }

    public PosViewModel(ProductoService productoService, ClienteService clienteService, VentaService ventaService, ConfiguracionRepository configRepo, CierreCajaService cierreService)
    {
        _productoService = productoService;
        _clienteService = clienteService;
        _ventaService = ventaService;
        _configRepo = configRepo;
        _cierreService = cierreService;

        AgregarProductoCommand = new RelayCommand(param => AgregarProducto(param as Producto));
        IncrementarItemCommand = new RelayCommand(param => IncrementarItem(param as ItemCarrito));
        QuitarItemCommand = new RelayCommand(param => QuitarItem(param as ItemCarrito));
        LimpiarCarritoCommand = new RelayCommand(LimpiarCarrito);
        ProcesarVentaCommand = new RelayCommand(ProcesarVenta, () => CartItems.Any());
        CotizarCommand = new RelayCommand(ProcesarCotizacion, () => CartItems.Any());
        SeleccionarClienteCommand = new RelayCommand(param => SeleccionarCliente(param as Cliente));
        LimpiarClienteCommand = new RelayCommand(() => { ClienteSeleccionado = null; ClienteSearch = string.Empty; });
        SeleccionarItemCommand = new RelayCommand(param => SeleccionarItem(param as ItemCarrito));
        AplicarDescuentoCommand = new RelayCommand(ExecuteAplicarDescuento);
        IrFlujoCajaCommand = new RelayCommand(() => NavigarFlujoCaja?.Invoke());

        PaginaAnteriorCommand = new RelayCommand(() => PaginaActual--, () => PaginaActual > 1);
        PaginaSiguienteCommand = new RelayCommand(() => PaginaActual++, () => PaginaActual < TotalPaginas);
        PaginaPrimeraCommand = new RelayCommand(() => PaginaActual = 1, () => PaginaActual > 1);
        PaginaUltimaCommand = new RelayCommand(() => PaginaActual = TotalPaginas, () => PaginaActual < TotalPaginas);

        CargarProductos();
    }

    private void CargarProductos()
    {
        ProductosFiltrados.Clear();
        var lista = _productoService.GetPaged(
            PaginaActual,
            _pageSize,
            _searchText,
            new List<int>(),
            new List<string>(),
            out int totalCount
        );
        
        TotalProductos = totalCount;
        TotalPaginas = (int)System.Math.Ceiling((double)totalCount / _pageSize);
        if (TotalPaginas == 0) TotalPaginas = 1;
        
        OnPropertyChanged(nameof(PaginaTexto));
        
        foreach (var p in lista)
        {
            ProductosFiltrados.Add(p);
        }
        
        PaginaAnteriorCommand.RaiseCanExecuteChanged();
        PaginaSiguienteCommand.RaiseCanExecuteChanged();
        PaginaPrimeraCommand.RaiseCanExecuteChanged();
        PaginaUltimaCommand.RaiseCanExecuteChanged();
    }

    private void BuscarProductos()
    {
        if (PaginaActual != 1)
        {
            PaginaActual = 1;
        }
        else
        {
            CargarProductos();
        }
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

    public void CargarCotizacion(Venta cotizacion)
    {
        LimpiarCarrito();
        foreach (var det in cotizacion.Detalles)
        {
            var producto = _productoService.GetById(det.ProductoId);
            if (producto != null)
            {
                CartItems.Add(new ItemCarrito
                {
                    ProductoId = producto.Id,
                    Nombre = producto.Nombre,
                    Codigo = producto.Codigo,
                    Cantidad = det.Cantidad,
                    PrecioUnit = producto.PrecioVenta, // O usar det.PrecioUnit si se quiere mantener el precio cotizado
                    UnidadMedida = producto.UnidadMedida
                });
            }
        }
        
        if (cotizacion.ClienteId.HasValue)
        {
            var cliente = _clienteService.GetAll().FirstOrDefault(c => c.Id == cotizacion.ClienteId.Value);
            SeleccionarCliente(cliente);
        }
        
        RecalcularTotales();
    }

    private void AgregarProducto(Producto? producto)
    {
        if (producto == null) return;

        var jornada = _cierreService.GetJornadaHoy();
        if (jornada == null || jornada.EstaCerrado)
        {
            AppDialog.Show("Debe realizar la apertura de la jornada en la pantalla 'Flujo de Caja' antes de registrar artículos.",
                "Apertura Obligatoria de Caja",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

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
            if (item == SelectedItem)
            {
                SelectedItem = null;
            }
            CartItems.Remove(item);
        }
        RecalcularTotales();
    }

    private void LimpiarCarrito()
    {
        CartItems.Clear();
        SelectedItem = null;
        DescuentoGlobal = 0;
        MontoRecibido = 0;
        RecalcularTotales();
    }

    private void SeleccionarItem(ItemCarrito? item)
    {
        if (item == null) return;

        if (SelectedItem != null && SelectedItem != item)
        {
            SelectedItem.IsSelected = false;
        }

        if (SelectedItem == item)
        {
            SelectedItem.IsSelected = false;
            SelectedItem = null;
        }
        else
        {
            SelectedItem = item;
            SelectedItem.IsSelected = true;
        }
    }

    private void ExecuteAplicarDescuento()
    {
        if (!CartItems.Any())
        {
            AppDialog.Show("El carrito está vacío.", "Descuento", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new ComercialPerezGonzales.Views.POS.DescuentoWindow(SelectedItem != null, SelectedItem?.Nombre ?? string.Empty)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            bool aplicarAlItem = dialog.AplicarAlItem;
            bool esPorcentaje = dialog.EsPorcentaje;
            decimal valor = dialog.Valor;

            if (aplicarAlItem && SelectedItem != null)
            {
                if (esPorcentaje)
                {
                    decimal montoDescuento = Math.Round((SelectedItem.Cantidad * SelectedItem.PrecioUnit) * (valor / 100), 2);
                    SelectedItem.Descuento = montoDescuento;
                }
                else
                {
                    SelectedItem.Descuento = valor;
                }
                SelectedItem.OnCantidadChanged();
            }
            else
            {
                if (esPorcentaje)
                {
                    decimal montoDescuento = Math.Round(Subtotal * (valor / 100), 2);
                    DescuentoGlobal = montoDescuento;
                }
                else
                {
                    DescuentoGlobal = valor;
                }
            }
            RecalcularTotales();
        }
    }

    private void ProcesarVenta()
    {
        var jornada = _cierreService.GetJornadaHoy();
        if (jornada == null || jornada.EstaCerrado)
        {
            AppDialog.Show("Debe realizar la apertura de la jornada en la pantalla 'Flujo de Caja' antes de procesar una venta.",
                "Apertura Obligatoria de Caja",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var pagoVm = new PagoViewModel
        {
            TotalVenta = Total,
            Clientes = new ObservableCollection<Cliente>(_clienteService.GetAll()),
            ClienteSeleccionado = this.ClienteSeleccionado
        };
        
        pagoVm.CrearNuevoClienteSolicitado += (nuevoCliente) =>
        {
            try
            {
                _clienteService.Guardar(nuevoCliente);
                pagoVm.Clientes.Add(nuevoCliente);
                pagoVm.ClienteSeleccionado = nuevoCliente;
            }
            catch (Exception ex)
            {
                AppDialog.Show(ex.Message, "Error al crear cliente", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        
        foreach (var item in CartItems)
            pagoVm.CartItems.Add(item);

        SolicitarPago?.Invoke(pagoVm);

        if (!pagoVm.Confirmado) return;

        this.ClienteSeleccionado = pagoVm.ClienteSeleccionado;

        try
        {
            var venta = _ventaService.ProcesarVenta(
                CartItems.ToList(),
                ClienteSeleccionado?.Id,
                pagoVm.MetodoPago,
                pagoVm.MontoPagado,
                DescuentoGlobal,
                pagoVm.NotaCreditoCodigo);

            var reciboVm = new ReciboViewModel
            {
                NumeroVenta   = venta.Numero,
                FechaVenta    = DateTime.Now,
                TotalVenta    = venta.Total,
                Subtotal      = venta.Subtotal + CartItems.Sum(i => i.Descuento),
                Descuento     = venta.Descuento + CartItems.Sum(i => i.Descuento),
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
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ProcesarCotizacion()
    {
        var pagoVm = new PagoViewModel
        {
            TotalVenta = Total,
            Clientes = new ObservableCollection<Cliente>(_clienteService.GetAll()),
            ClienteSeleccionado = this.ClienteSeleccionado,
            EsCotizacion = true,
            MetodoPago = "COTIZACION"
        };
        
        pagoVm.CrearNuevoClienteSolicitado += (nuevoCliente) =>
        {
            try
            {
                _clienteService.Guardar(nuevoCliente);
                pagoVm.Clientes.Add(nuevoCliente);
                pagoVm.ClienteSeleccionado = nuevoCliente;
            }
            catch (Exception ex)
            {
                AppDialog.Show(ex.Message, "Error al crear cliente", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        foreach (var item in CartItems)
            pagoVm.CartItems.Add(item);

        SolicitarPago?.Invoke(pagoVm);

        if (!pagoVm.Confirmado) return;

        this.ClienteSeleccionado = pagoVm.ClienteSeleccionado;

        try
        {
            var cotizacion = _ventaService.ProcesarCotizacion(
                CartItems.ToList(),
                ClienteSeleccionado?.Id,
                DescuentoGlobal);

            var reciboVm = new ReciboViewModel
            {
                NumeroVenta   = cotizacion.Numero,
                FechaVenta    = DateTime.Now,
                TotalVenta    = cotizacion.Total,
                Subtotal      = cotizacion.Subtotal + CartItems.Sum(i => i.Descuento),
                Descuento     = cotizacion.Descuento + CartItems.Sum(i => i.Descuento),
                MontoPagado   = 0,
                MetodoPago    = "COTIZACION",
                NombreCliente = ClienteSeleccionado?.NombreCompleto ?? "Cliente General",
                OrdenId       = cotizacion.Id,
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
            AppDialog.Show(ex.Message, "Error al generar cotización", MessageBoxButton.OK, MessageBoxImage.Error);
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
