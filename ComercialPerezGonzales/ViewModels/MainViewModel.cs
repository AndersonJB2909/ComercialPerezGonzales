using System.Windows;
using System.Windows.Threading;
using ComercialPerezGonzales.ViewModels.Base;
using ComercialPerezGonzales.ViewModels.Clientes;
using ComercialPerezGonzales.ViewModels.Inventario;
using ComercialPerezGonzales.ViewModels.POS;
using ComercialPerezGonzales.ViewModels.Reportes;
using ComercialPerezGonzales.ViewModels.Configuracion;
using ComercialPerezGonzales.ViewModels.Tablero;
using ComercialPerezGonzales.ViewModels.CierreDia;
using Microsoft.Extensions.DependencyInjection;

namespace ComercialPerezGonzales.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly DispatcherTimer _clock;
    private object? _currentView;
    private string _fechaHora = string.Empty;

    public object? CurrentView
    {
        get => _currentView;
        set => SetProperty(ref _currentView, value);
    }

    public string FechaHora
    {
        get => _fechaHora;
        private set => SetProperty(ref _fechaHora, value);
    }

    public RelayCommand NavigateTableroCommand { get; }
    public RelayCommand NavigatePosCommand { get; }
    public RelayCommand NavigateProductosCommand { get; }
    public RelayCommand NavigateClientesCommand { get; }
    public RelayCommand NavigateReportesCommand { get; }
    public RelayCommand NavigateConfiguracionCommand { get; }
    public RelayCommand NavigateCierreDiaCommand { get; }
    public RelayCommand SalirCommand { get; }

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        NavigateTableroCommand    = new RelayCommand(() => CurrentView = _services.GetRequiredService<TableroViewModel>());
        NavigatePosCommand        = new RelayCommand(() => CurrentView = _services.GetRequiredService<PosViewModel>());
        NavigateProductosCommand  = new RelayCommand(() => CurrentView = _services.GetRequiredService<ProductosViewModel>());
        NavigateClientesCommand   = new RelayCommand(() => CurrentView = _services.GetRequiredService<ClientesViewModel>());
        NavigateReportesCommand   = new RelayCommand(() => CurrentView = _services.GetRequiredService<ReportesViewModel>());
        NavigateConfiguracionCommand = new RelayCommand(() => CurrentView = _services.GetRequiredService<ConfiguracionImpresionViewModel>());
        NavigateCierreDiaCommand  = new RelayCommand(() => CurrentView = _services.GetRequiredService<CierreDiaViewModel>());
        SalirCommand = new RelayCommand(() =>
        {
            if (MessageBox.Show("¿Desea cerrar el programa?", "Confirmar salida",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        });

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => FechaHora = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");
        _clock.Start();
        FechaHora = DateTime.Now.ToString("dd/MM/yyyy  HH:mm");

        CurrentView = _services.GetRequiredService<PosViewModel>();
    }
}
