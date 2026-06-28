using System;
using System.Collections.Generic;
using System.Linq;
using ComercialPerezGonzales.Data;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Data.Repositories;

namespace ComercialPerezGonzales.Services;

public class OrdenCompraService
{
    private readonly OrdenCompraRepository _ordenRepo;
    private readonly ProductoRepository _productoRepo;
    private readonly ProveedorRepository _proveedorRepo;

    public OrdenCompraService(OrdenCompraRepository ordenRepo, ProductoRepository productoRepo, ProveedorRepository proveedorRepo)
    {
        _ordenRepo = ordenRepo;
        _productoRepo = productoRepo;
        _proveedorRepo = proveedorRepo;
    }

    public OrdenCompra GenerarOrdenSugerida(int proveedorId)
    {
        // Obtener productos que pertenecen a este proveedor (simplificado: por ahora obtenemos todos y filtramos los que están por debajo del mínimo, 
        // en una base real podríamos filtrar por la tabla producto_proveedores si está configurado así,
        // por ahora vamos a asumir que cualquier producto con stock <= stock_minimo puede ser pedido, o si ya tienen una relación en DB).
        
        // Como simplificación inicial, buscamos los productos cuyo stock <= stock_minimo
        var productosBajoStock = _productoRepo.GetAll().Where(p => p.Stock <= p.StockMinimo && p.Activo).ToList();
        
        // Aquí podríamos filtrar solo los que tienen proveedorId asignado en producto_proveedores,
        // pero como es una versión inicial, se dejará la lista para que el usuario pueda armar su orden.
        
        var orden = new OrdenCompra
        {
            Numero = _ordenRepo.GetNextNumero(),
            ProveedorId = proveedorId,
            Estado = "BORRADOR",
            FechaEmision = DateTime.Now
        };

        foreach (var p in productosBajoStock)
        {
            // Sugerir comprar el doble del mínimo o la diferencia
            decimal sugerido = (p.StockMinimo * 2) - p.Stock;
            if (sugerido <= 0) sugerido = p.StockMinimo;

            orden.Detalles.Add(new DetalleOrdenCompra
            {
                ProductoId = p.Id,
                ProductoNombre = p.Nombre,
                ProductoCodigo = p.Codigo,
                CantidadSolicitada = sugerido,
                CantidadRecibida = 0,
                CostoUnitario = p.PrecioCosto // Sugerencia basada en último costo
            });
        }

        return orden;
    }

    public void GuardarOrden(OrdenCompra orden)
    {
        if (orden.Id == 0)
        {
            if (string.IsNullOrEmpty(orden.Numero))
                orden.Numero = _ordenRepo.GetNextNumero();
            _ordenRepo.Insert(orden);
        }
        else
        {
            _ordenRepo.Update(orden);
        }
    }

    public IEnumerable<OrdenCompra> ObtenerTodas() => _ordenRepo.GetAll();
    
    public IEnumerable<OrdenCompra> ObtenerPorProveedor(int proveedorId) => _ordenRepo.GetByProveedor(proveedorId);
    
    public OrdenCompra? ObtenerPorId(int id) => _ordenRepo.GetById(id);

    public void ActualizarEstado(int id, string estado) => _ordenRepo.UpdateEstado(id, estado);
}
