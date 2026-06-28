using System;
using System.Collections.Generic;
using System.Linq;
using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class DevolucionService
{
    private readonly DevolucionRepository _devolucionRepo;
    private readonly VentaRepository _ventaRepo;
    private readonly CierreCajaService _cierreService;
    private readonly ConfiguracionRepository _configRepo;

    public DevolucionService(DevolucionRepository devolucionRepo, VentaRepository ventaRepo,
        CierreCajaService cierreService, ConfiguracionRepository configRepo)
    {
        _devolucionRepo = devolucionRepo;
        _ventaRepo = ventaRepo;
        _cierreService = cierreService;
        _configRepo = configRepo;
    }

    public bool ValidarPINSupervisor(string pin)
    {
        var pinGuardado = _configRepo.GetValor("supervisor_pin") ?? "1234";
        return pinGuardado == pin;
    }

    public Devolucion ProcesarDevolucion(int ventaId, int cierreCajaId, string motivo, string metodoReembolso,
        string supervisorPin, string cajero, List<(int productoId, double cantidad, string estado)> items)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new ArgumentException("El motivo de la devolución es obligatorio.");

        if (!ValidarPINSupervisor(supervisorPin))
            throw new InvalidOperationException("PIN de supervisor incorrecto.");

        var venta = _ventaRepo.GetById(ventaId)
            ?? throw new InvalidOperationException("Venta no encontrada.");

        // Control de tiempo (ej. 30 días límite para devoluciones)
        if ((DateTime.Now - venta.CreatedAt).TotalDays > 30)
            throw new InvalidOperationException("La factura ha excedido el plazo límite de 30 días para devoluciones.");

        // Obtener devoluciones previas de esta venta
        var previousDevs = _devolucionRepo.GetByVentaId(ventaId).ToList();

        var returnedQuantities = new Dictionary<int, decimal>();
        foreach (var pd in previousDevs)
        {
            foreach (var pdd in pd.Detalles)
            {
                if (!returnedQuantities.ContainsKey(pdd.ProductoId))
                    returnedQuantities[pdd.ProductoId] = 0;
                returnedQuantities[pdd.ProductoId] += pdd.Cantidad;
            }
        }

        var devolucion = new Devolucion
        {
            VentaId = ventaId,
            CierreCajaId = cierreCajaId,
            Motivo = motivo.Trim(),
            MetodoReembolso = metodoReembolso,
            SupervisorAutorizo = "Supervisor", // or get from supervisor_pin configuration metadata if any
            CajeroSolicito = cajero,
            FechaHora = DateTime.Now
        };

        decimal subtotalReturned = 0;

        foreach (var item in items)
        {
            var ventaDetalle = venta.Detalles.FirstOrDefault(d => d.ProductoId == item.productoId)
                ?? throw new InvalidOperationException($"El producto ID {item.productoId} no corresponde a la venta original.");

            decimal prevQty = returnedQuantities.ContainsKey(item.productoId) ? returnedQuantities[item.productoId] : 0;
            decimal totalReturningQty = prevQty + (decimal)item.cantidad;

            if (totalReturningQty > ventaDetalle.Cantidad)
                throw new InvalidOperationException($"Cantidad a devolver excede la cantidad comprada. Comprado: {ventaDetalle.Cantidad}, Ya devuelto: {prevQty}, Solicitado: {item.cantidad}");

            var lineSubtotal = (decimal)item.cantidad * ventaDetalle.PrecioUnit;
            // Proportional item-level discount
            var itemDisc = ventaDetalle.Cantidad > 0 ? (ventaDetalle.Descuento / ventaDetalle.Cantidad) * (decimal)item.cantidad : 0;

            devolucion.Detalles.Add(new DetalleDevolucion
            {
                ProductoId = item.productoId,
                Cantidad = (decimal)item.cantidad,
                PrecioUnit = ventaDetalle.PrecioUnit,
                Subtotal = lineSubtotal - itemDisc,
                EstadoProducto = item.estado, // "STOCK" or "MERMA"
                ProductoNombre = ventaDetalle.ProductoNombre,
                ProductoCodigo = ventaDetalle.ProductoCodigo
            });

            subtotalReturned += (lineSubtotal - itemDisc);
        }

        // Proportional global discount and tax calculation
        decimal ratio = venta.Subtotal > 0 ? subtotalReturned / venta.Subtotal : 0;
        devolucion.MontoSubtotal = subtotalReturned;
        devolucion.MontoDescuento = Math.Round(venta.Descuento * ratio, 2);
        devolucion.MontoImpuesto = Math.Round(venta.Impuesto * ratio, 2);
        devolucion.MontoTotal = devolucion.MontoSubtotal - devolucion.MontoDescuento + devolucion.MontoImpuesto;

        // Reembolso
        if (metodoReembolso == "NOTA_CREDITO")
        {
            var codigoNC = _devolucionRepo.GetNextNotaCreditoCodigo();
            devolucion.NotaCreditoCodigo = codigoNC;

            var nc = new NotaCredito
            {
                Codigo = codigoNC,
                ClienteId = venta.ClienteId,
                MontoInicial = devolucion.MontoTotal,
                MontoDisponible = devolucion.MontoTotal,
                Estado = "ACTIVA",
                FechaEmision = DateTime.Now,
                FechaVencimiento = DateTime.Now.AddDays(30) // valid for 30 days
            };
            _devolucionRepo.InsertNotaCredito(nc);
        }

        // Insert refund into database
        var devId = _devolucionRepo.Insert(devolucion);
        devolucion.Id = devId;

        // Financial adjustments in cash register
        if (metodoReembolso == "EFECTIVO")
        {
            _cierreService.RegistrarMovimiento(cierreCajaId, "SALIDA", $"Reembolso Devolución Venta {venta.Numero}", devolucion.MontoTotal, $"DEV-{devId}");
        }

        return devolucion;
    }

    public NotaCredito? ValidarNotaCredito(string codigo, decimal totalVenta)
    {
        var nc = _devolucionRepo.GetNotaCreditoByCodigo(codigo);
        if (nc == null)
            throw new InvalidOperationException("La nota de crédito no existe.");

        if (nc.Estado != "ACTIVA")
            throw new InvalidOperationException($"La nota de crédito se encuentra en estado: {nc.Estado}.");

        if (nc.FechaVencimiento < DateTime.Now)
        {
            nc.Estado = "VENCIDA";
            _devolucionRepo.UpdateNotaCredito(nc);
            throw new InvalidOperationException("La nota de crédito ha vencido.");
        }

        if (nc.MontoDisponible < totalVenta)
            throw new InvalidOperationException($"Saldo disponible insuficiente. Disponible: {nc.MontoDisponible:C2}, Venta: {totalVenta:C2}");

        return nc;
    }

    public void ConsumirNotaCredito(string codigo, decimal monto)
    {
        var nc = _devolucionRepo.GetNotaCreditoByCodigo(codigo);
        if (nc == null || nc.Estado != "ACTIVA") return;

        nc.MontoDisponible = Math.Max(0, nc.MontoDisponible - monto);
        if (nc.MontoDisponible <= 0)
        {
            nc.Estado = "USADA";
        }
        _devolucionRepo.UpdateNotaCredito(nc);
    }

    public IEnumerable<Devolucion> ObtenerTodas()
    {
        return _devolucionRepo.GetAll();
    }
}
