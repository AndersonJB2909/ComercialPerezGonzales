using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class CierreCajaService
{
    private readonly CierreCajaRepository _repo;
    private readonly ConfiguracionRepository _configRepo;

    public CierreCajaService(CierreCajaRepository repo, ConfiguracionRepository configRepo)
    {
        _repo = repo;
        _configRepo = configRepo;
    }

    // Idempotente: devuelve la jornada existente si ya está abierta.
    public CierreCaja AbrirJornada(decimal? fondoInicial = null)
    {
        var hoy = DateTime.Today.ToString("yyyy-MM-dd");
        var existente = _repo.GetByFechaJornada(hoy);
        if (existente != null) return existente;

        var fondo = fondoInicial
            ?? (decimal.TryParse(_configRepo.GetValor("caja_fondo_inicial"), out var f) ? f : 0m);
        var usuario = _configRepo.GetValor("usuario_nombre") ?? string.Empty;

        return _repo.Insertar(new CierreCaja
        {
            FechaJornada   = hoy,
            FondoInicial   = fondo,
            Estado         = "ABIERTO",
            UsuarioApertura = usuario
        });
    }

    public void RegistrarMovimiento(int cierreCajaId, string tipo, string concepto, decimal monto, string? referencia = null)
    {
        if (monto <= 0) throw new ArgumentException("El monto debe ser mayor que cero.");
        if (string.IsNullOrWhiteSpace(concepto)) throw new ArgumentException("El concepto es obligatorio.");

        _repo.InsertarMovimiento(new MovimientoCaja
        {
            CierreCajaId  = cierreCajaId,
            FechaHora     = DateTime.Now,
            Tipo          = tipo,
            Concepto      = concepto.Trim(),
            Monto         = monto,
            Referencia    = referencia?.Trim(),
            UsuarioNombre = _configRepo.GetValor("usuario_nombre") ?? string.Empty
        });
    }

    public ResumenCierre CalcularResumen(CierreCaja cierre, decimal efectivoFisico)
    {
        var totales = _repo.GetTotalesDia(cierre.FechaJornada);
        var movs    = _repo.GetMovimientos(cierre.Id);

        var salidas  = movs.Where(m => m.Tipo == "SALIDA").Sum(m => m.Monto);
        var entradas = movs.Where(m => m.Tipo == "ENTRADA").Sum(m => m.Monto);
        var esperado = cierre.FondoInicial + totales.Efectivo - salidas + entradas;

        return new ResumenCierre
        {
            Totales          = totales,
            Movimientos      = movs,
            SalidasEfectivo  = salidas,
            EntradasExtra    = entradas,
            EfectivoEsperado = esperado,
            EfectivoReal     = efectivoFisico,
            Diferencia       = efectivoFisico - esperado,
            AlertasStock     = _repo.GetProductosBajoStock(),
            TopProductos     = _repo.GetTopProductosDia(cierre.FechaJornada)
        };
    }

    public CierreCaja ProcesarCierre(CierreCaja cierre, decimal efectivoFisico, string? observaciones)
    {
        if (cierre.EstaCerrado)
            throw new InvalidOperationException("La jornada ya está cerrada.");

        var resumen = CalcularResumen(cierre, efectivoFisico);

        cierre.TotalEfectivo       = resumen.Totales.Efectivo;
        cierre.TotalTarjetas       = resumen.Totales.Tarjetas;
        cierre.TotalTransferencias = resumen.Totales.Transferencias;
        cierre.TotalBruto          = resumen.Totales.Bruto;
        cierre.TotalDescuentos     = resumen.Totales.Descuentos;
        cierre.TotalImpuesto       = resumen.Totales.Impuesto;
        cierre.TotalNeto           = resumen.Totales.Neto;
        cierre.SalidasEfectivo     = resumen.SalidasEfectivo;
        cierre.EntradasExtra       = resumen.EntradasExtra;
        cierre.EfectivoEsperado    = resumen.EfectivoEsperado;
        cierre.EfectivoReal        = efectivoFisico;
        cierre.Diferencia          = resumen.Diferencia;
        cierre.CantidadVentas      = resumen.Totales.CantidadVentas;
        cierre.FechaCierre         = DateTime.Now;
        cierre.Estado              = "CERRADO";
        cierre.Observaciones       = observaciones?.Trim();
        cierre.UsuarioCierre       = _configRepo.GetValor("usuario_nombre") ?? string.Empty;

        _repo.Actualizar(cierre);
        _repo.ConsolidarKardex(cierre.FechaJornada);

        return cierre;
    }

    public CierreCaja? GetJornadaHoy()
        => _repo.GetByFechaJornada(DateTime.Today.ToString("yyyy-MM-dd"));
}

public class ResumenCierre
{
    public TotalesDia Totales { get; set; } = new();
    public List<MovimientoCaja> Movimientos { get; set; } = [];
    public decimal SalidasEfectivo { get; set; }
    public decimal EntradasExtra { get; set; }
    public decimal EfectivoEsperado { get; set; }
    public decimal EfectivoReal { get; set; }
    public decimal Diferencia { get; set; }
    public List<AlertaStockItem> AlertasStock { get; set; } = [];
    public List<TopProductoItem> TopProductos { get; set; } = [];

    public string EstadoConciliacion => Diferencia switch
    {
        > 0  => "SOBRANTE",
        < 0  => "FALTANTE",
        _    => "CUADRADO"
    };
}
