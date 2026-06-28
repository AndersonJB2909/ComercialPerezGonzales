using System;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Inventario;

public class UnidadFiltro : ViewModelBase
{
    private bool _isSelected;

    public UnidadMedida Unidad { get; }

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

    public UnidadFiltro(UnidadMedida unidad)
    {
        Unidad = unidad;
    }
}
