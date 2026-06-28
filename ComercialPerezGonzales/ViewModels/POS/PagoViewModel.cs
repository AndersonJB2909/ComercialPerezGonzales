using System.Collections.ObjectModel;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.POS;

public class PagoViewModel : ViewModelBase
{
    private decimal _totalVenta;
    private decimal _montoPagado;
    private string _metodoPago = "EFECTIVO";
    private string _displayNumpad = "0";
    private Cliente? _clienteSeleccionado;

    private bool _mostrarPanelNuevoCliente;
    private string _nuevoClienteNombre = string.Empty;
    private string _nuevoClienteApellido = string.Empty;
    private string _nuevoClienteTelefono = string.Empty;
    private string _nuevoClienteDocumento = string.Empty;
    private string _nuevoClienteEmail = string.Empty;
    private string _nuevoClienteCodigo = string.Empty;
    private string _nuevoClienteDireccion = string.Empty;

    public bool EsCotizacion { get; set; }
    public bool EsModoVenta => !EsCotizacion;
    public string TituloVentana => EsCotizacion ? "Opciones de Cotización" : "Procesar Pago";
    public string TextoBotonConfirmar => EsCotizacion ? "Guardar Cotización" : "Confirmar Pago";

    public bool MostrarPanelNuevoCliente
    {
        get => _mostrarPanelNuevoCliente;
        set => SetProperty(ref _mostrarPanelNuevoCliente, value);
    }
    public string NuevoClienteNombre
    {
        get => _nuevoClienteNombre;
        set { SetProperty(ref _nuevoClienteNombre, value); GuardarNuevoClienteCommand?.RaiseCanExecuteChanged(); }
    }
    public string NuevoClienteApellido
    {
        get => _nuevoClienteApellido;
        set => SetProperty(ref _nuevoClienteApellido, value);
    }
    public string NuevoClienteTelefono
    {
        get => _nuevoClienteTelefono;
        set => SetProperty(ref _nuevoClienteTelefono, value);
    }
    public string NuevoClienteDocumento
    {
        get => _nuevoClienteDocumento;
        set => SetProperty(ref _nuevoClienteDocumento, value);
    }
    public string NuevoClienteEmail
    {
        get => _nuevoClienteEmail;
        set => SetProperty(ref _nuevoClienteEmail, value);
    }
    public string NuevoClienteCodigo
    {
        get => _nuevoClienteCodigo;
        set => SetProperty(ref _nuevoClienteCodigo, value);
    }
    public string NuevoClienteDireccion
    {
        get => _nuevoClienteDireccion;
        set => SetProperty(ref _nuevoClienteDireccion, value);
    }

    public ObservableCollection<Cliente> Clientes { get; set; } = new();

    public Cliente? ClienteSeleccionado
    {
        get => _clienteSeleccionado;
        set => SetProperty(ref _clienteSeleccionado, value);
    }

    public decimal TotalVenta
    {
        get => _totalVenta;
        set { SetProperty(ref _totalVenta, value); OnPropertyChanged(nameof(Cambio)); OnPropertyChanged(nameof(PuedePagar)); }
    }

    public decimal MontoPagado
    {
        get => _montoPagado;
        set
        {
            SetProperty(ref _montoPagado, value);
            OnPropertyChanged(nameof(Cambio));
            OnPropertyChanged(nameof(PuedePagar));
            ConfirmarCommand?.RaiseCanExecuteChanged();
        }
    }

    private string _notaCreditoCodigo = string.Empty;
    private string _notaCreditoInfo = string.Empty;
    private string _notaCreditoError = string.Empty;
    private bool _notaCreditoValida = false;

    public bool EsEfectivo => _metodoPago == "EFECTIVO";
    public bool EsNotaCredito => _metodoPago == "NOTA_CREDITO";

    public string MetodoPago
    {
        get => _metodoPago;
        set
        {
            SetProperty(ref _metodoPago, value);
            OnPropertyChanged(nameof(EsEfectivo));
            OnPropertyChanged(nameof(EsNotaCredito));
            if (value != "EFECTIVO" && value != "NOTA_CREDITO")
            {
                MontoPagado = TotalVenta;
                DisplayNumpad = TotalVenta.ToString("N2");
            }
            else if (value == "NOTA_CREDITO")
            {
                MontoPagado = 0;
                DisplayNumpad = "0";
                NotaCreditoCodigo = string.Empty;
            }
            else
            {
                MontoPagado = 0;
                DisplayNumpad = "0";
            }
            OnPropertyChanged(nameof(PuedePagar));
            ConfirmarCommand?.RaiseCanExecuteChanged();
        }
    }

    public string NotaCreditoCodigo
    {
        get => _notaCreditoCodigo;
        set
        {
            SetProperty(ref _notaCreditoCodigo, value);
            NotaCreditoInfo = string.Empty;
            NotaCreditoError = string.Empty;
            _notaCreditoValida = false;
            OnPropertyChanged(nameof(PuedePagar));
            ConfirmarCommand?.RaiseCanExecuteChanged();
        }
    }

    public string NotaCreditoInfo
    {
        get => _notaCreditoInfo;
        set => SetProperty(ref _notaCreditoInfo, value);
    }

    public string NotaCreditoError
    {
        get => _notaCreditoError;
        set => SetProperty(ref _notaCreditoError, value);
    }

    public string DisplayNumpad
    {
        get => _displayNumpad;
        set => SetProperty(ref _displayNumpad, value);
    }

    public decimal Cambio => MontoPagado - TotalVenta;
    public bool PuedePagar => EsCotizacion || (EsNotaCredito ? _notaCreditoValida : MontoPagado >= TotalVenta);

    public ObservableCollection<ItemCarrito> CartItems { get; } = new();

    public bool Confirmado { get; private set; }
    public event Action? CerrarSolicitado;
    public event Action<Cliente>? CrearNuevoClienteSolicitado;

    public RelayCommand NumpadCommand { get; }
    public RelayCommand BorrarNumpadCommand { get; }
    public RelayCommand LimpiarNumpadCommand { get; }
    public RelayCommand SetMetodoPagoCommand { get; }
    public RelayCommand PagoExactoCommand { get; }
    public RelayCommand ConfirmarCommand { get; }
    public RelayCommand CancelarCommand { get; }
    public RelayCommand ValidarNotaCreditoCommand { get; }
    
    public RelayCommand MostrarNuevoClienteCommand { get; }
    public RelayCommand CancelarNuevoClienteCommand { get; }
    public RelayCommand GuardarNuevoClienteCommand { get; }

    public PagoViewModel()
    {
        NumpadCommand = new RelayCommand(param => AppendNumpad(param?.ToString() ?? ""));
        BorrarNumpadCommand = new RelayCommand(BorrarNumpad);
        LimpiarNumpadCommand = new RelayCommand(LimpiarNumpad);
        SetMetodoPagoCommand = new RelayCommand(param => MetodoPago = param?.ToString() ?? "EFECTIVO");
        PagoExactoCommand = new RelayCommand(() => {
            MontoPagado = TotalVenta;
            DisplayNumpad = TotalVenta.ToString("N2");
        });
        ConfirmarCommand = new RelayCommand(Confirmar, () => PuedePagar);
        CancelarCommand = new RelayCommand(() => CerrarSolicitado?.Invoke());
        ValidarNotaCreditoCommand = new RelayCommand(ValidarNotaCredito);
        
        MostrarNuevoClienteCommand = new RelayCommand(() => 
        {
            NuevoClienteNombre = "";
            NuevoClienteApellido = "";
            NuevoClienteTelefono = "";
            NuevoClienteDocumento = "";
            NuevoClienteEmail = "";
            NuevoClienteCodigo = "";
            NuevoClienteDireccion = "";
            MostrarPanelNuevoCliente = true;
        });

        CancelarNuevoClienteCommand = new RelayCommand(() => MostrarPanelNuevoCliente = false);

        GuardarNuevoClienteCommand = new RelayCommand(() => 
        {
            var nuevo = new Cliente 
            {
                Nombre = NuevoClienteNombre,
                Apellido = NuevoClienteApellido,
                Telefono = NuevoClienteTelefono,
                Documento = NuevoClienteDocumento,
                Email = NuevoClienteEmail,
                Codigo = NuevoClienteCodigo,
                Direccion = NuevoClienteDireccion
            };
            CrearNuevoClienteSolicitado?.Invoke(nuevo);
            MostrarPanelNuevoCliente = false;
        }, () => !string.IsNullOrWhiteSpace(NuevoClienteNombre));
    }

    private void AppendNumpad(string digito)
    {
        if (DisplayNumpad == "0") DisplayNumpad = digito;
        else DisplayNumpad += digito;

        if (decimal.TryParse(DisplayNumpad.Replace(",", ""), out var valor))
            MontoPagado = valor;
    }

    private void BorrarNumpad()
    {
        if (DisplayNumpad.Length > 1)
            DisplayNumpad = DisplayNumpad[..^1];
        else
            DisplayNumpad = "0";

        if (decimal.TryParse(DisplayNumpad.Replace(",", ""), out var valor))
            MontoPagado = valor;
        else
            MontoPagado = 0;
    }

    private void LimpiarNumpad()
    {
        DisplayNumpad = "0";
        MontoPagado = 0;
    }

    private void ValidarNotaCredito()
    {
        NotaCreditoInfo = string.Empty;
        NotaCreditoError = string.Empty;
        _notaCreditoValida = false;

        if (string.IsNullOrWhiteSpace(NotaCreditoCodigo))
        {
            NotaCreditoError = "Ingrese el código de la nota de crédito.";
            OnPropertyChanged(nameof(PuedePagar));
            ConfirmarCommand?.RaiseCanExecuteChanged();
            return;
        }

        try
        {
            var service = (ComercialPerezGonzales.Services.DevolucionService)App.Services.GetService(typeof(ComercialPerezGonzales.Services.DevolucionService))!;
            var nc = service.ValidarNotaCredito(NotaCreditoCodigo.Trim().ToUpper(), TotalVenta);
            if (nc != null)
            {
                if (nc.MontoDisponible < TotalVenta)
                {
                    NotaCreditoError = $"El saldo de la nota de crédito ({nc.MontoDisponible:C2}) es menor que el total de la venta ({TotalVenta:C2}).";
                }
                else
                {
                    _notaCreditoValida = true;
                    MontoPagado = TotalVenta;
                    NotaCreditoInfo = $"✓ Nota de Crédito Válida. Saldo disponible: {nc.MontoDisponible:C2}. Se cobrará: {TotalVenta:C2}. Nuevo saldo: {(nc.MontoDisponible - TotalVenta):C2}.";
                }
            }
        }
        catch (Exception ex)
        {
            NotaCreditoError = ex.Message;
        }

        OnPropertyChanged(nameof(PuedePagar));
        ConfirmarCommand?.RaiseCanExecuteChanged();
    }

    private void Confirmar()
    {
        Confirmado = true;
        CerrarSolicitado?.Invoke();
    }
}
