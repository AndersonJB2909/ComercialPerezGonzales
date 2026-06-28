using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Printing;
using MahApps.Metro.Controls;
using ComercialPerezGonzales.ViewModels.POS;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ComercialPerezGonzales.Views.POS;

public partial class ReciboWindow : MetroWindow
{
    private readonly ReciboViewModel _vm;

    public ReciboWindow(ReciboViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void ImprimirRecibo_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new PrintDialog();

        // Usar la impresora configurada sin mostrar diálogo de selección
        if (!string.IsNullOrWhiteSpace(_vm.ImpNombreImpresora))
        {
            try
            {
                using var server = new LocalPrintServer();
                dlg.PrintQueue = server.GetPrintQueue(_vm.ImpNombreImpresora);
            }
            catch { /* si no existe, usa la predeterminada */ }
        }

        var (paperW, paperH) = PaperSizeDip(_vm.ImpTipoPapel);
        dlg.PrintTicket.PageMediaSize  = new PageMediaSize(paperW, paperH);
        dlg.PrintTicket.PageOrientation = PageOrientation.Portrait;

        const double mmToDip = 96.0 / 25.4;
        double mL = _vm.ImpMargenIzquierda * mmToDip;
        double mT = _vm.ImpMargenArriba    * mmToDip;
        double mR = _vm.ImpMargenDerecha   * mmToDip;
        double printW = Math.Max(10, paperW - mL - mR);

        var panel = PanelRecibo;
        panel.Measure(new System.Windows.Size(printW, double.PositiveInfinity));
        panel.Arrange(new Rect(new System.Windows.Size(printW, panel.DesiredSize.Height)));

        double scale = _vm.ImpFuenteTamano / 100.0;

        try
        {
            for (int i = 0; i < Math.Max(1, _vm.ImpCopias); i++)
            {
                var dv = new DrawingVisual();
                using (var dc = dv.RenderOpen())
                {
                    dc.PushTransform(new TranslateTransform(mL, mT));
                    if (scale != 1.0) dc.PushTransform(new ScaleTransform(scale, scale));
                    dc.DrawRectangle(
                        new VisualBrush(panel) { Stretch = Stretch.None },
                        null,
                        new Rect(0, 0, printW, panel.DesiredSize.Height));
                    if (scale != 1.0) dc.Pop();
                    dc.Pop();
                }
                dlg.PrintVisual(dv, $"Recibo {_vm.NumeroVenta}");
            }

            AppDialog.Show("¡La factura ha sido impresa correctamente!", "Impresión exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppDialog.Show($"Ocurrió un error al intentar imprimir: {ex.Message}", "Error de impresión", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        panel.InvalidateMeasure();
        panel.InvalidateArrange();
        UpdateLayout();
    }

    private void GuardarPdf_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter   = "PDF files (*.pdf)|*.pdf",
            FileName = $"Recibo_{_vm.NumeroVenta.Replace("/", "_")}_{_vm.FechaVenta:yyyyMMdd_HHmm}.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            GenerarPdf(dlg.FileName);
            AppDialog.Show("PDF guardado correctamente.", "Guardar PDF",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppDialog.Show($"Error al generar PDF: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GenerarPdf(string ruta)
    {
        var sym = _vm.MonedaSimbolo;
        float pageWidthMm = _vm.ImpTipoPapel switch
        {
            "58mm"  => 58f,
            "80mm"  => 80f,
            "A4"    => 210f,
            "Carta" => 215.9f,
            _       => 80f
        };
        float pageHeightMm = _vm.ImpTipoPapel is "A4" ? 297f : _vm.ImpTipoPapel is "Carta" ? 279.4f : 500f;

        Document.Create(c =>
        {
            c.Page(page =>
            {
                page.Size(pageWidthMm, pageHeightMm, Unit.Millimetre);
                page.MarginLeft(_vm.ImpMargenIzquierda, Unit.Millimetre);
                page.MarginRight(_vm.ImpMargenDerecha,  Unit.Millimetre);
                page.MarginTop(_vm.ImpMargenArriba,     Unit.Millimetre);
                page.MarginBottom(_vm.ImpMargenAbajo,   Unit.Millimetre);

                page.Content().Column(col =>
                {
                    col.Spacing(2);

                    // Encabezado personalizado o datos del negocio
                    if (!string.IsNullOrWhiteSpace(_vm.Encabezado))
                    {
                        col.Item().AlignCenter().Text(_vm.Encabezado).FontSize(8).LineHeight(1.3f);
                    }
                    else
                    {
                        col.Item().AlignCenter().Text(_vm.NombreNegocio).Bold().FontSize(10);
                        if (!string.IsNullOrWhiteSpace(_vm.Direccion))
                            col.Item().AlignCenter().Text(_vm.Direccion).FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_vm.Telefono))
                            col.Item().AlignCenter().Text($"TEL. {_vm.Telefono}").FontSize(8);
                        if (!string.IsNullOrWhiteSpace(_vm.Rnc))
                            col.Item().AlignCenter().Text($"RNC. {_vm.Rnc}").FontSize(8);
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    col.Item().Text($"{_vm.TextoTipoDocumento} {_vm.NumeroVenta}").FontSize(8);
                    col.Item().Text($"{_vm.FechaVenta:d/M/yyyy h:mm:ss tt}").FontSize(8);
                    if (!string.IsNullOrWhiteSpace(_vm.NombreUsuario))
                        col.Item().Text($"Usuario: {_vm.NombreUsuario}").FontSize(8);
                    col.Item().Text($"Orden Nº: {_vm.OrdenId}").FontSize(8);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    // Ítems de la venta
                    foreach (var item in _vm.CartItems)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text(item.Nombre).FontSize(8);
                            row.ConstantItem(55).AlignRight().Text($"{sym}{item.Subtotal:#,##0.00}").FontSize(8);
                        });
                        col.Item().Text($"  {item.Cantidad} {item.UnidadMedida} x {sym}{item.PrecioUnit:#,##0.00}").FontSize(7).FontColor("#888888");
                        if (item.Descuento > 0)
                        {
                            col.Item().Text($"  Desc. -{sym}{item.Descuento:#,##0.00}").FontSize(7).FontColor("#EF4444");
                        }
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    if (_vm.HasDiscount)
                    {
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Subtotal").FontSize(8);
                            row.ConstantItem(65).AlignRight().Text($"{sym}{_vm.Subtotal:#,##0.00}").FontSize(8);
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Text("Descuento").FontSize(8);
                            row.ConstantItem(65).AlignRight().Text($"-{sym}{_vm.Descuento:#,##0.00}").FontSize(8);
                        });
                        col.Item().PaddingVertical(1);
                    }

                    // Total
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text("TOTAL").Bold().FontSize(11);
                        row.ConstantItem(65).AlignRight().Text($"{sym}{_vm.TotalVenta:#,##0.00}").Bold().FontSize(11);
                    });

                    col.Item().PaddingVertical(1);

                    col.Item().Text($"Método de pago: {_vm.MetodoPago}").FontSize(8);
                    if (_vm.MontoPagado > 0)
                    {
                        col.Item().Text($"Monto pagado: {sym}{_vm.MontoPagado:#,##0.00}").FontSize(8);
                        if (_vm.Cambio > 0)
                            col.Item().Text($"Cambio: {sym}{_vm.Cambio:#,##0.00}").FontSize(8);
                    }

                    if (!string.IsNullOrWhiteSpace(_vm.PiePagina))
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().AlignCenter().Text(_vm.PiePagina).FontSize(8).LineHeight(1.3f);
                    }
                });
            });
        }).GeneratePdf(ruta);
    }

    // Retorna (ancho, alto) en DIPs (96 dpi). Para tickets térmicos el alto es grande.
    private static (double width, double height) PaperSizeDip(string tipo)
    {
        const double mmToDip = 96.0 / 25.4;
        return tipo switch
        {
            "58mm"  => (58    * mmToDip, 2000),
            "A4"    => (210   * mmToDip, 297   * mmToDip),
            "Carta" => (215.9 * mmToDip, 279.4 * mmToDip),
            _       => (80    * mmToDip, 2000),   // 80mm por defecto
        };
    }

    private void Hecho_Click(object sender, RoutedEventArgs e) => Close();
}
