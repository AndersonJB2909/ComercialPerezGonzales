using System.Collections.ObjectModel;
using System.Windows;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Clientes;

public class ClientesViewModel : ViewModelBase
{
    private readonly ClienteService _service;
    private string _searchText = string.Empty;
    private Cliente? _selected;
    private bool _modoEdicion;

    public ObservableCollection<Cliente> Clientes { get; } = new();

    public Cliente? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public string SearchText
    {
        get => _searchText;
        set { SetProperty(ref _searchText, value); Buscar(); }
    }

    public bool ModoEdicion
    {
        get => _modoEdicion;
        set => SetProperty(ref _modoEdicion, value);
    }

    public Cliente? ClienteEdit { get; set; }

    public RelayCommand NuevoCommand { get; }
    public RelayCommand EditarCommand { get; }
    public RelayCommand EliminarCommand { get; }
    public RelayCommand GuardarCommand { get; }
    public RelayCommand CancelarCommand { get; }

    public ClientesViewModel(ClienteService service)
    {
        _service = service;
        NuevoCommand = new RelayCommand(Nuevo);
        EditarCommand = new RelayCommand(Editar, () => Selected != null);
        EliminarCommand = new RelayCommand(Eliminar, () => Selected != null);
        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => ModoEdicion = false);
        Cargar();
    }

    private void Cargar()
    {
        Clientes.Clear();
        var lista = string.IsNullOrWhiteSpace(_searchText) ? _service.GetAll() : _service.Search(_searchText);
        foreach (var c in lista) Clientes.Add(c);
    }

    private void Buscar() => Cargar();

    private void Nuevo()
    {
        ClienteEdit = new Cliente();
        ModoEdicion = true;
        OnPropertyChanged(nameof(ClienteEdit));
    }

    private void Editar()
    {
        if (Selected == null) return;
        ClienteEdit = new Cliente
        {
            Id = Selected.Id, Codigo = Selected.Codigo, Nombre = Selected.Nombre,
            Apellido = Selected.Apellido, Documento = Selected.Documento,
            Telefono = Selected.Telefono, Email = Selected.Email, Direccion = Selected.Direccion
        };
        ModoEdicion = true;
        OnPropertyChanged(nameof(ClienteEdit));
    }

    private void Guardar()
    {
        if (ClienteEdit == null) return;
        try
        {
            _service.Guardar(ClienteEdit);
            ModoEdicion = false;
            Cargar();
        }
        catch (Exception ex)
        {
            AppDialog.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Eliminar()
    {
        if (Selected == null) return;
        var r = AppDialog.Show($"¿Eliminar a '{Selected.NombreCompleto}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            _service.Eliminar(Selected.Id);
            Cargar();
        }
    }
}
