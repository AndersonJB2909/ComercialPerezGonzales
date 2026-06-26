using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ProductoConversionRepository
{
    private readonly DatabaseContext _context;

    public ProductoConversionRepository(DatabaseContext context) => _context = context;

    public ProductoConversion? GetByProductoId(int productoId)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<ProductoConversion>(@"
            SELECT pc.*,
                   p.nombre  AS ProductoNombre,
                   pb.nombre AS ProductoBaseNombre,
                   pb.stock  AS StockBase
            FROM producto_conversiones pc
            JOIN productos p  ON p.id  = pc.producto_id
            JOIN productos pb ON pb.id = pc.producto_base_id
            WHERE pc.producto_id = @productoId",
            new { productoId });
    }

    public IEnumerable<ProductoConversion> GetByProductoBaseId(int productoBaseId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ProductoConversion>(@"
            SELECT pc.*,
                   p.nombre  AS ProductoNombre,
                   pb.nombre AS ProductoBaseNombre,
                   pb.stock  AS StockBase
            FROM producto_conversiones pc
            JOIN productos p  ON p.id  = pc.producto_id
            JOIN productos pb ON pb.id = pc.producto_base_id
            WHERE pc.producto_base_id = @productoBaseId",
            new { productoBaseId });
    }

    public void Insert(ProductoConversion conv)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            INSERT INTO producto_conversiones (producto_id, producto_base_id, factor)
            VALUES (@ProductoId, @ProductoBaseId, @Factor)",
            conv);
    }

    public void Update(ProductoConversion conv)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE producto_conversiones
            SET producto_base_id = @ProductoBaseId, factor = @Factor
            WHERE producto_id = @ProductoId",
            conv);
    }

    public void Delete(int productoId)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("DELETE FROM producto_conversiones WHERE producto_id = @productoId",
            new { productoId });
    }

    public bool ExisteComoBase(int productoId)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM producto_conversiones WHERE producto_base_id = @productoId",
            new { productoId }) > 0;
    }
}
