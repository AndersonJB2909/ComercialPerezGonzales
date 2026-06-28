using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class ProveedorService
{
    private readonly ProveedorRepository _repo;

    public ProveedorService(ProveedorRepository repo) => _repo = repo;

    public IEnumerable<Proveedor> GetAll() => _repo.GetAll();
    public Proveedor? GetById(int id) => _repo.GetById(id);
    public IEnumerable<Proveedor> Search(string texto) => _repo.Search(texto);

    public int Guardar(Proveedor p)
    {
        if (string.IsNullOrWhiteSpace(p.Nombre))
            throw new InvalidOperationException("El nombre del proveedor es obligatorio.");

        if (p.Id == 0) return _repo.Insert(p);
        _repo.Update(p);
        return p.Id;
    }

    public void Eliminar(int id)
    {
        // En una etapa futura se debería validar si el proveedor tiene órdenes de compra antes de eliminarlo
        _repo.Delete(id);
    }
}
