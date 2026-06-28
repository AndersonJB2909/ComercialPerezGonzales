using ComercialPerezGonzales.Data;
using Dapper;

namespace ComercialPerezGonzales.Services;

public class ReporteService
{
    private readonly DatabaseContext _context;

    public ReporteService(DatabaseContext context) => _context = context;

    public IEnumerable<ResumenVentaDia> GetVentasPorDia(DateTime desde, DateTime hasta)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ResumenVentaDia>(@"
            SELECT date(created_at) as Fecha,
                   COUNT(*) as CantidadVentas,
                   SUM(total) as TotalVentas
            FROM ventas
            WHERE estado = 'COMPLETADA'
              AND date(created_at) BETWEEN @desde AND @hasta
            GROUP BY date(created_at)
            ORDER BY Fecha DESC",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd") });
    }

    public IEnumerable<ResumenProducto> GetProductosMasVendidos(DateTime desde, DateTime hasta, int top = 10)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ResumenProducto>(@"
            SELECT p.nombre as Nombre, p.codigo as Codigo,
                   SUM(d.cantidad) as CantidadVendida,
                   SUM(d.subtotal) as TotalVendido
            FROM detalle_ventas d
            JOIN productos p ON d.producto_id = p.id
            JOIN ventas v ON d.venta_id = v.id
            WHERE v.estado = 'COMPLETADA'
              AND date(v.created_at) BETWEEN @desde AND @hasta
            GROUP BY p.id
            ORDER BY CantidadVendida DESC
            LIMIT @top",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), top });
    }

    public IEnumerable<VentaMensual> GetVentasPorMes(int anio)
    {
        using var conn = _context.CreateConnection();
        var datos = conn.Query<(int Mes, decimal Total, int Cantidad)>(@"
            SELECT CAST(strftime('%m', created_at) AS INTEGER) as Mes,
                   COALESCE(SUM(total), 0) as Total,
                   COUNT(*) as Cantidad
            FROM ventas WHERE estado = 'COMPLETADA' AND strftime('%Y', created_at) = @anio
            GROUP BY Mes", new { anio = anio.ToString() })
            .ToDictionary(x => x.Mes);

        var meses = new[] { "Ene","Feb","Mar","Abr","May","Jun","Jul","Ago","Sep","Oct","Nov","Dic" };
        var lista = Enumerable.Range(1, 12).Select(m => new VentaMensual
        {
            Mes = m,
            NombreMes = meses[m - 1],
            EtiquetaCompleta = $"Mes: {meses[m - 1]}",
            TotalVentas = datos.ContainsKey(m) ? datos[m].Total : 0,
            CantidadVentas = datos.ContainsKey(m) ? datos[m].Cantidad : 0
        }).ToList();

        CalcBarHeights(lista);
        return lista;
    }

    public IEnumerable<VentaMensual> GetVentasPorDiaReciente()
    {
        var desde = DateTime.Today.AddDays(-29);
        var datos = GetVentasPorDia(desde, DateTime.Today)
            .ToDictionary(x => x.Fecha);
        var lista = Enumerable.Range(0, 30).Select(i =>
        {
            var d = desde.AddDays(i);
            var k = d.ToString("yyyy-MM-dd");
            return new VentaMensual
            {
                NombreMes      = i % 5 == 0 ? d.ToString("d MMM") : string.Empty,
                EtiquetaCompleta = $"Día: {d.ToString("d MMM")}",
                TotalVentas    = datos.TryGetValue(k, out var r) ? r.TotalVentas    : 0,
                CantidadVentas = datos.TryGetValue(k, out var r2) ? r2.CantidadVentas : 0
            };
        }).ToList();
        CalcBarHeights(lista);
        return lista;
    }

    public IEnumerable<VentaMensual> GetVentasPorSemana(int anio)
    {
        using var conn = _context.CreateConnection();
        var datos = conn.Query<(int Semana, decimal Total, int Cantidad)>(@"
            SELECT CAST(strftime('%W', created_at) AS INTEGER) as Semana,
                   COALESCE(SUM(total), 0) as Total,
                   COUNT(*) as Cantidad
            FROM ventas WHERE estado = 'COMPLETADA' AND strftime('%Y', created_at) = @anio
            GROUP BY Semana", new { anio = anio.ToString() })
            .ToDictionary(x => x.Semana);
        var lista = Enumerable.Range(1, 52).Select(w => new VentaMensual
        {
            NombreMes      = w % 4 == 1 ? $"S{w}" : string.Empty,
            EtiquetaCompleta = $"Semana: {w}",
            TotalVentas    = datos.TryGetValue(w, out var r) ? r.Total    : 0,
            CantidadVentas = datos.TryGetValue(w, out var r2) ? r2.Cantidad : 0
        }).ToList();
        CalcBarHeights(lista);
        return lista;
    }

    public IEnumerable<VentaMensual> GetVentasPorAnio(int anioBase)
    {
        using var conn = _context.CreateConnection();
        int desde = anioBase - 4, hasta = anioBase;
        var datos = conn.Query<(int Anio, decimal Total, int Cantidad)>(@"
            SELECT CAST(strftime('%Y', created_at) AS INTEGER) as Anio,
                   COALESCE(SUM(total), 0) as Total,
                   COUNT(*) as Cantidad
            FROM ventas WHERE estado = 'COMPLETADA'
              AND CAST(strftime('%Y', created_at) AS INTEGER) BETWEEN @desde AND @hasta
            GROUP BY Anio", new { desde, hasta })
            .ToDictionary(x => x.Anio);
        var lista = Enumerable.Range(desde, 5).Select(y => new VentaMensual
        {
            NombreMes      = y.ToString(),
            EtiquetaCompleta = $"Año: {y}",
            TotalVentas    = datos.TryGetValue(y, out var r) ? r.Total    : 0,
            CantidadVentas = datos.TryGetValue(y, out var r2) ? r2.Cantidad : 0
        }).ToList();
        CalcBarHeights(lista);
        return lista;
    }

    private static void CalcBarHeights(List<VentaMensual> lista)
    {
        var max = lista.Max(x => x.TotalVentas);
        foreach (var v in lista)
            v.BarHeight = max > 0 ? (double)(v.TotalVentas / max) * 140 : 0;
    }

    public IEnumerable<ResumenCliente> GetTopClientes(DateTime desde, DateTime hasta, int top = 5)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ResumenCliente>(@"
            SELECT c.nombre || ' ' || COALESCE(c.apellido,'') as Nombre,
                   COUNT(v.id) as CantidadVentas,
                   SUM(v.total) as TotalComprado
            FROM ventas v JOIN clientes c ON v.cliente_id = c.id
            WHERE v.estado = 'COMPLETADA'
              AND date(v.created_at) BETWEEN @desde AND @hasta
              AND c.nombre != 'Cliente'
            GROUP BY v.cliente_id ORDER BY TotalComprado DESC LIMIT @top",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), top });
    }

    public ResumenCaja GetResumenCaja(DateTime fecha)
    {
        using var conn = _context.CreateConnection();
        var resumen = conn.QueryFirstOrDefault<ResumenCaja>(@"
            SELECT
                COUNT(*) as TotalVentas,
                COALESCE(SUM(total), 0) as TotalIngresos,
                COALESCE(SUM(CASE WHEN metodo_pago = 'EFECTIVO' THEN total ELSE 0 END), 0) as TotalEfectivo,
                COALESCE(SUM(CASE WHEN metodo_pago = 'TARJETA' THEN total ELSE 0 END), 0) as TotalTarjeta,
                COALESCE(SUM(CASE WHEN metodo_pago = 'TRANSFERENCIA' THEN total ELSE 0 END), 0) as TotalTransferencia
            FROM ventas
            WHERE estado = 'COMPLETADA' AND date(created_at) = @fecha",
            new { fecha = fecha.ToString("yyyy-MM-dd") });
        return resumen ?? new ResumenCaja();
    }
}

public class ResumenVentaDia
{
    public string Fecha { get; set; } = string.Empty;
    public int CantidadVentas { get; set; }
    public decimal TotalVentas { get; set; }
}

public class ResumenProducto
{
    public string Nombre { get; set; } = string.Empty;
    public string Codigo { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalVendido { get; set; }
}

public class VentaMensual
{
    public int Mes { get; set; }
    public string NombreMes { get; set; } = string.Empty;
    public string EtiquetaCompleta { get; set; } = string.Empty;
    public decimal TotalVentas { get; set; }
    public int CantidadVentas { get; set; }
    public double BarHeight { get; set; }
}

public class ResumenCliente
{
    public string Nombre { get; set; } = string.Empty;
    public int CantidadVentas { get; set; }
    public decimal TotalComprado { get; set; }
}

public class ResumenCaja
{
    public int TotalVentas { get; set; }
    public decimal TotalIngresos { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjeta { get; set; }
    public decimal TotalTransferencia { get; set; }
}
