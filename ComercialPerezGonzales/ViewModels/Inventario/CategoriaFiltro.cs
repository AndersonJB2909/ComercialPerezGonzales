using System;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Inventario;

public class CategoriaFiltro : ViewModelBase
{
    private bool _isSelected;

    public Categoria Categoria { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnSelectionChanged?.Invoke();
            }
        }
    }

    public Action? OnSelectionChanged { get; set; }

    public CategoriaFiltro(Categoria categoria)
    {
        Categoria = categoria;
    }
}
