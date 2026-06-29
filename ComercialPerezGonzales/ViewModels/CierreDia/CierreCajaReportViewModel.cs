using System;
using System.Collections.Generic;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Data.Repositories;

namespace ComercialPerezGonzales.ViewModels.CierreDia;

public class CierreCajaReportViewModel
{
    // Datos del negocio
    public string NombreNegocio { get; set; } = string.Empty;
    public string Direccion     { get; set; } = string.Empty;
    public string Telefono      { get; set; } = string.Empty;
    public string Rnc           { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = "$";
    public string NombreUsuario { get; set; } = string.Empty;

    // Datos del cierre
    public int Id { get; set; }
    public string FechaJornada { get; set; } = string.Empty;
    public DateTime FechaApertura { get; set; }
    public DateTime? FechaCierre { get; set; }
    public string UsuarioApertura { get; set; } = string.Empty;
    public string UsuarioCierre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    // Totales
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
    public decimal EfectivoEsperado { get; set; }
    public decimal EfectivoReal { get; set; }
    public decimal Diferencia { get; set; }
    public int CantidadVentas { get; set; }
    public string EstadoConciliacion { get; set; } = string.Empty;
    public string Observaciones { get; set; } = string.Empty;

    // Listados
    public List<MovimientoCaja> Movimientos { get; set; } = new();
    public List<AlertaStockItem> AlertasStock { get; set; } = new();
    public List<TopProductoItem> TopProductos { get; set; } = new();

    // Configuración de impresión
    public string ImpNombreImpresora { get; set; } = string.Empty;
    public string ImpTipoPapel       { get; set; } = "80mm";
    public int    ImpCopias          { get; set; } = 1;
    public int    ImpMargenArriba    { get; set; }
    public int    ImpMargenAbajo     { get; set; }
    public int    ImpMargenIzquierda { get; set; }
    public int    ImpMargenDerecha   { get; set; }
    public string ImpFuenteFamilia   { get; set; } = string.Empty;
    public int    ImpFuenteTamano    { get; set; } = 100;
}
