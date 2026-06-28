using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ComercialPerezGonzales.Services;
using ZXing;
using ZXing.Common;

namespace ComercialPerezGonzales.ViewModels.POS;

public class ReciboViewModel
{
    // Datos de la venta
    public string   NumeroVenta   { get; set; } = string.Empty;
    public DateTime FechaVenta    { get; set; } = DateTime.Now;
    public decimal  TotalVenta    { get; set; }
    public decimal  Subtotal      { get; set; }
    public decimal  Descuento     { get; set; }
    public bool     HasDiscount   => Descuento > 0;
    public decimal  MontoPagado   { get; set; }
    public decimal  Cambio        => MontoPagado - TotalVenta;
    public string   MetodoPago    { get; set; } = "EFECTIVO";
    public string   NombreCliente { get; set; } = "Cliente General";
    public int      OrdenId       { get; set; }
    
    public bool   EsCotizacion  => MetodoPago == "COTIZACION";
    public string TituloPrincipal => EsCotizacion ? "¡Cotización Guardada!" : "¡Venta Completada!";
    public string TextoTipoDocumento => EsCotizacion ? "Nº Cotización:" : "Nº Recibo:";
    public string TipoComprobante => EsCotizacion ? "COTIZACIÓN" : "RECIBO";
    
    public string TextoPiePagina => EsCotizacion ? "ESTA COTIZACIÓN ES VÁLIDA POR 15 DÍAS." : PiePagina;
    public string TextoBotonCerrar => EsCotizacion ? "Cerrar" : "Nueva Venta";

    // Datos del negocio (vienen de configuracion)
    public string NombreNegocio { get; set; } = string.Empty;
    public string Direccion     { get; set; } = string.Empty;
    public string Telefono      { get; set; } = string.Empty;
    public string Rnc           { get; set; } = string.Empty;
    public string NombreUsuario { get; set; } = string.Empty;
    public string Encabezado    { get; set; } = string.Empty;
    public string PiePagina     { get; set; } = string.Empty;
    public string MonedaSimbolo { get; set; } = "$";

    // Config de impresión (cargada desde BD para usarla al imprimir/exportar)
    public string ImpNombreImpresora { get; set; } = string.Empty;
    public string ImpTipoPapel       { get; set; } = "80mm";
    public int    ImpCopias          { get; set; } = 1;
    public int    ImpMargenArriba    { get; set; }
    public int    ImpMargenAbajo     { get; set; }
    public int    ImpMargenIzquierda { get; set; }
    public int    ImpMargenDerecha   { get; set; }
    public string ImpFuenteFamilia   { get; set; } = string.Empty;
    public int    ImpFuenteTamano    { get; set; } = 100;

    public int CantidadArticulos => CartItems.Count;

    public ObservableCollection<ItemCarrito> CartItems { get; } = new();

    // Barcode generado a partir del número de venta
    public BitmapSource? BarcodeSource => GenerarBarcode(NumeroVenta);

    private static BitmapSource? GenerarBarcode(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        try
        {
            var writer = new BarcodeWriterPixelData
            {
                Format  = BarcodeFormat.CODE_128,
                Options = new EncodingOptions { Height = 60, Width = 280, Margin = 0, PureBarcode = true }
            };
            var pixelData = writer.Write(texto);
            var bmp = new WriteableBitmap(pixelData.Width, pixelData.Height, 96, 96, PixelFormats.Bgr32, null);
            bmp.WritePixels(new Int32Rect(0, 0, pixelData.Width, pixelData.Height),
                            pixelData.Pixels, pixelData.Width * 4, 0);
            return bmp;
        }
        catch { return null; }
    }
}
