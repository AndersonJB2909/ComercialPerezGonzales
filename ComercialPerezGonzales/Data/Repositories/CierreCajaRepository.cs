using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class CierreCajaRepository
{
    private readonly DatabaseContext _context;

    public CierreCajaRepository(DatabaseContext context) => _context = context;

    public CierreCaja? GetByFechaJornada(string fecha)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<CierreCaja>(
            "SELECT * FROM cierres_caja WHERE fecha_jornada = @fecha", new { fecha });
    }

    public CierreCaja Insertar(CierreCaja c)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        c.Id = conn.ExecuteScalar<int>(@"
            INSERT INTO cierres_caja (fecha_jornada, fondo_inicial, estado, usuario_apertura)
            VALUES (@FechaJornada, @FondoInicial, @Estado, @UsuarioApertura);
            SELECT last_insert_rowid();", c, tx);
        c.FechaApertura = DateTime.Now;
        tx.Commit();
        return c;
    }

    public void Actualizar(CierreCaja c)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE cierres_caja SET
                fecha_cierre         = @FechaCierre,
                total_efectivo       = @TotalEfectivo,
                total_tarjetas       = @TotalTarjetas,
                total_transferencias = @TotalTransferencias,
                total_bruto          = @TotalBruto,
                total_descuentos     = @TotalDescuentos,
                total_impuesto       = @TotalImpuesto,
                total_neto           = @TotalNeto,
                salidas_efectivo     = @SalidasEfectivo,
                entradas_extra       = @EntradasExtra,
                efectivo_esperado    = @EfectivoEsperado,
                efectivo_real        = @EfectivoReal,
                diferencia           = @Diferencia,
                cantidad_ventas      = @CantidadVentas,
                estado               = @Estado,
                observaciones        = @Observaciones,
                usuario_cierre       = @UsuarioCierre
            WHERE id = @Id", c);
    }

    public void InsertarMovimiento(MovimientoCaja m)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        m.Id = conn.ExecuteScalar<int>(@"
            INSERT INTO movimientos_caja (cierre_caja_id, tipo, concepto, monto, referencia, usuario_nombre)
            VALUES (@CierreCajaId, @Tipo, @Concepto, @Monto, @Referencia, @UsuarioNombre);
            SELECT last_insert_rowid();", m, tx);
        tx.Commit();
    }

    public List<MovimientoCaja> GetMovimientos(int cierreCajaId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<MovimientoCaja>(
            "SELECT * FROM movimientos_caja WHERE cierre_caja_id = @cierreCajaId ORDER BY fecha_hora",
            new { cierreCajaId }).ToList();
    }

    public TotalesDia GetTotalesDia(string fechaJornada)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<TotalesDia>(@"
            SELECT
                COALESCE(SUM(CASE WHEN metodo_pago = 'EFECTIVO'      THEN total ELSE 0 END), 0) AS Efectivo,
                COALESCE(SUM(CASE WHEN metodo_pago = 'TARJETA'       THEN total ELSE 0 END), 0) AS Tarjetas,
                COALESCE(SUM(CASE WHEN metodo_pago = 'TRANSFERENCIA' THEN total ELSE 0 END), 0) AS Transferencias,
                COALESCE(SUM(subtotal),  0) AS Bruto,
                COALESCE(SUM(descuento), 0) AS Descuentos,
                COALESCE(SUM(impuesto),  0) AS Impuesto,
                COALESCE(SUM(total),     0) AS Neto,
                COUNT(*)                    AS CantidadVentas
            FROM ventas
            WHERE estado = 'COMPLETADA'
              AND date(created_at) = @fechaJornada",
            new { fechaJornada }) ?? new TotalesDia();
    }

    public List<AlertaStockItem> GetProductosBajoStock()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<AlertaStockItem>(@"
            SELECT id AS ProductoId, nombre AS Nombre,
                   stock AS Stock, stock_minimo AS StockMinimo,
                   (stock_minimo - stock) AS Deficit
            FROM productos
            WHERE activo = 1 AND stock <= stock_minimo
            ORDER BY Deficit DESC").ToList();
    }

    public List<TopProductoItem> GetTopProductosDia(string fechaJornada)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<TopProductoItem>(@"
            SELECT p.id AS ProductoId, p.nombre AS Nombre,
                   SUM(dv.cantidad) AS CantidadVendida,
                   SUM(dv.subtotal) AS TotalVendido
            FROM detalle_ventas dv
            JOIN ventas v  ON v.id  = dv.venta_id
            JOIN productos p ON p.id = dv.producto_id
            WHERE v.estado = 'COMPLETADA' AND date(v.created_at) = @fechaJornada
            GROUP BY p.id
            ORDER BY CantidadVendida DESC
            LIMIT 10",
            new { fechaJornada }).ToList();
    }

    public void ConsolidarKardex(string fechaJornada)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            // ponytail: stock_resultante usa el stock actual del producto, no el stock por movimiento;
            // el stock ya fue decrementado por VentaRepository.Insert al momento de venta.
            conn.Execute(@"
                INSERT OR IGNORE INTO kardex
                    (producto_id, fecha_hora, tipo_movimiento, cantidad,
                     costo_unitario, stock_resultante, referencia_id, referencia_tipo, notas)
                SELECT
                    dv.producto_id,
                    v.created_at,
                    'SALIDA_VENTA',
                    dv.cantidad,
                    dv.precio_unit,
                    p.stock,
                    v.id,
                    'VENTA',
                    'Cierre: ' || v.numero
                FROM detalle_ventas dv
                JOIN ventas    v ON v.id  = dv.venta_id
                JOIN productos p ON p.id  = dv.producto_id
                WHERE v.estado = 'COMPLETADA'
                  AND date(v.created_at) = @fechaJornada",
                new { fechaJornada }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public class TotalesDia
{
    public decimal Efectivo { get; set; }
    public decimal Tarjetas { get; set; }
    public decimal Transferencias { get; set; }
    public decimal Bruto { get; set; }
    public decimal Descuentos { get; set; }
    public decimal Impuesto { get; set; }
    public decimal Neto { get; set; }
    public int CantidadVentas { get; set; }
}

public class AlertaStockItem
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal Stock { get; set; }
    public decimal StockMinimo { get; set; }
    public decimal Deficit { get; set; }
}

public class TopProductoItem
{
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CantidadVendida { get; set; }
    public decimal TotalVendido { get; set; }
}
