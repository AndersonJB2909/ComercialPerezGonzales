using ComercialPerezGonzales.Data.Repositories;
using ComercialPerezGonzales.Models;

namespace ComercialPerezGonzales.Services;

public class ClienteService
{
    private readonly ClienteRepository _repo;

    public ClienteService(ClienteRepository repo) => _repo = repo;

    public IEnumerable<Cliente> GetAll() => _repo.GetAll();
    public Cliente? GetById(int id) => _repo.GetById(id);
    public IEnumerable<Cliente> Search(string texto) => _repo.Search(texto);

    public int Guardar(Cliente c)
    {
        if (string.IsNullOrWhiteSpace(c.Nombre))
            throw new InvalidOperationException("El nombre del cliente es obligatorio.");

        if (c.Id == 0) return _repo.Insert(c);
        _repo.Update(c);
        return c.Id;
    }

    public void Eliminar(int id) => _repo.Delete(id);
}
