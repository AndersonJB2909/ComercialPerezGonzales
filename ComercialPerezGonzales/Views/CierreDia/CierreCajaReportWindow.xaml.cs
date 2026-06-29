using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Printing;
using MahApps.Metro.Controls;
using ComercialPerezGonzales.ViewModels.CierreDia;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ComercialPerezGonzales.Views.CierreDia;

public partial class CierreCajaReportWindow : MetroWindow
{
    private readonly CierreCajaReportViewModel _vm;

    public CierreCajaReportWindow(CierreCajaReportViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        DataContext = vm;
    }

    private void ImprimirReporte_Click(object sender, RoutedEventArgs e)
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

        var panel = PanelReporte;
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
                dlg.PrintVisual(dv, $"Reporte Cierre Caja #{_vm.Id}");
            }

            AppDialog.Show("¡El reporte de cierre ha sido impreso correctamente!", "Impresión exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
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
            FileName = $"Reporte_Cierre_Caja_{_vm.Id}_{_vm.FechaJornada}.pdf"
        };
        if (dlg.ShowDialog() != true) return;

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;
            GenerarPdf(dlg.FileName);
            AppDialog.Show("Reporte en PDF guardado correctamente.", "Guardar PDF",
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
        float pageHeightMm = _vm.ImpTipoPapel is "A4" ? 297f : _vm.ImpTipoPapel is "Carta" ? 279.4f : 550f;

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
                    col.Spacing(3);

                    // Encabezado del negocio
                    col.Item().AlignCenter().Text(_vm.NombreNegocio).Bold().FontSize(10);
                    if (!string.IsNullOrWhiteSpace(_vm.Direccion))
                        col.Item().AlignCenter().Text(_vm.Direccion).FontSize(8);
                    if (!string.IsNullOrWhiteSpace(_vm.Telefono))
                        col.Item().AlignCenter().Text($"TEL. {_vm.Telefono}").FontSize(8);
                    if (!string.IsNullOrWhiteSpace(_vm.Rnc))
                        col.Item().AlignCenter().Text($"RNC. {_vm.Rnc}").FontSize(8);

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    // Información del Reporte
                    col.Item().AlignCenter().Text("REPORTE DE CIERRE DE CAJA").Bold().FontSize(9);
                    col.Item().Text($"Cierre ID: #{_vm.Id}").FontSize(8);
                    col.Item().Text($"Jornada: {_vm.FechaJornada}").FontSize(8);
                    col.Item().Text($"Estado: {_vm.Estado}").FontSize(8).Bold();
                    col.Item().Text($"Apertura: {_vm.FechaApertura:dd/MM/yyyy h:mm tt} ({_vm.UsuarioApertura})").FontSize(8);
                    if (_vm.FechaCierre.HasValue)
                    {
                        col.Item().Text($"Cierre: {_vm.FechaCierre.Value:dd/MM/yyyy h:mm tt} ({_vm.UsuarioCierre})").FontSize(8);
                    }

                    col.Item().PaddingVertical(2).LineHorizontal(0.5f);

                    // Resumen Financiero
                    col.Item().Text("RESUMEN GENERAL").Bold().FontSize(8);
                    col.Item().Row(r => { r.RelativeItem().Text("Fondo Inicial").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.FondoInicial:#,##0.00}").FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("(+) Ventas Efectivo").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.TotalEfectivo:#,##0.00}").FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("(+) Ventas Tarjeta").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.TotalTarjetas:#,##0.00}").FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("(+) Ventas Transferencia").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.TotalTransferencias:#,##0.00}").FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("(+) Entradas Extra").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.EntradasExtra:#,##0.00}").FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("(-) Salidas Efectivo").FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.SalidasEfectivo:#,##0.00}").FontSize(8); });
                    
                    col.Item().PaddingVertical(1).LineHorizontal(0.3f);

                    col.Item().Row(r => { r.RelativeItem().Text("Efectivo Esperado").Bold().FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.EfectivoEsperado:#,##0.00}").Bold().FontSize(8); });
                    col.Item().Row(r => { r.RelativeItem().Text("Efectivo Contado").Bold().FontSize(8); r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.EfectivoReal:#,##0.00}").Bold().FontSize(8); });
                    
                    col.Item().PaddingVertical(1).LineHorizontal(0.3f);

                    col.Item().Row(r => { 
                        r.RelativeItem().Text($"DIFERENCIA ({_vm.EstadoConciliacion})").Bold().FontSize(9); 
                        r.ConstantItem(60).AlignRight().Text($"{sym}{_vm.Diferencia:#,##0.00}").Bold().FontSize(9); 
                    });

                    col.Item().Text($"Ventas Realizadas: {_vm.CantidadVentas}").FontSize(8);

                    if (!string.IsNullOrWhiteSpace(_vm.Observaciones))
                    {
                        col.Item().PaddingVertical(2);
                        col.Item().Text($"Observaciones:").Bold().FontSize(8);
                        col.Item().Text(_vm.Observaciones).FontSize(8).Italic();
                    }

                    // Movimientos de Caja
                    if (_vm.Movimientos != null && _vm.Movimientos.Count > 0)
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().Text("MOVIMIENTOS DE CAJA").Bold().FontSize(8);
                        foreach (var m in _vm.Movimientos)
                        {
                            col.Item().Row(r => {
                                r.RelativeItem().Text($"[{m.Tipo}] {m.Concepto}").FontSize(7.5f);
                                r.ConstantItem(60).AlignRight().Text($"{sym}{m.Monto:#,##0.00}").FontSize(7.5f);
                            });
                            if (!string.IsNullOrWhiteSpace(m.Referencia))
                            {
                                col.Item().Text($"  Ref: {m.Referencia}").FontSize(7f).FontColor("#666666");
                            }
                        }
                    }

                    // Alertas de Stock
                    if (_vm.AlertasStock != null && _vm.AlertasStock.Count > 0)
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().Text("ALERTAS DE STOCK BAJO").Bold().FontSize(8);
                        foreach (var a in _vm.AlertasStock)
                        {
                            col.Item().Row(r => {
                                r.RelativeItem().Text(a.Nombre).FontSize(7.5f);
                                r.ConstantItem(60).AlignRight().Text($"Stock: {a.Stock:0.##} / Mín: {a.StockMinimo:0.##}").FontSize(7.5f).FontColor("#FF0000");
                            });
                        }
                    }

                    // Top Productos
                    if (_vm.TopProductos != null && _vm.TopProductos.Count > 0)
                    {
                        col.Item().PaddingVertical(2).LineHorizontal(0.5f);
                        col.Item().Text("TOP PRODUCTOS VENDIDOS").Bold().FontSize(8);
                        foreach (var t in _vm.TopProductos)
                        {
                            col.Item().Row(r => {
                                r.RelativeItem().Text(t.Nombre).FontSize(7.5f);
                                r.ConstantItem(60).AlignRight().Text($"{t.CantidadVendida:0.##} und ({sym}{t.TotalVendido:#,##0.00})").FontSize(7.5f);
                            });
                        }
                    }

                    col.Item().PaddingVertical(3).LineHorizontal(0.5f);
                    col.Item().AlignCenter().Text("FIN DEL REPORTE").FontSize(8).Bold();
                });
            });
        }).GeneratePdf(ruta);
    }

    private static (double width, double height) PaperSizeDip(string tipo)
    {
        const double mmToDip = 96.0 / 25.4;
        return tipo switch
        {
            "58mm"  => (58    * mmToDip, 3000),
            "A4"    => (210   * mmToDip, 297   * mmToDip),
            "Carta" => (215.9 * mmToDip, 279.4 * mmToDip),
            _       => (80    * mmToDip, 3000),   // 80mm por defecto
        };
    }

    private void Hecho_Click(object sender, RoutedEventArgs e) => Close();
}
