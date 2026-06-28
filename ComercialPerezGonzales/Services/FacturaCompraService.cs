using System;
using System.Collections.Generic;
using ComercialPerezGonzales.Data;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Services;

public class FacturaCompraService
{
    private readonly FacturaCompraRepository _facturaRepo;
    private readonly OrdenCompraRepository _ordenRepo;
    private readonly ProductoRepository _productoRepo;
    private readonly CierreCajaService _cierreCajaService;
    private readonly DatabaseContext _db;

    public FacturaCompraService(
        FacturaCompraRepository facturaRepo,
        OrdenCompraRepository ordenRepo,
        ProductoRepository productoRepo,
        CierreCajaService cierreCajaService,
        DatabaseContext db)
    {
        _facturaRepo = facturaRepo;
        _ordenRepo = ordenRepo;
        _productoRepo = productoRepo;
        _cierreCajaService = cierreCajaService;
        _db = db;
    }

    public int RecibirMercanciaYGenerarFactura(OrdenCompra orden, string numeroFactura)
    {
        decimal subtotal = orden.Detalles.Sum(d => d.CantidadRecibida * d.CostoUnitario);

        var facturaId = _facturaRepo.Insert(new FacturaCompra
        {
            OrdenCompraId = orden.Id,
            ProveedorId = orden.ProveedorId,
            NumeroFactura = numeroFactura,
            FechaEmision = DateTime.Now,
            Subtotal = subtotal,
            Total = subtotal,
            SaldoPendiente = subtotal,
            Estado = "PENDIENTE"
        });

        bool todoRecibido = true, algoRecibido = false;
        using var conn = _db.CreateConnection();
        conn.Open();

        foreach (var det in orden.Detalles)
        {
            if (det.CantidadRecibida < det.CantidadSolicitada) todoRecibido = false;
            if (det.CantidadRecibida <= 0) continue;
            algoRecibido = true;

            var p = _productoRepo.GetById(det.ProductoId);
            if (p == null) continue;
            p.Stock += det.CantidadRecibida;
            p.PrecioCosto = det.CostoUnitario;
            _productoRepo.Update(p);

            // ponytail: referencia_id NULL para evitar conflicto con índice único
            conn.Execute(@"
                INSERT INTO kardex (producto_id, tipo_movimiento, cantidad, costo_unitario, stock_resultante, referencia_tipo, notas)
                VALUES (@ProductoId, 'ENTRADA_COMPRA', @Cantidad, @Costo, @Stock, 'FACTURA_COMPRA', @Notas)",
                new { ProductoId = p.Id, Cantidad = det.CantidadRecibida, Costo = det.CostoUnitario, Stock = p.Stock, Notas = $"Factura {numeroFactura}" });
        }

        string estado = algoRecibido ? (todoRecibido ? "RECIBIDA_COMPLETA" : "RECIBIDA_PARCIAL") : "ENVIADA";
        _ordenRepo.UpdateEstado(orden.Id, estado);
        _ordenRepo.Update(orden);

        return facturaId;
    }

    public void RegistrarPago(int facturaId, decimal monto, string metodoPago, string referencia, string usuario)
    {
        var factura = _facturaRepo.GetById(facturaId);
        if (factura == null) throw new Exception("Factura no encontrada.");
        
        if (monto <= 0) throw new Exception("El monto debe ser mayor a 0.");
        if (monto > factura.SaldoPendiente) throw new Exception("El monto supera el saldo pendiente.");

        int? cierreId = null;
        
        // Si el pago es en efectivo, descontar de la caja actual si hay una abierta
        if (metodoPago == "EFECTIVO")
        {
            var cierreActual = _cierreCajaService.GetJornadaHoy();
            if (cierreActual != null)
            {
                cierreId = cierreActual.Id;
                _cierreCajaService.RegistrarMovimiento(cierreId.Value, "SALIDA", $"Pago Proveedor Fac: {factura.NumeroFactura}", monto, facturaId.ToString());
            }
        }

        var pago = new PagoProveedor
        {
            FacturaCompraId = facturaId,
            CierreCajaId = cierreId,
            FechaPago = DateTime.Now,
            Monto = monto,
            MetodoPago = metodoPago,
            Referencia = referencia,
            UsuarioNombre = usuario
        };

        _facturaRepo.InsertPago(pago);

        // Actualizar saldo de factura
        factura.SaldoPendiente -= monto;
        factura.Estado = factura.SaldoPendiente <= 0 ? "PAGADA" : "PENDIENTE";
        
        _facturaRepo.UpdateEstadoYSaldo(factura.Id, factura.Estado, factura.SaldoPendiente);
    }

    public IEnumerable<FacturaCompra> ObtenerTodas() => _facturaRepo.GetAll();
    
    public IEnumerable<FacturaCompra> ObtenerPorProveedor(int proveedorId) => _facturaRepo.GetByProveedor(proveedorId);
    
    public IEnumerable<PagoProveedor> ObtenerPagosDeFactura(int facturaId) => _facturaRepo.GetPagosByFactura(facturaId);
}
