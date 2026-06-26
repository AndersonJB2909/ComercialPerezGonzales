using ComercialPerezGonzales.Helpers;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ProductoRepository
{
    private readonly DatabaseContext _context;

    public ProductoRepository(DatabaseContext context) => _context = context;

    public IEnumerable<Producto> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1
            ORDER BY p.nombre",
            new { });
    }

    public Producto? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<Producto>(
            "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.id = @id",
            new { id });
    }

    public Producto? GetByCodigo(string codigo)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<Producto>(
            "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.codigo = @codigo AND p.activo = 1",
            new { codigo });
    }

    public IEnumerable<Producto> Search(string texto)
    {
        using var conn = _context.CreateConnection();
        var todos = conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1
            ORDER BY p.nombre");

        return todos
            .Where(p => TextHelper.ContieneSinAcento(p.Nombre, texto)
                     || TextHelper.ContieneSinAcento(p.Codigo, texto)
                     || TextHelper.ContieneSinAcento(p.CategoriaNombre ?? "", texto))
            .Take(50);
    }

    public IEnumerable<Producto> GetByCategoria(int categoriaId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1 AND p.categoria_id = @categoriaId
            ORDER BY p.nombre",
            new { categoriaId });
    }

    public int Insert(Producto p)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO productos (codigo, nombre, descripcion, precio_venta, precio_costo, stock, stock_minimo, categoria_id, unidad_medida, imagen_path, imagen_data, activo)
            VALUES (@Codigo, @Nombre, @Descripcion, @PrecioVenta, @PrecioCosto, @Stock, @StockMinimo, @CategoriaId, @UnidadMedida, @ImagenPath, @ImagenData, 1);
            SELECT last_insert_rowid();", p);
    }

    public void Update(Producto p)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE productos SET
                codigo = @Codigo, nombre = @Nombre, descripcion = @Descripcion,
                precio_venta = @PrecioVenta, precio_costo = @PrecioCosto,
                stock = @Stock, stock_minimo = @StockMinimo, categoria_id = @CategoriaId,
                unidad_medida = @UnidadMedida, imagen_path = @ImagenPath, imagen_data = @ImagenData,
                updated_at = datetime('now','localtime')
            WHERE id = @Id", p);
    }

    public void UpdateStock(int id, decimal cantidad)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE productos SET stock = stock + @cantidad, updated_at = datetime('now','localtime') WHERE id = @id",
            new { id, cantidad });
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE productos SET activo = 0 WHERE id = @id", new { id });
    }

    public IEnumerable<Producto> GetBajoStock()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1 AND p.stock <= p.stock_minimo
            ORDER BY p.stock ASC");
    }
}
