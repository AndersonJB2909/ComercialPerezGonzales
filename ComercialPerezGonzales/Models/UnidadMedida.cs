namespace ComercialPerezGonzales.Models;

public class UnidadMedida
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;

    public override string ToString() => Nombre;
}
