using System.Collections.ObjectModel;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.POS;

public class PagoViewModel : ViewModelBase
{
    private decimal _totalVenta;
    private decimal _montoPagado;
    private string _metodoPago = "EFECTIVO";
    private string _displayNumpad = "0";

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

    public bool EsEfectivo => _metodoPago == "EFECTIVO";

    public string MetodoPago
    {
        get => _metodoPago;
        set
        {
            SetProperty(ref _metodoPago, value);
            OnPropertyChanged(nameof(EsEfectivo));
            if (value != "EFECTIVO")
            {
                MontoPagado = TotalVenta;
                DisplayNumpad = TotalVenta.ToString("N2");
            }
            else
            {
                MontoPagado = 0;
                DisplayNumpad = "0";
            }
        }
    }

    public string DisplayNumpad
    {
        get => _displayNumpad;
        set => SetProperty(ref _displayNumpad, value);
    }

    public decimal Cambio => MontoPagado - TotalVenta;
    public bool PuedePagar => MontoPagado >= TotalVenta;

    public ObservableCollection<ItemCarrito> CartItems { get; } = new();

    public bool Confirmado { get; private set; }
    public event Action? CerrarSolicitado;

    public RelayCommand NumpadCommand { get; }
    public RelayCommand BorrarNumpadCommand { get; }
    public RelayCommand LimpiarNumpadCommand { get; }
    public RelayCommand SetMetodoPagoCommand { get; }
    public RelayCommand PagoExactoCommand { get; }
    public RelayCommand ConfirmarCommand { get; }
    public RelayCommand CancelarCommand { get; }

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

    private void Confirmar()
    {
        Confirmado = true;
        CerrarSolicitado?.Invoke();
    }
}
