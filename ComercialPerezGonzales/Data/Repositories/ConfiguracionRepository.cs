using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ConfiguracionRepository
{
    private readonly DatabaseContext _context;

    public ConfiguracionRepository(DatabaseContext context) => _context = context;

    public string? GetValor(string clave)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<string>("SELECT valor FROM configuracion WHERE clave = @clave", new { clave });
    }

    public void SetValor(string clave, string valor)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("UPDATE configuracion SET valor = @valor WHERE clave = @clave", new { clave, valor });
    }

    public IEnumerable<Configuracion> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Configuracion>("SELECT * FROM configuracion ORDER BY grupo, clave");
    }

    public IEnumerable<Configuracion> GetByGrupo(string grupo)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Configuracion>("SELECT * FROM configuracion WHERE grupo = @grupo ORDER BY clave", new { grupo });
    }
}
