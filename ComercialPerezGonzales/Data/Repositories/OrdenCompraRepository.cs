using System.Collections.Generic;
using System.Linq;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class OrdenCompraRepository
{
    private readonly DatabaseContext _context;

    public OrdenCompraRepository(DatabaseContext context) => _context = context;

    public int Insert(OrdenCompra orden)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            var sql = @"
                INSERT INTO ordenes_compra (numero, proveedor_id, estado, fecha_emision, fecha_esperada, notas)
                VALUES (@Numero, @ProveedorId, @Estado, @FechaEmision, @FechaEsperada, @Notas);
                SELECT last_insert_rowid();";
            orden.Id = conn.ExecuteScalar<int>(sql, orden, tx);

            foreach (var det in orden.Detalles)
            {
                det.OrdenCompraId = orden.Id;
                conn.Execute(@"
                    INSERT INTO detalle_ordenes_compra (orden_compra_id, producto_id, cantidad_solicitada, cantidad_recibida, costo_unitario)
                    VALUES (@OrdenCompraId, @ProductoId, @CantidadSolicitada, @CantidadRecibida, @CostoUnitario)", det, tx);
            }
            tx.Commit();
            return orden.Id;
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void Update(OrdenCompra orden)
    {
        using var conn = _context.CreateConnection();
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            conn.Execute(@"
                UPDATE ordenes_compra 
                SET estado = @Estado, fecha_esperada = @FechaEsperada, notas = @Notas 
                WHERE id = @Id", orden, tx);

            conn.Execute("DELETE FROM detalle_ordenes_compra WHERE orden_compra_id = @Id", new { orden.Id }, tx);

            foreach (var det in orden.Detalles)
            {
                det.OrdenCompraId = orden.Id;
                conn.Execute(@"
                    INSERT INTO detalle_ordenes_compra (orden_compra_id, producto_id, cantidad_solicitada, cantidad_recibida, costo_unitario)
                    VALUES (@OrdenCompraId, @ProductoId, @CantidadSolicitada, @CantidadRecibida, @CostoUnitario)", det, tx);
            }
            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }

    public void UpdateEstado(int id, string estado)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE ordenes_compra SET estado = @estado WHERE id = @id", new { id, estado });
    }

    public IEnumerable<OrdenCompra> GetAll()
    {
        using var conn = _context.CreateConnection();
        var ordenes = conn.Query<OrdenCompra>(@"
            SELECT o.*, p.nombre as ProveedorNombre 
            FROM ordenes_compra o 
            JOIN proveedores p ON o.proveedor_id = p.id 
            ORDER BY o.fecha_emision DESC").ToList();
            
        foreach(var o in ordenes)
        {
            o.Detalles = conn.Query<DetalleOrdenCompra>(@"
                SELECT d.*, pr.nombre as ProductoNombre, pr.codigo as ProductoCodigo
                FROM detalle_ordenes_compra d
                JOIN productos pr ON d.producto_id = pr.id
                WHERE d.orden_compra_id = @Id", new { o.Id }).ToList();
        }
        return ordenes;
    }

    public IEnumerable<OrdenCompra> GetByProveedor(int proveedorId)
    {
        using var conn = _context.CreateConnection();
        var ordenes = conn.Query<OrdenCompra>(@"
            SELECT o.*, p.nombre as ProveedorNombre 
            FROM ordenes_compra o 
            JOIN proveedores p ON o.proveedor_id = p.id 
            WHERE o.proveedor_id = @proveedorId
            ORDER BY o.fecha_emision DESC", new { proveedorId }).ToList();
            
        foreach(var o in ordenes)
        {
            o.Detalles = conn.Query<DetalleOrdenCompra>(@"
                SELECT d.*, pr.nombre as ProductoNombre, pr.codigo as ProductoCodigo
                FROM detalle_ordenes_compra d
                JOIN productos pr ON d.producto_id = pr.id
                WHERE d.orden_compra_id = @Id", new { o.Id }).ToList();
        }
        return ordenes;
    }

    public OrdenCompra? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        var o = conn.QueryFirstOrDefault<OrdenCompra>(@"
            SELECT o.*, p.nombre as ProveedorNombre 
            FROM ordenes_compra o 
            JOIN proveedores p ON o.proveedor_id = p.id 
            WHERE o.id = @id", new { id });
            
        if (o != null)
        {
            o.Detalles = conn.Query<DetalleOrdenCompra>(@"
                SELECT d.*, pr.nombre as ProductoNombre, pr.codigo as ProductoCodigo
                FROM detalle_ordenes_compra d
                JOIN productos pr ON d.producto_id = pr.id
                WHERE d.orden_compra_id = @Id", new { o.Id }).ToList();
        }
        return o;
    }

    public string GetNextNumero()
    {
        using var conn = _context.CreateConnection();
        // DESIGN-006: Usar el ID máximo secuencial para generar el número de orden de compra
        var maxId = conn.ExecuteScalar<int>("SELECT COALESCE(MAX(id), 0) FROM ordenes_compra");
        return $"OC-{(maxId + 1):D6}";
    }
}
