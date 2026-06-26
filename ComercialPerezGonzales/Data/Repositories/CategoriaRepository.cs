using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class CategoriaRepository
{
    private readonly DatabaseContext _context;

    public CategoriaRepository(DatabaseContext context) => _context = context;

    public IEnumerable<Categoria> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Categoria>("SELECT * FROM categorias WHERE activo = 1 ORDER BY nombre");
    }

    public int Insert(Categoria c)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(
            "INSERT INTO categorias (nombre, descripcion) VALUES (@Nombre, @Descripcion); SELECT last_insert_rowid();", c);
    }

    public void Update(Categoria c)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE categorias SET nombre = @Nombre, descripcion = @Descripcion WHERE id = @Id", c);
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE categorias SET activo = 0 WHERE id = @id", new { id });
    }
}
