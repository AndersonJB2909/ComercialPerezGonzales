using ComercialPerezGonzales.Data;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Services;

public class UnidadMedidaService
{
    private readonly DatabaseContext _context;

    public UnidadMedidaService(DatabaseContext context)
    {
        _context = context;
    }

    public IEnumerable<UnidadMedida> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<UnidadMedida>("SELECT id as Id, nombre as Nombre FROM unidades_medida ORDER BY nombre");
    }

    public void Insert(UnidadMedida unidad)
    {
        using var conn = _context.CreateConnection();
        var sql = "INSERT INTO unidades_medida (nombre) VALUES (@Nombre) RETURNING id";
        unidad.Id = conn.QuerySingle<int>(sql, unidad);
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("DELETE FROM unidades_medida WHERE id = @Id", new { Id = id });
    }

    public bool PuedeEliminarse(string nombre)
    {
        using var conn = _context.CreateConnection();
        var count = conn.QuerySingle<int>("SELECT COUNT(*) FROM productos WHERE unidad_medida = @Nombre", new { Nombre = nombre });
        return count == 0;
    }
}
