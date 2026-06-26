namespace ComercialPerezGonzales.Models;

public class Configuracion
{
    public string Clave { get; set; } = string.Empty;
    public string? Valor { get; set; }
    public string Tipo { get; set; } = "STRING";
    public string Grupo { get; set; } = "GENERAL";
    public string? Descripcion { get; set; }
}
