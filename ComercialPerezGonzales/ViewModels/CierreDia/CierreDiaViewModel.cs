using System.Collections.ObjectModel;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.CierreDia;

public class CierreDiaViewModel : ViewModelBase
{
    private readonly CierreCajaService _service;

    // ── Estado ───────────────────────────────────────────────────────────
    private EstadoVistaCierre _estado = EstadoVistaCierre.Cargando;
    private CierreCaja? _jornada;
    private decimal _fondoInicial;
    private decimal _efectivoFisico;
    private ResumenCierre? _resumen;
    private string _observaciones = string.Empty;
    private string _errorMensaje = string.Empty;

    // ── Formulario de movimiento ─────────────────────────────────────────
    private string _movTipo = "SALIDA";
    private string _movConcepto = string.Empty;
    private string _movMontoTexto = string.Empty;
    private string? _movReferencia;

    // ── Colecciones ──────────────────────────────────────────────────────
    public ObservableCollection<MovimientoCaja> Movimientos { get; } = [];
    public ObservableCollection<AlertaStockItem> AlertasStock { get; } = [];
    public ObservableCollection<TopProductoItem> TopProductos { get; } = [];

    // ── Propiedades ──────────────────────────────────────────────────────
    public EstadoVistaCierre Estado
    {
        get => _estado;
        private set
        {
            SetProperty(ref _estado, value);
            OnPropertyChanged(nameof(MostrarSinJornada));
            OnPropertyChanged(nameof(MostrarAbierta));
            OnPropertyChanged(nameof(MostrarConteo));
            OnPropertyChanged(nameof(MostrarResumen));
            OnPropertyChanged(nameof(MostrarCerrada));
        }
    }

    public bool MostrarSinJornada => Estado == EstadoVistaCierre.SinJornada;
    public bool MostrarAbierta    => Estado == EstadoVistaCierre.Abierta;
    public bool MostrarConteo     => Estado == EstadoVistaCierre.Conteo;
    public bool MostrarResumen    => Estado == EstadoVistaCierre.Resumen;
    public bool MostrarCerrada    => Estado == EstadoVistaCierre.Cerrada;

    public CierreCaja? Jornada
    {
        get => _jornada;
        private set => SetProperty(ref _jornada, value);
    }

    public decimal FondoInicial
    {
        get => _fondoInicial;
        set => SetProperty(ref _fondoInicial, value);
    }

    public decimal EfectivoFisico
    {
        get => _efectivoFisico;
        set => SetProperty(ref _efectivoFisico, value);
    }

    public ResumenCierre? Resumen
    {
        get => _resumen;
        private set => SetProperty(ref _resumen, value);
    }

    public string Observaciones
    {
        get => _observaciones;
        set => SetProperty(ref _observaciones, value);
    }

    public string ErrorMensaje
    {
        get => _errorMensaje;
        set
        {
            SetProperty(ref _errorMensaje, value);
            OnPropertyChanged(nameof(HayError));
        }
    }

    public bool HayError => !string.IsNullOrEmpty(_errorMensaje);

    public string MovTipo
    {
        get => _movTipo;
        set => SetProperty(ref _movTipo, value);
    }

    public string MovConcepto
    {
        get => _movConcepto;
        set => SetProperty(ref _movConcepto, value);
    }

    public string MovMontoTexto
    {
        get => _movMontoTexto;
        set => SetProperty(ref _movMontoTexto, value);
    }

    public string? MovReferencia
    {
        get => _movReferencia;
        set => SetProperty(ref _movReferencia, value);
    }

    // ── Comandos ─────────────────────────────────────────────────────────
    public RelayCommand CargarCommand { get; }
    public RelayCommand CrearJornadaCommand { get; }
    public RelayCommand AgregarMovimientoCommand { get; }
    public RelayCommand IniciarCierreCommand { get; }
    public RelayCommand CalcularResumenCommand { get; }
    public RelayCommand ConfirmarCierreCommand { get; }
    public RelayCommand VolverCommand { get; }

    public CierreDiaViewModel(CierreCajaService service)
    {
        _service = service;

        CargarCommand          = new RelayCommand(Cargar);
        CrearJornadaCommand    = new RelayCommand(CrearJornada);
        AgregarMovimientoCommand = new RelayCommand(AgregarMovimiento);
        IniciarCierreCommand   = new RelayCommand(() => Estado = EstadoVistaCierre.Conteo);
        CalcularResumenCommand = new RelayCommand(CalcularResumen);
        ConfirmarCierreCommand = new RelayCommand(ConfirmarCierre);
        VolverCommand          = new RelayCommand(() =>
        {
            ErrorMensaje = string.Empty;
            Estado = EstadoVistaCierre.Abierta;
        });

        Cargar();
    }

    private void Cargar()
    {
        ErrorMensaje = string.Empty;
        try
        {
            Jornada = _service.GetJornadaHoy();

            if (Jornada == null)
            {
                Estado = EstadoVistaCierre.SinJornada;
                return;
            }

            RefrescarMovimientos();

            Estado = Jornada.EstaCerrado
                ? EstadoVistaCierre.Cerrada
                : EstadoVistaCierre.Abierta;
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void CrearJornada()
    {
        ErrorMensaje = string.Empty;
        try
        {
            Jornada = _service.AbrirJornada(FondoInicial);
            RefrescarMovimientos();
            Estado = EstadoVistaCierre.Abierta;
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void AgregarMovimiento()
    {
        ErrorMensaje = string.Empty;
        if (Jornada == null) return;

        if (!decimal.TryParse(_movMontoTexto.Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var monto) || monto <= 0)
        {
            ErrorMensaje = "Ingrese un monto válido mayor que cero.";
            return;
        }

        try
        {
            _service.RegistrarMovimiento(Jornada.Id, MovTipo, MovConcepto, monto, MovReferencia);
            MovConcepto = string.Empty;
            MovMontoTexto = string.Empty;
            MovReferencia = null;
            RefrescarMovimientos();
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void CalcularResumen()
    {
        ErrorMensaje = string.Empty;
        if (Jornada == null) return;

        if (EfectivoFisico < 0)
        {
            ErrorMensaje = "El efectivo físico no puede ser negativo.";
            return;
        }

        try
        {
            Resumen = _service.CalcularResumen(Jornada, EfectivoFisico);
            RefrescarAlertasYTop();
            Estado = EstadoVistaCierre.Resumen;
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void ConfirmarCierre()
    {
        ErrorMensaje = string.Empty;
        if (Jornada == null) return;

        try
        {
            Jornada = _service.ProcesarCierre(Jornada, EfectivoFisico, Observaciones);
            Resumen = _service.CalcularResumen(Jornada, EfectivoFisico);
            Estado  = EstadoVistaCierre.Cerrada;
        }
        catch (Exception ex)
        {
            ErrorMensaje = ex.Message;
        }
    }

    private void RefrescarMovimientos()
    {
        Movimientos.Clear();
        if (Jornada == null) return;
        foreach (var m in _service.CalcularResumen(Jornada, 0).Movimientos)
            Movimientos.Add(m);
    }

    private void RefrescarAlertasYTop()
    {
        if (Resumen == null) return;

        AlertasStock.Clear();
        foreach (var a in Resumen.AlertasStock) AlertasStock.Add(a);

        TopProductos.Clear();
        foreach (var t in Resumen.TopProductos) TopProductos.Add(t);
    }
}

public enum EstadoVistaCierre
{
    Cargando,
    SinJornada,
    Abierta,
    Conteo,
    Resumen,
    Cerrada
}
