using System.IO;
using System.Windows;
using Dapper;
using ComercialPerezGonzales.Data;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels;
using ComercialPerezGonzales.ViewModels.Clientes;
using ComercialPerezGonzales.ViewModels.Inventario;
using ComercialPerezGonzales.ViewModels.POS;
using ComercialPerezGonzales.ViewModels.Reportes;
using ComercialPerezGonzales.ViewModels.Configuracion;
using ComercialPerezGonzales.ViewModels.Tablero;
using ComercialPerezGonzales.ViewModels.CierreDia;
using ComercialPerezGonzales.ViewModels.Proveedores;
using Microsoft.Extensions.DependencyInjection;

namespace ComercialPerezGonzales;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DefaultTypeMap.MatchNamesWithUnderscores = true;

        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ComercialPerezGonzales", "pos.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        var services = new ServiceCollection();

        services.AddSingleton(new DatabaseContext(dbPath));
        services.AddSingleton<DatabaseInitializer>();
        services.AddSingleton<ProductoRepository>();
        services.AddSingleton<ProductoConversionRepository>();
        services.AddSingleton<CategoriaRepository>();
        services.AddSingleton<ClienteRepository>();
        services.AddSingleton<ProveedorRepository>();
        services.AddSingleton<VentaRepository>();
        services.AddSingleton<ConfiguracionRepository>();
        services.AddSingleton<CierreCajaRepository>();
        services.AddSingleton<DevolucionRepository>();
        services.AddSingleton<OrdenCompraRepository>();
        services.AddSingleton<FacturaCompraRepository>();

        services.AddSingleton<ProductoService>();
        services.AddSingleton<ProductoConversionService>();
        services.AddSingleton<ClienteService>();
        services.AddSingleton<ProveedorService>();
        services.AddSingleton<VentaService>();
        services.AddSingleton<ReporteService>();
        services.AddSingleton<CierreCajaService>();
        services.AddSingleton<DevolucionService>();
        services.AddSingleton<ImageSearchService>();
        services.AddSingleton<UnidadMedidaService>();
        services.AddSingleton<OrdenCompraService>();
        services.AddSingleton<FacturaCompraService>();

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<PosViewModel>();
        services.AddTransient<ProductosViewModel>();
        services.AddTransient<ClientesViewModel>();
        services.AddSingleton<ProveedoresViewModel>();
        services.AddSingleton<ReportesViewModel>();
        services.AddSingleton<TableroViewModel>();
        services.AddTransient<ConfiguracionImpresionViewModel>();
        services.AddTransient<CierreDiaViewModel>();
        services.AddSingleton<DevolucionesViewModel>();

        Services = services.BuildServiceProvider();
        Services.GetRequiredService<DatabaseInitializer>().Initialize();

        var mainWindow = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
        mainWindow.Show();
    }
}
