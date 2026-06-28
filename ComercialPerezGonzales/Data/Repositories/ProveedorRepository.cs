using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ProveedorRepository
{
    private readonly DatabaseContext _context;

    public ProveedorRepository(DatabaseContext context) => _context = context;

    public IEnumerable<Proveedor> GetAll()
    {
        using var conn = _context.CreateConnection();
        return conn.Query<Proveedor>("SELECT * FROM proveedores ORDER BY nombre");
    }

    public Proveedor? GetById(int id)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<Proveedor>("SELECT * FROM proveedores WHERE id = @Id", new { Id = id });
    }

    public IEnumerable<Proveedor> Search(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return GetAll();
        var t = $"%{term}%";
        using var conn = _context.CreateConnection();
        return conn.Query<Proveedor>(
            "SELECT * FROM proveedores WHERE nombre LIKE @T OR documento LIKE @T ORDER BY nombre",
            new { T = t });
    }

    public int Insert(Proveedor p)
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            INSERT INTO proveedores (
                documento, documento_fiscal, nombre, telefono, email, direccion, 
                contacto_nombre, contacto_telefono, contacto_email,
                dias_credito, limite_credito, condiciones_pago, metodo_pago_preferido, activo
            )
            VALUES (
                @Documento, @DocumentoFiscal, @Nombre, @Telefono, @Email, @Direccion, 
                @ContactoNombre, @ContactoTelefono, @ContactoEmail,
                @DiasCredito, @LimiteCredito, @CondicionesPago, @MetodoPagoPreferido, @Activo
            );
            SELECT last_insert_rowid();";
        p.Id = conn.ExecuteScalar<int>(sql, p);
        return p.Id;
    }

    public void Update(Proveedor p)
    {
        using var conn = _context.CreateConnection();
        var sql = @"
            UPDATE proveedores 
            SET documento = @Documento,
                documento_fiscal = @DocumentoFiscal,
                nombre = @Nombre,
                telefono = @Telefono,
                email = @Email,
                direccion = @Direccion,
                contacto_nombre = @ContactoNombre,
                contacto_telefono = @ContactoTelefono,
                contacto_email = @ContactoEmail,
                dias_credito = @DiasCredito,
                limite_credito = @LimiteCredito,
                condiciones_pago = @CondicionesPago,
                metodo_pago_preferido = @MetodoPagoPreferido,
                activo = @Activo
            WHERE id = @Id";
        conn.Execute(sql, p);
    }

    public void Delete(int id)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("DELETE FROM proveedores WHERE id = @Id", new { Id = id });
    }
}
