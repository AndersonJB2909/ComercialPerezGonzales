using ComercialPerezGonzales.Helpers;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ClienteRepository
{
    private readonly DatabaseContext _context;

    public ClienteRepository(DatabaseContext context) => _context = context;

    public IEnumerable<Cliente> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Cliente>("SELECT * FROM clientes WHERE activo = 1 ORDER BY nombre");
    }

    public Cliente? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<Cliente>("SELECT * FROM clientes WHERE id = @id", new { id });
    }

    public IEnumerable<Cliente> Search(string texto)
    {
        using var conn = _context.CreateConnection();
        var todos = conn.Query<Cliente>("SELECT * FROM clientes WHERE activo = 1 ORDER BY nombre");
        return todos
            .Where(c => TextHelper.ContieneSinAcento(c.Nombre, texto)
                     || TextHelper.ContieneSinAcento(c.Apellido ?? "", texto)
                     || TextHelper.ContieneSinAcento(c.Documento ?? "", texto)
                     || TextHelper.ContieneSinAcento(c.Telefono ?? "", texto))
            .Take(30);
    }

    public int Insert(Cliente c)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(@"
            INSERT INTO clientes (codigo, nombre, apellido, documento, telefono, email, direccion)
            VALUES (@Codigo, @Nombre, @Apellido, @Documento, @Telefono, @Email, @Direccion);
            SELECT last_insert_rowid();", c);
    }

    public void Update(Cliente c)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE clientes SET codigo = @Codigo, nombre = @Nombre, apellido = @Apellido,
                documento = @Documento, telefono = @Telefono, email = @Email, direccion = @Direccion
            WHERE id = @Id", c);
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE clientes SET activo = 0 WHERE id = @id", new { id });
    }
}
