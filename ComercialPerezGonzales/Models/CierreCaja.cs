namespace ComercialPerezGonzales.Models;

public class CierreCaja
{
    public int Id { get; set; }
    public string FechaJornada { get; set; } = string.Empty;   // yyyy-MM-dd
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public decimal FondoInicial { get; set; }
    public decimal TotalEfectivo { get; set; }
    public decimal TotalTarjetas { get; set; }
    public decimal TotalTransferencias { get; set; }
    public decimal TotalBruto { get; set; }
    public decimal TotalDescuentos { get; set; }
    public decimal TotalImpuesto { get; set; }
    public decimal TotalNeto { get; set; }
    public decimal SalidasEfectivo { get; set; }
    public decimal EntradasExtra { get; set; }
    public decimal? EfectivoEsperado { get; set; }
    public decimal? EfectivoReal { get; set; }
    public decimal? Diferencia { get; set; }
    public int CantidadVentas { get; set; }
    public string Estado { get; set; } = "ABIERTO";
    public string? Observaciones { get; set; }
    public string? UsuarioApertura { get; set; }
    public string? UsuarioCierre { get; set; }

    public bool EstaCerrado => Estado == "CERRADO";

    public string EstadoConciliacion => Diferencia switch
    {
        null  => "—",
        > 0   => "SOBRANTE",
        < 0   => "FALTANTE",
        _     => "CUADRADO"
    };
}
