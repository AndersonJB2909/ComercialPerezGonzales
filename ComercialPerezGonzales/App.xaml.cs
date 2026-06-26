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
        services.AddSingleton<VentaRepository>();
        services.AddSingleton<ConfiguracionRepository>();
        services.AddSingleton<CierreCajaRepository>();

        services.AddSingleton<ProductoService>();
        services.AddSingleton<ProductoConversionService>();
        services.AddSingleton<ClienteService>();
        services.AddSingleton<VentaService>();
        services.AddSingleton<ReporteService>();
        services.AddSingleton<CierreCajaService>();

        services.AddSingleton<MainViewModel>();
        services.AddTransient<PosViewModel>();
        services.AddTransient<ProductosViewModel>();
        services.AddTransient<ClientesViewModel>();
        services.AddTransient<ReportesViewModel>();
        services.AddTransient<TableroViewModel>();
        services.AddTransient<ConfiguracionImpresionViewModel>();
        services.AddTransient<CierreDiaViewModel>();

        Services = services.BuildServiceProvider();
        Services.GetRequiredService<DatabaseInitializer>().Initialize();

        var mainWindow = new MainWindow { DataContext = Services.GetRequiredService<MainViewModel>() };
        mainWindow.Show();
    }
}
