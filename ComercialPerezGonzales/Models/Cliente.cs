namespace ComercialPerezGonzales.Models;

public class Cliente
{
    public int Id { get; set; }
    public string? Codigo { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Apellido { get; set; }
    public string? Documento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public string NombreCompleto => string.IsNullOrWhiteSpace(Apellido) ? Nombre : $"{Nombre} {Apellido}";

    public override string ToString() => NombreCompleto;
}
