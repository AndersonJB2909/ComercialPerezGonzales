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
        var productos = conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1
            ORDER BY p.nombre",
            new { }).ToList();
        CargarConversiones(productos);
        return productos;
    }

    public Producto? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        var p = conn.QueryFirstOrDefault<Producto>(
            "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.id = @id",
            new { id });
        if (p != null) CargarConversiones(new[] { p });
        return p;
    }

    public Producto? GetByCodigo(string codigo)
    {
        using var conn = _context.CreateConnection();
        var p = conn.QueryFirstOrDefault<Producto>(
            "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.codigo = @codigo AND p.activo = 1",
            new { codigo });
        if (p != null) CargarConversiones(new[] { p });
        return p;
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

        var resultado = todos
            .Where(p => TextHelper.ContieneSinAcento(p.Nombre, texto)
                     || TextHelper.ContieneSinAcento(p.Codigo, texto)
                     || TextHelper.ContieneSinAcento(p.CategoriaNombre ?? "", texto))
            .Take(50).ToList();
        CargarConversiones(resultado);
        return resultado;
    }

    public IEnumerable<Producto> GetByCategoria(int categoriaId)
    {
        using var conn = _context.CreateConnection();
        var productos = conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1 AND p.categoria_id = @categoriaId
            ORDER BY p.nombre",
            new { categoriaId }).ToList();
        CargarConversiones(productos);
        return productos;
    }

    public int Insert(Producto p)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO productos (codigo, nombre, descripcion, precio_venta, precio_costo, stock, stock_minimo, categoria_id, unidad_medida, imagen_path, imagen_data, activo, fecha_caducidad)
            VALUES (@Codigo, @Nombre, @Descripcion, @PrecioVenta, @PrecioCosto, @Stock, @StockMinimo, @CategoriaId, @UnidadMedida, @ImagenPath, @ImagenData, 1, @FechaCaducidad);
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
                fecha_caducidad = @FechaCaducidad,
                updated_at = datetime('now','localtime')
            WHERE id = @Id", p);
    }

    public void UpdateStock(int id, decimal cantidad)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE productos SET stock = stock + @cantidad, updated_at = datetime('now','localtime') WHERE id = @id",
            new { id, cantidad });
    }

    public void UpdateImagen(int id, byte[] imagen)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"UPDATE productos SET imagen_data = @imagen,
            updated_at = datetime('now','localtime') WHERE id = @id",
            new { id, imagen });
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE productos SET activo = 0 WHERE id = @id", new { id });
    }

    public IEnumerable<Producto> GetBajoStock()
    {
        using var conn = _context.CreateConnection();
        var productos = conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1 AND p.stock <= p.stock_minimo
            ORDER BY p.stock ASC").ToList();
        CargarConversiones(productos);
        return productos;
    }

    public bool TieneDerivados(int id)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM producto_conversiones WHERE producto_base_id = @id",
            new { id }) > 0;
    }

    public IEnumerable<Producto> GetPaged(int page, int pageSize, string searchText, List<int> categoryIds, List<string> units, out int totalCount)
    {
        using var conn = _context.CreateConnection();
        
        var conditions = new List<string> { "p.activo = 1" };
        var parameters = new DynamicParameters();
        
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var searchPattern = $"%{searchText.Trim()}%";
            conditions.Add("(p.nombre LIKE @search OR p.codigo LIKE @search OR c.nombre LIKE @search)");
            parameters.Add("@search", searchPattern);
        }
        
        if (categoryIds != null && categoryIds.Any())
        {
            conditions.Add("p.categoria_id IN @categoryIds");
            parameters.Add("@categoryIds", categoryIds);
        }
        
        if (units != null && units.Any())
        {
            conditions.Add("p.unidad_medida IN @units");
            parameters.Add("@units", units);
        }
        
        var whereClause = string.Join(" AND ", conditions);
        
        var countSql = $@"
            SELECT COUNT(*) 
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE {whereClause}";
            
        totalCount = conn.ExecuteScalar<int>(countSql, parameters);
        
        var offset = (page - 1) * pageSize;
        parameters.Add("@pageSize", pageSize);
        parameters.Add("@offset", offset);
        
        var dataSql = $@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE {whereClause}
            ORDER BY p.nombre
            LIMIT @pageSize OFFSET @offset";
            
        var productos = conn.Query<Producto>(dataSql, parameters).ToList();
        CargarConversiones(productos);
        return productos;
    }

    private void CargarConversiones(IEnumerable<Producto> productos)
    {
        using var conn = _context.CreateConnection();
        var ids = productos.Select(p => p.Id).ToList();
        if (!ids.Any()) return;

        var conversiones = conn.Query<ProductoConversion>(@"
            SELECT pc.*,
                   p.nombre  AS ProductoNombre,
                   pb.nombre AS ProductoBaseNombre,
                   pb.stock  AS StockBase
            FROM producto_conversiones pc
            JOIN productos p  ON p.id  = pc.producto_id
            JOIN productos pb ON pb.id = pc.producto_base_id
            WHERE pc.producto_id IN @ids",
            new { ids }).ToDictionary(c => c.ProductoId);

        foreach (var prod in productos)
        {
            if (conversiones.TryGetValue(prod.Id, out var conv))
                prod.Conversion = conv;
        }
    }

    public IEnumerable<Producto> GetProductosPorVencer(int diasThreshold)
    {
        using var conn = _context.CreateConnection();
        var productos = conn.Query<Producto>(@"
            SELECT p.*, c.nombre as CategoriaNombre
            FROM productos p
            LEFT JOIN categorias c ON p.categoria_id = c.id
            WHERE p.activo = 1 AND p.fecha_caducidad IS NOT NULL
              AND date(p.fecha_caducidad) <= date('now', 'localtime', '+' || @diasThreshold || ' days')
            ORDER BY p.fecha_caducidad ASC", new { diasThreshold }).ToList();
        CargarConversiones(productos);
        return productos;
    }
}
