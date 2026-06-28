using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class ProductoService
{
    private readonly ProductoRepository _repo;
    private readonly CategoriaRepository _catRepo;

    public ProductoService(ProductoRepository repo, CategoriaRepository catRepo)
    {
        _repo = repo;
        _catRepo = catRepo;
    }

    public IEnumerable<Producto> GetAll() => _repo.GetAll();
    public IEnumerable<Producto> GetPaged(int page, int pageSize, string searchText, List<int> categoryIds, List<string> units, out int totalCount)
    {
        return _repo.GetPaged(page, pageSize, searchText, categoryIds, units, out totalCount);
    }
    public Producto? GetById(int id) => _repo.GetById(id);
    public Producto? GetByCodigo(string codigo) => _repo.GetByCodigo(codigo);
    public IEnumerable<Producto> Search(string texto) => _repo.Search(texto);
    public IEnumerable<Producto> GetByCategoria(int categoriaId) => _repo.GetByCategoria(categoriaId);
    public IEnumerable<Producto> GetBajoStock() => _repo.GetBajoStock();
    public IEnumerable<Categoria> GetCategorias() => _catRepo.GetAll();

    public int GuardarCategoria(Categoria c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre))
            throw new InvalidOperationException("El nombre de la categoría es obligatorio.");
        
        if (c.Id == 0) return _catRepo.Insert(c);
        _catRepo.Update(c);
        return c.Id;
    }

    public int Guardar(Producto p)
    {
        if (string.IsNullOrWhiteSpace(p.Nombre))
            throw new InvalidOperationException("El nombre del producto es obligatorio.");
        if (string.IsNullOrWhiteSpace(p.Codigo))
            throw new InvalidOperationException("El código del producto es obligatorio.");
        if (p.PrecioVenta < 0)
            throw new InvalidOperationException("El precio de venta no puede ser negativo.");

        if (p.Id == 0) return _repo.Insert(p);
        _repo.Update(p);
        return p.Id;
    }

    public void GuardarImagen(int id, byte[] imagen) => _repo.UpdateImagen(id, imagen);

    public void Eliminar(int id)
    {
        if (_repo.TieneDerivados(id))
            throw new InvalidOperationException(
                "No se puede eliminar este producto porque otros productos lo usan como base. Elimina primero los productos derivados.");
        _repo.Delete(id);
    }

    public IEnumerable<Producto> GetProductosPorVencer(int diasThreshold = 30) => _repo.GetProductosPorVencer(diasThreshold);
}
