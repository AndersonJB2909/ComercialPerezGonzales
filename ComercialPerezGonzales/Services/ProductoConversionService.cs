using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class ProductoConversionService
{
    private readonly ProductoConversionRepository _convRepo;
    private readonly ProductoRepository _productoRepo;

    public ProductoConversionService(ProductoConversionRepository convRepo, ProductoRepository productoRepo)
    {
        _convRepo = convRepo;
        _productoRepo = productoRepo;
    }

    public ProductoConversion? GetByProductoId(int productoId) =>
        _convRepo.GetByProductoId(productoId);

    public IEnumerable<ProductoConversion> GetDerivadosDeBase(int productoBaseId) =>
        _convRepo.GetByProductoBaseId(productoBaseId);

    public void Guardar(ProductoConversion conv)
    {
        if (conv.Factor <= 0)
            throw new InvalidOperationException("El factor de conversión debe ser mayor a cero.");

        if (conv.ProductoId == conv.ProductoBaseId)
            throw new InvalidOperationException("Un producto no puede ser su propio producto base.");

        var productoBase = _productoRepo.GetById(conv.ProductoBaseId)
            ?? throw new InvalidOperationException("El producto base no existe.");

        if (!productoBase.Activo)
            throw new InvalidOperationException("El producto base está inactivo.");

        // Evitar ciclos: el base no puede ser derivado de este producto
        var convDelBase = _convRepo.GetByProductoId(conv.ProductoBaseId);
        if (convDelBase != null)
            throw new InvalidOperationException(
                $"'{productoBase.Nombre}' ya es derivado de otro producto. No se permiten ciclos.");

        // El derivado no puede ser base de otro producto
        if (_convRepo.ExisteComoBase(conv.ProductoId))
            throw new InvalidOperationException(
                "Este producto ya es base de otros derivados. No puede ser derivado a la vez.");

        var existente = _convRepo.GetByProductoId(conv.ProductoId);
        if (existente == null)
            _convRepo.Insert(conv);
        else
            _convRepo.Update(conv);
    }

    public void Eliminar(int productoId) => _convRepo.Delete(productoId);

    /// <summary>
    /// Valida que haya stock suficiente para descontar la cantidad indicada del producto.
    /// Lanza InvalidOperationException con mensaje claro si no alcanza.
    /// </summary>
    public void ValidarStockSuficiente(int productoId, decimal cantidad)
    {
        var conv = _convRepo.GetByProductoId(productoId);
        if (conv != null)
        {
            // Producto derivado: verificar stock en base
            var necesario = cantidad * conv.Factor;
            if (conv.StockBase < necesario)
            {
                var disponibles = Math.Floor(conv.StockBase / conv.Factor);
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{conv.ProductoNombre}'. " +
                    $"Disponible: {disponibles} (necesita {cantidad}, requiere {necesario} {conv.ProductoBaseNombre}).");
            }
        }
        else
        {
            // Producto base o independiente
            var producto = _productoRepo.GetById(productoId)
                ?? throw new InvalidOperationException($"Producto ID {productoId} no encontrado.");
            if (producto.Stock < cantidad)
                throw new InvalidOperationException(
                    $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}");
        }
    }

    /// <summary>
    /// Descuenta stock del producto. Si es derivado, descuenta del producto base.
    /// Debe llamarse DESPUÉS de ValidarStockSuficiente.
    /// </summary>
    public void DescontarStock(int productoId, decimal cantidad)
    {
        var conv = _convRepo.GetByProductoId(productoId);
        if (conv != null)
            _productoRepo.UpdateStock(conv.ProductoBaseId, -(cantidad * conv.Factor));
        else
            _productoRepo.UpdateStock(productoId, -cantidad);
    }

    /// <summary>
    /// Agrega stock al producto. Si es derivado, agrega al producto base.
    /// </summary>
    public void AgregarStock(int productoId, decimal cantidad)
    {
        var conv = _convRepo.GetByProductoId(productoId);
        if (conv != null)
            _productoRepo.UpdateStock(conv.ProductoBaseId, cantidad * conv.Factor);
        else
            _productoRepo.UpdateStock(productoId, cantidad);
    }
}
