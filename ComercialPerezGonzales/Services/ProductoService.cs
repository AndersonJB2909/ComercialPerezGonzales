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
    public Producto? GetById(int id) => _repo.GetById(id);
    public Producto? GetByCodigo(string codigo) => _repo.GetByCodigo(codigo);
    public IEnumerable<Producto> Search(string texto) => _repo.Search(texto);
    public IEnumerable<Producto> GetByCategoria(int categoriaId) => _repo.GetByCategoria(categoriaId);
    public IEnumerable<Producto> GetBajoStock() => _repo.GetBajoStock();
    public IEnumerable<Categoria> GetCategorias() => _catRepo.GetAll();

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

    public void Eliminar(int id) => _repo.Delete(id);
}
