using System.Collections.ObjectModel;
using System.Windows;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Configuracion;

public class ConfiguracionImpresionViewModel : ViewModelBase
{
    private readonly ConfiguracionRepository _repo;

    // ── Impresora ────────────────────────────────────────────────────────────
    private string _impresora = string.Empty;
    private string _tipoPapel = "58mm";
    private int    _copias = 1;

    // ── Márgenes ─────────────────────────────────────────────────────────────
    private int _margenArriba;
    private int _margenAbajo;
    private int _margenIzquierda;
    private int _margenDerecha;

    // ── Encabezado / Pie ─────────────────────────────────────────────────────
    private string _encabezado = string.Empty;
    private string _pie        = string.Empty;

    // ── Fuente ───────────────────────────────────────────────────────────────
    private string _fuenteFamilia = string.Empty;
    private int    _fuenteTamano  = 100;

    // ── Opciones ─────────────────────────────────────────────────────────────
    private bool _imprimirAlVender;
    private bool _imprimirCodigoBarras;
    private bool _logoAnchoCompleto;

    // ── Sección activa (panel derecho) ───────────────────────────────────────
    private string _seccionActiva = "IMPRESORA";

    public ObservableCollection<string> Impresoras    { get; } = new();
    public ObservableCollection<string> TamaniosPapel { get; } = new() { "58mm", "80mm", "A4", "Carta" };
    public ObservableCollection<string> Fuentes       { get; } = new();

    public string SeccionActiva
    {
        get => _seccionActiva;
        set { SetProperty(ref _seccionActiva, value); NotifySeccionBools(); }
    }

    // Helpers de visibilidad para cada panel
    public bool MostrarImpresora   => _seccionActiva == "IMPRESORA";
    public bool MostrarPapel       => _seccionActiva == "PAPEL";
    public bool MostrarEncabezado  => _seccionActiva == "ENCABEZADO";
    public bool MostrarFuente      => _seccionActiva == "FUENTE";
    public bool MostrarOpciones    => _seccionActiva == "OPCIONES";

    public string Impresora
    {
        get => _impresora;
        set => SetProperty(ref _impresora, value);
    }

    public string TipoPapel
    {
        get => _tipoPapel;
        set => SetProperty(ref _tipoPapel, value);
    }

    public int Copias
    {
        get => _copias;
        set => SetProperty(ref _copias, Math.Max(1, Math.Min(10, value)));
    }

    public int MargenArriba
    {
        get => _margenArriba;
        set => SetProperty(ref _margenArriba, Math.Max(0, value));
    }

    public int MargenAbajo
    {
        get => _margenAbajo;
        set => SetProperty(ref _margenAbajo, Math.Max(0, value));
    }

    public int MargenIzquierda
    {
        get => _margenIzquierda;
        set => SetProperty(ref _margenIzquierda, Math.Max(0, value));
    }

    public int MargenDerecha
    {
        get => _margenDerecha;
        set => SetProperty(ref _margenDerecha, Math.Max(0, value));
    }

    public string Encabezado
    {
        get => _encabezado;
        set => SetProperty(ref _encabezado, value);
    }

    public string Pie
    {
        get => _pie;
        set => SetProperty(ref _pie, value);
    }

    public string FuenteFamilia
    {
        get => _fuenteFamilia;
        set => SetProperty(ref _fuenteFamilia, value);
    }

    public int FuenteTamano
    {
        get => _fuenteTamano;
        set => SetProperty(ref _fuenteTamano, Math.Max(50, Math.Min(150, value)));
    }

    public bool ImprimirAlVender
    {
        get => _imprimirAlVender;
        set => SetProperty(ref _imprimirAlVender, value);
    }

    public bool ImprimirCodigoBarras
    {
        get => _imprimirCodigoBarras;
        set => SetProperty(ref _imprimirCodigoBarras, value);
    }

    public bool LogoAnchoCompleto
    {
        get => _logoAnchoCompleto;
        set => SetProperty(ref _logoAnchoCompleto, value);
    }

    // ── Comandos ─────────────────────────────────────────────────────────────
    public RelayCommand GuardarCommand     { get; }
    public RelayCommand IncrCopiaCommand   { get; }
    public RelayCommand DecrCopiaCommand   { get; }
    public RelayCommand NavImpresoraCommand { get; }
    public RelayCommand NavPapelCommand     { get; }
    public RelayCommand NavEncabezadoCommand { get; }
    public RelayCommand NavFuenteCommand    { get; }
    public RelayCommand NavOpcionesCommand  { get; }

    public ConfiguracionImpresionViewModel(ConfiguracionRepository repo)
    {
        _repo = repo;

        GuardarCommand       = new RelayCommand(Guardar);
        IncrCopiaCommand     = new RelayCommand(() => Copias++);
        DecrCopiaCommand     = new RelayCommand(() => Copias--);
        NavImpresoraCommand  = new RelayCommand(() => SeccionActiva = "IMPRESORA");
        NavPapelCommand      = new RelayCommand(() => SeccionActiva = "PAPEL");
        NavEncabezadoCommand = new RelayCommand(() => SeccionActiva = "ENCABEZADO");
        NavFuenteCommand     = new RelayCommand(() => SeccionActiva = "FUENTE");
        NavOpcionesCommand   = new RelayCommand(() => SeccionActiva = "OPCIONES");

        CargarImpresoras();
        CargarFuentes();
        CargarDesdeBD();
    }

    private void CargarImpresoras()
    {
        Impresoras.Clear();
        try
        {
            foreach (string name in System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                Impresoras.Add(name);
        }
        catch { /* sin impresoras instaladas */ }
    }

    private void CargarFuentes()
    {
        Fuentes.Clear();
        Fuentes.Add("(Predeterminada)");
        try
        {
            var ifc = new System.Drawing.Text.InstalledFontCollection();
            foreach (var f in ifc.Families.Take(60))
                Fuentes.Add(f.Name);
            ifc.Dispose();
        }
        catch { }
    }

    private void CargarDesdeBD()
    {
        Impresora           = Get("imp_impresora",      string.Empty);
        TipoPapel           = Get("imp_papel",          "58mm");
        Copias              = int.TryParse(Get("imp_copias", "1"), out var c) ? c : 1;
        MargenArriba        = int.TryParse(Get("imp_margen_arriba",    "0"), out var ma) ? ma : 0;
        MargenAbajo         = int.TryParse(Get("imp_margen_abajo",     "0"), out var mb) ? mb : 0;
        MargenIzquierda     = int.TryParse(Get("imp_margen_izquierda", "0"), out var mi) ? mi : 0;
        MargenDerecha       = int.TryParse(Get("imp_margen_derecha",   "0"), out var md) ? md : 0;
        Encabezado          = Get("imp_encabezado", string.Empty);
        Pie                 = Get("imp_pie",         string.Empty);
        FuenteFamilia       = Get("imp_fuente_familia", "(Predeterminada)");
        FuenteTamano        = int.TryParse(Get("imp_fuente_tamano", "100"), out var ft) ? ft : 100;
        ImprimirAlVender    = Get("imprimir_ticket",      "false") == "true";
        ImprimirCodigoBarras = Get("imp_codigo_barras",   "false") == "true";
        LogoAnchoCompleto   = Get("imp_logo_ancho",       "false") == "true";
    }

    private string Get(string clave, string defecto) =>
        _repo.GetValor(clave) ?? defecto;

    private void Guardar()
    {
        try
        {
            Set("imp_impresora",        Impresora);
            Set("imp_papel",            TipoPapel);
            Set("imp_copias",           Copias.ToString());
            Set("imp_margen_arriba",    MargenArriba.ToString());
            Set("imp_margen_abajo",     MargenAbajo.ToString());
            Set("imp_margen_izquierda", MargenIzquierda.ToString());
            Set("imp_margen_derecha",   MargenDerecha.ToString());
            Set("imp_encabezado",       Encabezado);
            Set("imp_pie",              Pie);
            Set("imp_fuente_familia",   FuenteFamilia);
            Set("imp_fuente_tamano",    FuenteTamano.ToString());
            Set("imprimir_ticket",      ImprimirAlVender    ? "true" : "false");
            Set("imp_codigo_barras",    ImprimirCodigoBarras ? "true" : "false");
            Set("imp_logo_ancho",       LogoAnchoCompleto   ? "true" : "false");

            MessageBox.Show("Configuración guardada correctamente.", "Guardado",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al guardar: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Set(string clave, string valor) => _repo.SetValor(clave, valor);

    private void NotifySeccionBools()
    {
        OnPropertyChanged(nameof(MostrarImpresora));
        OnPropertyChanged(nameof(MostrarPapel));
        OnPropertyChanged(nameof(MostrarEncabezado));
        OnPropertyChanged(nameof(MostrarFuente));
        OnPropertyChanged(nameof(MostrarOpciones));
    }
}
