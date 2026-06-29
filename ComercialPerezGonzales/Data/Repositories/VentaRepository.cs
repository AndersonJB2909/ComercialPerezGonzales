using ComercialPerezGonzales.Models;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ComercialPerezGonzales.Data.Repositories;

public class VentaRepository
{
    private readonly DatabaseContext _context;

    public VentaRepository(DatabaseContext context) => _context = context;

    public int Insert(Venta venta, bool descontarStock = true)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var ventaId = conn.ExecuteScalar<int>(@"
                INSERT INTO ventas (numero, cliente_id, subtotal, descuento, impuesto, total, metodo_pago, monto_recibido, cambio, pago_efectivo, pago_tarjeta, pago_transferencia, referencia_transferencia, estado, notas)
                VALUES (@Numero, @ClienteId, @Subtotal, @Descuento, @Impuesto, @Total, @MetodoPago, @MontoRecibido, @Cambio, @PagoEfectivo, @PagoTarjeta, @PagoTransferencia, @ReferenciaTransferencia, @Estado, @Notas);
                SELECT last_insert_rowid();", venta, tx);

            foreach (var d in venta.Detalles)
            {
                d.VentaId = ventaId;
                conn.Execute(@"
                    INSERT INTO detalle_ventas (venta_id, producto_id, cantidad, precio_unit, descuento, subtotal)
                    VALUES (@VentaId, @ProductoId, @Cantidad, @PrecioUnit, @Descuento, @Subtotal)", d, tx);

                if (descontarStock)
                {
                    conn.Execute(@"
                        UPDATE productos SET stock = stock - @Cantidad, updated_at = datetime('now','localtime')
                        WHERE id = @ProductoId", new { d.Cantidad, d.ProductoId }, tx);
                }
            }

            tx.Commit();
            return ventaId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public Venta? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        var venta = conn.QueryFirstOrDefault<Venta>(@"
            SELECT v.*, c.nombre || ' ' || COALESCE(c.apellido, '') as ClienteNombre
            FROM ventas v LEFT JOIN clientes c ON v.cliente_id = c.id
            WHERE v.id = @id", new { id });

        if (venta != null)
            venta.Detalles = conn.Query<DetalleVenta>(@"
                SELECT d.*, p.nombre as ProductoNombre, p.codigo as ProductoCodigo
                FROM detalle_ventas d JOIN productos p ON d.producto_id = p.id
                WHERE d.venta_id = @id", new { id }).ToList();

        return venta;
    }

    public IEnumerable<Venta> GetByFecha(DateTime desde, DateTime hasta)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Venta>(@"
            SELECT v.*, c.nombre || ' ' || COALESCE(c.apellido, '') as ClienteNombre
            FROM ventas v LEFT JOIN clientes c ON v.cliente_id = c.id
            WHERE v.estado IN ('COMPLETADA', 'COTIZACION')
              AND date(v.created_at) BETWEEN @desde AND @hasta
            ORDER BY v.created_at DESC",
            new { desde = desde.ToString("yyyy-MM-dd"), hasta = hasta.ToString("yyyy-MM-dd") });
    }

    public (decimal Total, int Cantidad) GetResumenDia(DateTime fecha)
    {
        using var conn = _context.CreateConnection();
        var result = conn.QueryFirstOrDefault<(decimal Total, int Cantidad)>(@"
            SELECT COALESCE(SUM(total), 0) as Total, COUNT(*) as Cantidad
            FROM ventas WHERE estado = 'COMPLETADA' AND date(created_at) = @fecha",
            new { fecha = fecha.ToString("yyyy-MM-dd") });
        return result;
    }

    public string GetNextNumero()
    {
        using var conn = _context.CreateConnection();
        var maxId = conn.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) FROM ventas");
        return $"V-{(maxId + 1):D6}";
    }

    public void Anular(int id, string motivo)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var detalles = conn.Query<DetalleVenta>(
                "SELECT * FROM detalle_ventas WHERE venta_id = @id", new { id }, tx);

            foreach (var d in detalles)
            {
                // Para derivados el stock vive en el producto BASE — restaurar ahí
                var conv = conn.QueryFirstOrDefault<(int BaseId, double Factor)>(
                    "SELECT producto_base_id as BaseId, factor as Factor FROM producto_conversiones WHERE producto_id = @ProductoId",
                    new { d.ProductoId }, tx);

                if (conv != default)
                {
                    var cantidadBase = d.Cantidad * (decimal)conv.Factor;
                    conn.Execute("UPDATE productos SET stock = stock + @Cantidad, updated_at = datetime('now','localtime') WHERE id = @BaseId",
                        new { Cantidad = cantidadBase, conv.BaseId }, tx);
                }
                else
                {
                    conn.Execute("UPDATE productos SET stock = stock + @Cantidad, updated_at = datetime('now','localtime') WHERE id = @ProductoId",
                        new { d.Cantidad, d.ProductoId }, tx);
                }
            }

            conn.Execute("UPDATE ventas SET estado = 'ANULADA', notas = @motivo WHERE id = @id",
                new { id, motivo }, tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}
