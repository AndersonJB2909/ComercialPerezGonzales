using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Data.Repositories;

public class DevolucionRepository
{
    private readonly DatabaseContext _context;

    public DevolucionRepository(DatabaseContext context) => _context = context;

    public int Insert(Devolucion dev)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var devId = conn.ExecuteScalar<int>(@"
                INSERT INTO devoluciones (venta_id, cierre_caja_id, motivo, monto_subtotal, monto_descuento, monto_impuesto, monto_total, metodo_reembolso, supervisor_autorizo, cajero_solicito, nota_credito_codigo)
                VALUES (@VentaId, @CierreCajaId, @Motivo, @MontoSubtotal, @MontoDescuento, @MontoImpuesto, @MontoTotal, @MetodoReembolso, @SupervisorAutorizo, @CajeroSolicito, @NotaCreditoCodigo);
                SELECT last_insert_rowid();", dev, tx);

            foreach (var d in dev.Detalles)
            {
                d.DevolucionId = devId;
                conn.Execute(@"
                    INSERT INTO detalle_devoluciones (devolucion_id, producto_id, cantidad, precio_unit, subtotal, estado_producto)
                    VALUES (@DevolucionId, @ProductoId, @Cantidad, @PrecioUnit, @Subtotal, @EstadoProducto)", d, tx);

                if (d.EstadoProducto == "STOCK")
                {
                    conn.Execute(@"
                        UPDATE productos SET stock = stock + @Cantidad, updated_at = datetime('now','localtime')
                        WHERE id = @ProductoId", new { d.Cantidad, d.ProductoId }, tx);

                    // Registrar en kardex solo cuando el stock físico cambia realmente
                    var currentStock = conn.ExecuteScalar<double>("SELECT stock FROM productos WHERE id = @ProductoId", new { d.ProductoId }, tx);
                    conn.Execute(@"
                        INSERT INTO kardex (producto_id, tipo_movimiento, cantidad, costo_unitario, stock_resultante, referencia_id, referencia_tipo, notas)
                        VALUES (@ProductoId, 'DEVOLUCION', @Cantidad, @PrecioUnit, @currentStock, @DevolucionId, 'DEVOLUCION', @Notas)",
                        new {
                            d.ProductoId,
                            d.Cantidad,
                            d.PrecioUnit,
                            currentStock,
                            DevolucionId = devId,
                            Notas = "Devolución de venta. Estado: STOCK"
                        }, tx);
                }
                // MERMA: el stock no cambia, no se registra movimiento en kardex
            }

            tx.Commit();
            return devId;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public IEnumerable<Devolucion> GetByCierreCajaId(int cierreCajaId)
    {
        using var conn = _context.CreateConnection();
        var devs = conn.Query<Devolucion>(@"
            SELECT d.*, v.numero as VentaNumero, nc.monto_disponible as NotaCreditoDisponible
            FROM devoluciones d 
            JOIN ventas v ON d.venta_id = v.id
            LEFT JOIN notas_credito nc ON d.nota_credito_codigo = nc.codigo
            WHERE d.cierre_caja_id = @cierreCajaId
            ORDER BY d.fecha_hora DESC", new { cierreCajaId }).ToList();

        foreach (var dev in devs)
        {
            dev.Detalles = conn.Query<DetalleDevolucion>(@"
                SELECT dd.*, p.nombre as ProductoNombre, p.codigo as ProductoCodigo
                FROM detalle_devoluciones dd JOIN productos p ON dd.producto_id = p.id
                WHERE dd.devolucion_id = @id", new { id = dev.Id }).ToList();
        }

        return devs;
    }

    public IEnumerable<Devolucion> GetByVentaId(int ventaId)
    {
        using var conn = _context.CreateConnection();
        var devs = conn.Query<Devolucion>(@"
            SELECT d.*, v.numero as VentaNumero, nc.monto_disponible as NotaCreditoDisponible
            FROM devoluciones d 
            JOIN ventas v ON d.venta_id = v.id
            LEFT JOIN notas_credito nc ON d.nota_credito_codigo = nc.codigo
            WHERE d.venta_id = @ventaId
            ORDER BY d.fecha_hora DESC", new { ventaId }).ToList();

        foreach (var dev in devs)
        {
            dev.Detalles = conn.Query<DetalleDevolucion>(@"
                SELECT dd.*, p.nombre as ProductoNombre, p.codigo as ProductoCodigo
                FROM detalle_devoluciones dd JOIN productos p ON dd.producto_id = p.id
                WHERE dd.devolucion_id = @id", new { id = dev.Id }).ToList();
        }

        return devs;
    }

    public IEnumerable<Devolucion> GetAll()
    {
        using var conn = _context.CreateConnection();
        var devs = conn.Query<Devolucion>(@"
            SELECT d.*, v.numero as VentaNumero, nc.monto_disponible as NotaCreditoDisponible
            FROM devoluciones d 
            JOIN ventas v ON d.venta_id = v.id
            LEFT JOIN notas_credito nc ON d.nota_credito_codigo = nc.codigo
            ORDER BY d.fecha_hora DESC").ToList();

        foreach (var dev in devs)
        {
            dev.Detalles = conn.Query<DetalleDevolucion>(@"
                SELECT dd.*, p.nombre as ProductoNombre, p.codigo as ProductoCodigo
                FROM detalle_devoluciones dd JOIN productos p ON dd.producto_id = p.id
                WHERE dd.devolucion_id = @id", new { id = dev.Id }).ToList();
        }

        return devs;
    }

    // ── Notas de Crédito ──────────────────────────────────────────────────
    
    public void InsertNotaCredito(NotaCredito nc)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            INSERT INTO notas_credito (codigo, cliente_id, monto_inicial, monto_disponible, estado, fecha_vencimiento)
            VALUES (@Codigo, @ClienteId, @MontoInicial, @MontoDisponible, @Estado, @FechaVencimiento)",
            new {
                nc.Codigo,
                nc.ClienteId,
                nc.MontoInicial,
                nc.MontoDisponible,
                nc.Estado,
                FechaVencimiento = nc.FechaVencimiento.ToString("yyyy-MM-dd HH:mm:ss")
            });
    }

    public NotaCredito? GetNotaCreditoByCodigo(string codigo)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<NotaCredito>(@"
            SELECT nc.*, c.nombre || ' ' || COALESCE(c.apellido, '') as ClienteNombre
            FROM notas_credito nc LEFT JOIN clientes c ON nc.cliente_id = c.id
            WHERE nc.codigo = @codigo", new { codigo });
    }

    public void UpdateNotaCredito(NotaCredito nc)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE notas_credito
            SET monto_disponible = @MontoDisponible, estado = @Estado
            WHERE id = @Id", nc);
    }

    public string GetNextNotaCreditoCodigo()
    {
        using var conn = _context.CreateConnection();
        var maxId = conn.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) FROM notas_credito");
        return $"NC-{(maxId + 1):D6}";
    }
}
