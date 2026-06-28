using System.Collections.Generic;
using System.Linq;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class FacturaCompraRepository
{
    private readonly DatabaseContext _context;

    public FacturaCompraRepository(DatabaseContext context) => _context = context;

    public int Insert(FacturaCompra factura)
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            INSERT INTO facturas_compras (orden_compra_id, proveedor_id, numero_factura, fecha_emision, subtotal, impuesto, total, saldo_pendiente, estado)
            VALUES (@OrdenCompraId, @ProveedorId, @NumeroFactura, @FechaEmision, @Subtotal, @Impuesto, @Total, @SaldoPendiente, @Estado);
            SELECT last_insert_rowid();";
        factura.Id = conn.ExecuteScalar<int>(sql, factura);
        return factura.Id;
    }

    public void UpdateEstadoYSaldo(int id, string estado, decimal saldoPendiente)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE facturas_compras SET estado = @estado, saldo_pendiente = @saldoPendiente WHERE id = @id", 
            new { id, estado, saldoPendiente });
    }

    public IEnumerable<FacturaCompra> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<FacturaCompra>(@"
            SELECT f.*, p.nombre as ProveedorNombre, o.numero as OrdenCompraNumero 
            FROM facturas_compras f 
            JOIN proveedores p ON f.proveedor_id = p.id 
            LEFT JOIN ordenes_compra o ON f.orden_compra_id = o.id
            ORDER BY f.fecha_emision DESC");
    }

    public IEnumerable<FacturaCompra> GetByProveedor(int proveedorId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<FacturaCompra>(@"
            SELECT f.*, p.nombre as ProveedorNombre, o.numero as OrdenCompraNumero 
            FROM facturas_compras f 
            JOIN proveedores p ON f.proveedor_id = p.id 
            LEFT JOIN ordenes_compra o ON f.orden_compra_id = o.id
            WHERE f.proveedor_id = @proveedorId
            ORDER BY f.fecha_emision DESC", new { proveedorId });
    }

    public FacturaCompra? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<FacturaCompra>(@"
            SELECT f.*, p.nombre as ProveedorNombre, o.numero as OrdenCompraNumero 
            FROM facturas_compras f 
            JOIN proveedores p ON f.proveedor_id = p.id 
            LEFT JOIN ordenes_compra o ON f.orden_compra_id = o.id
            WHERE f.id = @id", new { id });
    }
    
    public int InsertPago(PagoProveedor pago)
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            INSERT INTO pagos_proveedores (factura_compra_id, cierre_caja_id, fecha_pago, monto, metodo_pago, referencia, usuario_nombre)
            VALUES (@FacturaCompraId, @CierreCajaId, @FechaPago, @Monto, @MetodoPago, @Referencia, @UsuarioNombre);
            SELECT last_insert_rowid();";
        pago.Id = conn.ExecuteScalar<int>(sql, pago);
        return pago.Id;
    }
    
    public IEnumerable<PagoProveedor> GetPagosByFactura(int facturaId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<PagoProveedor>("SELECT * FROM pagos_proveedores WHERE factura_compra_id = @facturaId ORDER BY fecha_pago DESC", new { facturaId });
    }
}
