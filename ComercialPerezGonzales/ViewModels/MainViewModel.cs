using System;
using System.Windows;
using System.Windows.Threading;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.ViewModels.Base;
using ComercialPerezGonzales.ViewModels.Clientes;
using ComercialPerezGonzales.ViewModels.Inventario;
using ComercialPerezGonzales.ViewModels.POS;
using ComercialPerezGonzales.ViewModels.Reportes;
using ComercialPerezGonzales.ViewModels.Configuracion;
using ComercialPerezGonzales.ViewModels.Tablero;
using ComercialPerezGonzales.ViewModels.CierreDia;
using ComercialPerezGonzales.ViewModels.Proveedores;
using ComercialPerezGonzales.ViewModels.Login;
using Microsoft.Extensions.DependencyInjection;

namespace ComercialPerezGonzales.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _services;
    private readonly DispatcherTimer _clock;
    private object? _currentView;
    private string _fechaHora = string.Empty;
    private bool _isLoggedIn;
    private LoginViewModel? _loginVM;

    public bool IsLoggedIn
    {
        get => _isLoggedIn;
        set => SetProperty(ref _isLoggedIn, value);
    }

    public LoginViewModel? LoginVM
    {
        get => _loginVM;
        set => SetProperty(ref _loginVM, value);
    }

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
    public RelayCommand NavigateProveedoresCommand { get; }
    public RelayCommand NavigateReportesCommand { get; }
    public RelayCommand NavigateConfiguracionCommand { get; }
    public RelayCommand NavigateCierreDiaCommand { get; }
    public RelayCommand NavigateDevolucionesCommand { get; }
    public RelayCommand CerrarSesionCommand { get; }
    public RelayCommand CerrarProgramaCommand { get; }

    public MainViewModel(IServiceProvider services)
    {
        _services = services;
        NavigateTableroCommand = new RelayCommand(() => 
        {
            var vm = _services.GetRequiredService<TableroViewModel>();
            vm.Actualizar();
            CurrentView = vm;
        });
        NavigatePosCommand        = new RelayCommand(() => CurrentView = _services.GetRequiredService<PosViewModel>());
        NavigateProductosCommand  = new RelayCommand(() => CurrentView = _services.GetRequiredService<ProductosViewModel>());
        NavigateClientesCommand   = new RelayCommand(() => CurrentView = _services.GetRequiredService<ClientesViewModel>());
        NavigateProveedoresCommand = new RelayCommand(() => CurrentView = _services.GetRequiredService<ProveedoresViewModel>());
        NavigateReportesCommand   = new RelayCommand(() =>
        {
            var vm = _services.GetRequiredService<ReportesViewModel>();
            vm.CargarCotizacionPOS = (venta) =>
            {
                var posVm = _services.GetRequiredService<PosViewModel>();
                posVm.CargarCotizacion(venta);
                CurrentView = posVm;
            };
            vm.AlMostrar();
            CurrentView = vm;
        });
        NavigateConfiguracionCommand = new RelayCommand(() => CurrentView = _services.GetRequiredService<ConfiguracionImpresionViewModel>());
        NavigateCierreDiaCommand  = new RelayCommand(() => CurrentView = _services.GetRequiredService<CierreDiaViewModel>());
        NavigateDevolucionesCommand = new RelayCommand(() => CurrentView = _services.GetRequiredService<DevolucionesViewModel>());
        CerrarSesionCommand = new RelayCommand(() =>
        {
            if (AppDialog.Show("¿Desea cerrar la sesión actual?", "Cerrar sesión",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                IsLoggedIn = false;
            }
        });

        CerrarProgramaCommand = new RelayCommand(() =>
        {
            if (AppDialog.Show("¿Desea cerrar el programa?", "Confirmar salida",
                    MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        });

        _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clock.Tick += (_, _) => FechaHora = DateTime.Now.ToString("dd/MM/yyyy  hh:mm tt");
        _clock.Start();
        FechaHora = DateTime.Now.ToString("dd/MM/yyyy  hh:mm tt");

        IsLoggedIn = false;
        LoginVM = new LoginViewModel(_services.GetRequiredService<ConfiguracionRepository>(), () =>
        {
            IsLoggedIn = true;
            CurrentView = _services.GetRequiredService<PosViewModel>();
        });
    }
}
