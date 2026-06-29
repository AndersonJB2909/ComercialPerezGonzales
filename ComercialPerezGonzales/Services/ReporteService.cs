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
            SELECT date(Fecha) as Fecha,
                   COALESCE(SUM(EsVenta), 0) as CantidadVentas,
                   COALESCE(SUM(Monto), 0) as TotalVentas
            FROM (
                SELECT created_at as Fecha,
                       1 as EsVenta,
                       total as Monto
                FROM ventas
                WHERE estado = 'COMPLETADA' AND date(created_at) BETWEEN @desde AND @hasta
                
                UNION ALL
                
                SELECT fecha_hora as Fecha,
                       0 as EsVenta,
                       -monto_total as Monto
                FROM devoluciones
                WHERE date(fecha_hora) BETWEEN @desde AND @hasta
            )
            GROUP BY date(Fecha)
            ORDER BY Fecha DESC",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd") });
    }

    public IEnumerable<ResumenProducto> GetProductosMasVendidos(DateTime desde, DateTime hasta, int top = 10)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ResumenProducto>(@"
            SELECT p.nombre as Nombre, p.codigo as Codigo,
                   COALESCE(SUM(Cantidad), 0) as CantidadVendida,
                   COALESCE(SUM(Subtotal), 0) as TotalVendido
            FROM (
                SELECT dv.producto_id,
                       dv.cantidad as Cantidad,
                       dv.subtotal as Subtotal
                FROM detalle_ventas dv
                JOIN ventas v ON dv.venta_id = v.id
                WHERE v.estado = 'COMPLETADA' AND date(v.created_at) BETWEEN @desde AND @hasta
                
                UNION ALL
                
                SELECT dd.producto_id,
                       -dd.cantidad as Cantidad,
                       -dd.subtotal as Subtotal
                FROM detalle_devoluciones dd
                JOIN devoluciones dev ON dd.devolucion_id = dev.id
                WHERE date(dev.fecha_hora) BETWEEN @desde AND @hasta
            ) t
            JOIN productos p ON t.producto_id = p.id
            GROUP BY p.id
            ORDER BY CantidadVendida DESC
            LIMIT @top",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), top });
    }

    public IEnumerable<VentaMensual> GetVentasPorMes(int anio)
    {
        using var conn = _context.CreateConnection();
        var datos = conn.Query<(int Mes, decimal Total, int Cantidad)>(@"
            SELECT Mes,
                   COALESCE(SUM(Monto), 0) as Total,
                   COALESCE(SUM(EsVenta), 0) as Cantidad
            FROM (
                SELECT CAST(strftime('%m', created_at) AS INTEGER) as Mes,
                       total as Monto,
                       1 as EsVenta
                FROM ventas
                WHERE estado = 'COMPLETADA' AND strftime('%Y', created_at) = @anio
                
                UNION ALL
                
                SELECT CAST(strftime('%m', fecha_hora) AS INTEGER) as Mes,
                       -monto_total as Monto,
                       0 as EsVenta
                FROM devoluciones
                WHERE strftime('%Y', fecha_hora) = @anio
            )
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
            SELECT Semana,
                   COALESCE(SUM(Monto), 0) as Total,
                   COALESCE(SUM(EsVenta), 0) as Cantidad
            FROM (
                SELECT CAST(strftime('%W', created_at) AS INTEGER) as Semana,
                       total as Monto,
                       1 as EsVenta
                FROM ventas
                WHERE estado = 'COMPLETADA' AND strftime('%Y', created_at) = @anio
                
                UNION ALL
                
                SELECT CAST(strftime('%W', fecha_hora) AS INTEGER) as Semana,
                       -monto_total as Monto,
                       0 as EsVenta
                FROM devoluciones
                WHERE strftime('%Y', fecha_hora) = @anio
            )
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
            SELECT Anio,
                   COALESCE(SUM(Monto), 0) as Total,
                   COALESCE(SUM(EsVenta), 0) as Cantidad
            FROM (
                SELECT CAST(strftime('%Y', created_at) AS INTEGER) as Anio,
                       total as Monto,
                       1 as EsVenta
                FROM ventas
                WHERE estado = 'COMPLETADA'
                  AND CAST(strftime('%Y', created_at) AS INTEGER) BETWEEN @desde AND @hasta
                
                UNION ALL
                
                SELECT CAST(strftime('%Y', fecha_hora) AS INTEGER) as Anio,
                       -monto_total as Monto,
                       0 as EsVenta
                FROM devoluciones
                WHERE CAST(strftime('%Y', fecha_hora) AS INTEGER) BETWEEN @desde AND @hasta
            )
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
                   COALESCE(SUM(EsVenta), 0) as CantidadVentas,
                   COALESCE(SUM(Monto), 0) as TotalComprado
            FROM (
                SELECT v.cliente_id,
                       1 as EsVenta,
                       v.total as Monto
                FROM ventas v
                WHERE v.estado = 'COMPLETADA' AND date(v.created_at) BETWEEN @desde AND @hasta
                
                UNION ALL
                
                SELECT v.cliente_id,
                       0 as EsVenta,
                       -dev.monto_total as Monto
                FROM devoluciones dev
                JOIN ventas v ON dev.venta_id = v.id
                WHERE date(dev.fecha_hora) BETWEEN @desde AND @hasta
            ) t
            JOIN clientes c ON t.cliente_id = c.id
            WHERE c.nombre != 'Cliente'
            GROUP BY t.cliente_id
            ORDER BY TotalComprado DESC
            LIMIT @top",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd"), top });
    }

    public ResumenCaja GetResumenCaja(DateTime fecha)
    {
        using var conn = _context.CreateConnection();
        var resumen = conn.QueryFirstOrDefault<ResumenCaja>(@"
            SELECT
                COALESCE(SUM(EsVenta), 0) as TotalVentas,
                COALESCE(SUM(Monto), 0) as TotalIngresos,
                COALESCE(SUM(PagoEfectivo), 0) as TotalEfectivo,
                COALESCE(SUM(PagoTarjeta), 0) as TotalTarjeta,
                COALESCE(SUM(PagoTransferencia), 0) as TotalTransferencia,
                COALESCE(SUM(EsDevolucion), 0) as TotalDevoluciones,
                COALESCE(SUM(MontoDevuelto), 0) as TotalDevuelto
            FROM (
                SELECT 1 as EsVenta,
                       total as Monto,
                       pago_efectivo as PagoEfectivo,
                       pago_tarjeta as PagoTarjeta,
                       pago_transferencia as PagoTransferencia,
                       0 as EsDevolucion,
                       0 as MontoDevuelto
                FROM ventas
                WHERE estado = 'COMPLETADA' AND date(created_at) = @fecha
                
                UNION ALL
                
                SELECT 0 as EsVenta,
                       -monto_total as Monto,
                       CASE WHEN metodo_reembolso = 'EFECTIVO' THEN -monto_total ELSE 0 END as PagoEfectivo,
                       CASE WHEN metodo_reembolso = 'TARJETA' THEN -monto_total ELSE 0 END as PagoTarjeta,
                       CASE WHEN metodo_reembolso = 'TRANSFERENCIA' THEN -monto_total ELSE 0 END as PagoTransferencia,
                       1 as EsDevolucion,
                       monto_total as MontoDevuelto
                FROM devoluciones
                WHERE date(fecha_hora) = @fecha
            )",
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
    public int TotalDevoluciones { get; set; }
    public decimal TotalDevuelto { get; set; }
}
