namespace ComercialPerezGonzales.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string? Documento { get; set; } // RUC u otro documento
    public string? DocumentoFiscal { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    
    // Datos de contacto
    public string? ContactoNombre { get; set; }
    public string? ContactoTelefono { get; set; }
    public string? ContactoEmail { get; set; }

    // Condiciones comerciales
    public int DiasCredito { get; set; } = 0;
    public decimal LimiteCredito { get; set; } = 0;
    public string? CondicionesPago { get; set; }
    public string MetodoPagoPreferido { get; set; } = "EFECTIVO";

    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public override string ToString() => Nombre;
}
