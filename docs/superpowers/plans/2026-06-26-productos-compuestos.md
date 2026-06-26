# Productos Compuestos — Plan de Implementación

> **Para trabajadores agénticos:** SUB-SKILL REQUERIDO: Usa `superpowers:subagent-driven-development` (recomendado) o `superpowers:executing-plans` para ejecutar este plan tarea a tarea. Los pasos usan sintaxis de checkbox (`- [ ]`) para seguimiento.

**Goal:** Agregar soporte de productos compuestos/paquetes en el inventario, donde el stock se almacena en la unidad base y los productos derivados calculan su disponibilidad en tiempo real mediante un factor de conversión.

**Architecture:** Se agrega una tabla `producto_conversiones` que vincula un producto derivado (paquete) con su producto base y un factor numérico. El stock nunca se duplica — solo vive en el producto base. Al vender o ajustar un derivado, el sistema opera sobre el stock del base multiplicando por el factor. VentaService y ProductosViewModel son los dos puntos de integración principales.

**Tech Stack:** .NET 8 WPF, SQLite, Dapper, MahApps.Metro, Microsoft.Extensions.DependencyInjection

## Restricciones globales

- No hay proyecto de tests — se omite TDD. Verificar manualmente compilando con `dotnet build`.
- Dapper usa `DefaultTypeMap.MatchNamesWithUnderscores = true` — las propiedades C# se mapean desde columnas `snake_case` automáticamente.
- Todos los repositorios reciben `DatabaseContext` por constructor.
- El DI se registra en `App.xaml.cs` con `services.AddSingleton<T>()`.
- Los importes en XAML usan `xmlns:iconPacks="http://metro.mahapps.com/winfx/xaml/iconpacks"`.
- Los ViewModels usan `SetProperty` (de `ViewModelBase`) para notificación de cambios.

---

## Mapa de archivos

| Archivo | Acción |
|---------|--------|
| `ComercialPerezGonzales/Data/DatabaseInitializer.cs` | Modificar — agregar migración de tabla |
| `ComercialPerezGonzales/Models/ProductoConversion.cs` | Crear |
| `ComercialPerezGonzales/Models/Producto.cs` | Modificar — agregar `Conversion` y `StockEfectivo` |
| `ComercialPerezGonzales/Data/Repositories/ProductoConversionRepository.cs` | Crear |
| `ComercialPerezGonzales/Services/ProductoConversionService.cs` | Crear |
| `ComercialPerezGonzales/Services/VentaService.cs` | Modificar — validación y descuento vía base |
| `ComercialPerezGonzales/ViewModels/Inventario/ProductosViewModel.cs` | Modificar — manejo de conversiones |
| `ComercialPerezGonzales/Views/Inventario/ProductosView.xaml` | Modificar — sección conversión en formulario y columna stock |
| `ComercialPerezGonzales/App.xaml.cs` | Modificar — registrar repositorio y servicio nuevos |

---

## Tarea 1: Migración de base de datos

**Archivos:**
- Modificar: `ComercialPerezGonzales/Data/DatabaseInitializer.cs`

**Interfaces:**
- Produce: tabla `producto_conversiones` en SQLite con columnas `id`, `producto_id` (UNIQUE), `producto_base_id`, `factor`

- [ ] **Paso 1: Agregar el bloque de migración en `Initialize()`**

Abrir `ComercialPerezGonzales/Data/DatabaseInitializer.cs`. Después del bloque try/catch existente (migración de `imagen_data`), agregar:

```csharp
// Migración: tabla de conversiones de productos
try
{
    using var m2 = conn.CreateCommand();
    m2.CommandText = @"
        CREATE TABLE IF NOT EXISTS producto_conversiones (
            id               INTEGER PRIMARY KEY AUTOINCREMENT,
            producto_id      INTEGER NOT NULL UNIQUE,
            producto_base_id INTEGER NOT NULL,
            factor           REAL    NOT NULL CHECK (factor > 0),
            FOREIGN KEY (producto_id)      REFERENCES productos(id),
            FOREIGN KEY (producto_base_id) REFERENCES productos(id),
            CHECK (producto_id != producto_base_id)
        );
        CREATE INDEX IF NOT EXISTS idx_conv_base ON producto_conversiones(producto_base_id);";
    m2.ExecuteNonQuery();
}
catch { /* tabla ya existe */ }
```

- [ ] **Paso 2: Compilar para verificar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded, 0 errores.

- [ ] **Paso 3: Commit**

```
git add ComercialPerezGonzales/Data/DatabaseInitializer.cs
git commit -m "feat: agregar migración tabla producto_conversiones"
```

---

## Tarea 2: Modelo `ProductoConversion` y extensión de `Producto`

**Archivos:**
- Crear: `ComercialPerezGonzales/Models/ProductoConversion.cs`
- Modificar: `ComercialPerezGonzales/Models/Producto.cs`

**Interfaces:**
- Produce: clase `ProductoConversion` con propiedades `Id`, `ProductoId`, `ProductoBaseId`, `Factor`, `StockBase`, `ProductoNombre`, `ProductoBaseNombre`
- Produce: propiedad `Producto.Conversion` (nullable) y propiedad calculada `Producto.StockEfectivo`

- [ ] **Paso 1: Crear `ProductoConversion.cs`**

```csharp
namespace ComercialPerezGonzales.Models;

public class ProductoConversion
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int ProductoBaseId { get; set; }
    public decimal Factor { get; set; }

    // Cargado por JOIN al consultar
    public decimal StockBase { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public string ProductoBaseNombre { get; set; } = string.Empty;
}
```

- [ ] **Paso 2: Extender `Producto.cs`**

Al final de la clase `Producto`, antes del cierre `}`, agregar:

```csharp
public ProductoConversion? Conversion { get; set; }

public decimal StockEfectivo => Conversion != null
    ? Math.Floor(Conversion.StockBase / Conversion.Factor)
    : Stock;

public bool EsDerivado => Conversion != null;
```

- [ ] **Paso 3: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded, 0 errores.

- [ ] **Paso 4: Commit**

```
git add ComercialPerezGonzales/Models/ProductoConversion.cs ComercialPerezGonzales/Models/Producto.cs
git commit -m "feat: modelo ProductoConversion y propiedad StockEfectivo en Producto"
```

---

## Tarea 3: Repositorio `ProductoConversionRepository`

**Archivos:**
- Crear: `ComercialPerezGonzales/Data/Repositories/ProductoConversionRepository.cs`

**Interfaces:**
- Consume: `DatabaseContext`, `ProductoConversion` (de Tarea 2)
- Produce:
  - `GetByProductoId(int productoId) → ProductoConversion?`
  - `GetByProductoBaseId(int productoBaseId) → IEnumerable<ProductoConversion>`
  - `Insert(ProductoConversion conv) → void`
  - `Update(ProductoConversion conv) → void`
  - `Delete(int productoId) → void`
  - `ExisteComoBase(int productoId) → bool`

- [ ] **Paso 1: Crear el repositorio**

```csharp
using ComercialPerezGonzales.Models;
using Dapper;

namespace ComercialPerezGonzales.Data.Repositories;

public class ProductoConversionRepository
{
    private readonly DatabaseContext _context;

    public ProductoConversionRepository(DatabaseContext context) => _context = context;

    public ProductoConversion? GetByProductoId(int productoId)
    {
        using var conn = _context.CreateConnection();
        return conn.QueryFirstOrDefault<ProductoConversion>(@"
            SELECT pc.*,
                   p.nombre  AS ProductoNombre,
                   pb.nombre AS ProductoBaseNombre,
                   pb.stock  AS StockBase
            FROM producto_conversiones pc
            JOIN productos p  ON p.id  = pc.producto_id
            JOIN productos pb ON pb.id = pc.producto_base_id
            WHERE pc.producto_id = @productoId",
            new { productoId });
    }

    public IEnumerable<ProductoConversion> GetByProductoBaseId(int productoBaseId)
    {
        using var conn = _context.CreateConnection();
        return conn.Query<ProductoConversion>(@"
            SELECT pc.*,
                   p.nombre  AS ProductoNombre,
                   pb.nombre AS ProductoBaseNombre,
                   pb.stock  AS StockBase
            FROM producto_conversiones pc
            JOIN productos p  ON p.id  = pc.producto_id
            JOIN productos pb ON pb.id = pc.producto_base_id
            WHERE pc.producto_base_id = @productoBaseId",
            new { productoBaseId });
    }

    public void Insert(ProductoConversion conv)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            INSERT INTO producto_conversiones (producto_id, producto_base_id, factor)
            VALUES (@ProductoId, @ProductoBaseId, @Factor)",
            conv);
    }

    public void Update(ProductoConversion conv)
    {
        using var conn = _context.CreateConnection();
        conn.Execute(@"
            UPDATE producto_conversiones
            SET producto_base_id = @ProductoBaseId, factor = @Factor
            WHERE producto_id = @ProductoId",
            conv);
    }

    public void Delete(int productoId)
    {
        using var conn = _context.CreateConnection();
        conn.Execute("DELETE FROM producto_conversiones WHERE producto_id = @productoId",
            new { productoId });
    }

    public bool ExisteComoBase(int productoId)
    {
        using var conn = _context.CreateConnection();
        return conn.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM producto_conversiones WHERE producto_base_id = @productoId",
            new { productoId }) > 0;
    }
}
```

- [ ] **Paso 2: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded.

- [ ] **Paso 3: Commit**

```
git add ComercialPerezGonzales/Data/Repositories/ProductoConversionRepository.cs
git commit -m "feat: repositorio ProductoConversionRepository"
```

---

## Tarea 4: Servicio `ProductoConversionService`

**Archivos:**
- Crear: `ComercialPerezGonzales/Services/ProductoConversionService.cs`

**Interfaces:**
- Consume: `ProductoConversionRepository`, `ProductoRepository` (de Tarea 3)
- Produce:
  - `GetByProductoId(int) → ProductoConversion?`
  - `GetDerivadosDeBase(int) → IEnumerable<ProductoConversion>`
  - `Guardar(ProductoConversion) → void` — valida y hace insert o update
  - `Eliminar(int productoId) → void`
  - `DescontarStock(int productoId, decimal cantidad) → void` — opera sobre base si es derivado
  - `AgregarStock(int productoId, decimal cantidad) → void` — ídem
  - `ValidarStockSuficiente(int productoId, decimal cantidad) → void` — lanza excepción si no alcanza

- [ ] **Paso 1: Crear el servicio**

```csharp
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
```

- [ ] **Paso 2: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded.

- [ ] **Paso 3: Commit**

```
git add ComercialPerezGonzales/Services/ProductoConversionService.cs
git commit -m "feat: servicio ProductoConversionService con lógica de stock"
```

---

## Tarea 5: Registrar en DI y actualizar `VentaService`

**Archivos:**
- Modificar: `ComercialPerezGonzales/App.xaml.cs`
- Modificar: `ComercialPerezGonzales/Services/VentaService.cs`

**Interfaces:**
- Consume: `ProductoConversionService` (de Tarea 4)
- `VentaService.ProcesarVenta` ahora usa `ProductoConversionService.ValidarStockSuficiente` y `DescontarStock`

- [ ] **Paso 1: Registrar en `App.xaml.cs`**

En `App.xaml.cs`, después de `services.AddSingleton<ProductoRepository>();` agregar:

```csharp
services.AddSingleton<ProductoConversionRepository>();
```

Después de `services.AddSingleton<ProductoService>();` agregar:

```csharp
services.AddSingleton<ProductoConversionService>();
```

Agregar el using al inicio del archivo:
```csharp
using ComercialPerezGonzales.Services;
```
(ya existe, solo verificar que esté)

- [ ] **Paso 2: Actualizar constructor de `VentaService`**

Reemplazar el constructor y el campo `_productoRepo` en `VentaService.cs`:

```csharp
private readonly VentaRepository _ventaRepo;
private readonly ProductoRepository _productoRepo;
private readonly ConfiguracionRepository _configRepo;
private readonly ProductoConversionService _conversionService;

public VentaService(VentaRepository ventaRepo, ProductoRepository productoRepo,
    ConfiguracionRepository configRepo, ProductoConversionService conversionService)
{
    _ventaRepo = ventaRepo;
    _productoRepo = productoRepo;
    _configRepo = configRepo;
    _conversionService = conversionService;
}
```

- [ ] **Paso 3: Reemplazar el bloque de validación y descuento en `ProcesarVenta`**

Ubicar el `foreach (var item in carrito)` en `ProcesarVenta`. Reemplazar el cuerpo completo del `foreach`:

```csharp
foreach (var item in carrito)
{
    var producto = _productoRepo.GetById(item.ProductoId)
        ?? throw new InvalidOperationException($"Producto ID {item.ProductoId} no encontrado.");

    // Valida stock (maneja derivados y base automáticamente)
    _conversionService.ValidarStockSuficiente(item.ProductoId, item.Cantidad);

    var lineaSubtotal = item.Cantidad * item.PrecioUnit - item.Descuento;
    subtotal += lineaSubtotal;

    detalles.Add(new DetalleVenta
    {
        ProductoId = item.ProductoId,
        ProductoNombre = producto.Nombre,
        Cantidad = item.Cantidad,
        PrecioUnit = item.PrecioUnit,
        Descuento = item.Descuento,
        Subtotal = lineaSubtotal
    });
}
```

Luego, después de `venta.Id = _ventaRepo.Insert(venta);`, agregar el descuento de stock:

```csharp
// Descontar stock (derivados descuentan del base)
foreach (var item in carrito)
    _conversionService.DescontarStock(item.ProductoId, item.Cantidad);
```

- [ ] **Paso 4: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded.

- [ ] **Paso 5: Commit**

```
git add ComercialPerezGonzales/App.xaml.cs ComercialPerezGonzales/Services/VentaService.cs
git commit -m "feat: integrar ProductoConversionService en VentaService y DI"
```

---

## Tarea 6: Actualizar `ProductoRepository` para cargar conversiones

**Archivos:**
- Modificar: `ComercialPerezGonzales/Data/Repositories/ProductoRepository.cs`

**Interfaces:**
- Produce: `GetAll()`, `GetById()`, `GetByCodigo()` y `Search()` retornan productos con `Conversion` cargada si aplica
- Produce: método `TieneDerivados(int id) → bool` para validar eliminación

- [ ] **Paso 1: Agregar `TieneDerivados` al repositorio**

Al final de `ProductoRepository.cs`, antes del cierre `}`, agregar:

```csharp
public bool TieneDerivados(int id)
{
    using var conn = _context.CreateConnection();
    return conn.ExecuteScalar<int>(
        "SELECT COUNT(*) FROM producto_conversiones WHERE producto_base_id = @id",
        new { id }) > 0;
}
```

- [ ] **Paso 2: Agregar método privado de hidratación de conversiones**

Agregar método privado `CargarConversiones` en `ProductoRepository`:

```csharp
private void CargarConversiones(IEnumerable<Producto> productos)
{
    using var conn = _context.CreateConnection();
    var ids = productos.Select(p => p.Id).ToList();
    if (!ids.Any()) return;

    var conversiones = conn.Query<ProductoConversion>(@"
        SELECT pc.*,
               p.nombre  AS ProductoNombre,
               pb.nombre AS ProductoBaseNombre,
               pb.stock  AS StockBase
        FROM producto_conversiones pc
        JOIN productos p  ON p.id  = pc.producto_id
        JOIN productos pb ON pb.id = pc.producto_base_id
        WHERE pc.producto_id IN @ids",
        new { ids }).ToDictionary(c => c.ProductoId);

    foreach (var prod in productos)
    {
        if (conversiones.TryGetValue(prod.Id, out var conv))
            prod.Conversion = conv;
    }
}
```

Agregar `using ComercialPerezGonzales.Models;` si no está (ya debe estar).

- [ ] **Paso 3: Llamar `CargarConversiones` en los métodos de consulta**

En `GetAll()`, después de `return conn.Query<Producto>(...)`, cambiar a:

```csharp
public IEnumerable<Producto> GetAll()
{
    using var conn = _context.CreateConnection();
    var productos = conn.Query<Producto>(@"
        SELECT p.*, c.nombre as CategoriaNombre
        FROM productos p
        LEFT JOIN categorias c ON p.categoria_id = c.id
        WHERE p.activo = 1
        ORDER BY p.nombre",
        new { }).ToList();
    CargarConversiones(productos);
    return productos;
}
```

En `GetById()`:

```csharp
public Producto? GetById(int id)
{
    using var conn = _context.CreateConnection();
    var p = conn.QueryFirstOrDefault<Producto>(
        "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.id = @id",
        new { id });
    if (p != null) CargarConversiones(new[] { p });
    return p;
}
```

En `GetByCodigo()`:

```csharp
public Producto? GetByCodigo(string codigo)
{
    using var conn = _context.CreateConnection();
    var p = conn.QueryFirstOrDefault<Producto>(
        "SELECT p.*, c.nombre as CategoriaNombre FROM productos p LEFT JOIN categorias c ON p.categoria_id = c.id WHERE p.codigo = @codigo AND p.activo = 1",
        new { codigo });
    if (p != null) CargarConversiones(new[] { p });
    return p;
}
```

En `Search()`, cambiar el return para hidratar:

```csharp
var resultado = todos
    .Where(p => TextHelper.ContieneSinAcento(p.Nombre, texto)
             || TextHelper.ContieneSinAcento(p.Codigo, texto)
             || TextHelper.ContieneSinAcento(p.CategoriaNombre ?? "", texto))
    .Take(50).ToList();
CargarConversiones(resultado);
return resultado;
```

- [ ] **Paso 4: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded.

- [ ] **Paso 5: Commit**

```
git add ComercialPerezGonzales/Data/Repositories/ProductoRepository.cs
git commit -m "feat: cargar conversiones en ProductoRepository y agregar TieneDerivados"
```

---

## Tarea 7: Actualizar `ProductoService` para proteger eliminación de base

**Archivos:**
- Modificar: `ComercialPerezGonzales/Services/ProductoService.cs`

**Interfaces:**
- Consume: `ProductoRepository.TieneDerivados(int)`
- `Eliminar(int)` lanza excepción si el producto tiene derivados activos

- [ ] **Paso 1: Proteger `Eliminar` en `ProductoService`**

Reemplazar el método `Eliminar`:

```csharp
public void Eliminar(int id)
{
    if (_repo.TieneDerivados(id))
        throw new InvalidOperationException(
            "No se puede eliminar este producto porque otros productos lo usan como base. Elimina primero los productos derivados.");
    _repo.Delete(id);
}
```

- [ ] **Paso 2: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```

- [ ] **Paso 3: Commit**

```
git add ComercialPerezGonzales/Services/ProductoService.cs
git commit -m "feat: bloquear eliminación de producto base con derivados"
```

---

## Tarea 8: Actualizar `ProductosViewModel` para gestionar conversiones

**Archivos:**
- Modificar: `ComercialPerezGonzales/ViewModels/Inventario/ProductosViewModel.cs`

**Interfaces:**
- Consume: `ProductoConversionService` (de Tarea 4)
- Produce:
  - Propiedad `EsDerivado` (bool, bindable) — refleja si el producto en edición es derivado
  - Propiedad `ConversionEdit` (`ProductoConversion?`, bindable) — conversión en edición
  - Propiedad `ProductosBase` (`ObservableCollection<Producto>`) — productos disponibles como base
  - `GuardarConversionCommand` — guarda la conversión
  - `EliminarConversionCommand` — quita la relación derivado/base

- [ ] **Paso 1: Actualizar constructor y campos**

Reemplazar la clase completa `ProductosViewModel`:

```csharp
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ComercialPerezGonzales.Models;
using ComercialPerezGonzales.Services;
using ComercialPerezGonzales.ViewModels.Base;

namespace ComercialPerezGonzales.ViewModels.Inventario;

public class ProductosViewModel : ViewModelBase
{
    private readonly ProductoService _service;
    private readonly ProductoConversionService _convService;
    private string _searchText = string.Empty;
    private Producto? _selected;
    private bool _modoEdicion;
    private bool _esDerivado;
    private ProductoConversion? _conversionEdit;

    public ObservableCollection<Producto> Productos { get; } = new();
    public ObservableCollection<Categoria> Categorias { get; } = new();
    public ObservableCollection<Producto> ProductosBase { get; } = new();

    public Producto? Selected
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

    public bool EsDerivado
    {
        get => _esDerivado;
        set => SetProperty(ref _esDerivado, value);
    }

    public ProductoConversion? ConversionEdit
    {
        get => _conversionEdit;
        set => SetProperty(ref _conversionEdit, value);
    }

    public Producto? ProductoEdit { get; set; }

    public RelayCommand NuevoCommand { get; }
    public RelayCommand EditarCommand { get; }
    public RelayCommand EliminarCommand { get; }
    public RelayCommand GuardarCommand { get; }
    public RelayCommand CancelarCommand { get; }
    public RelayCommand SeleccionarImagenCommand { get; }
    public RelayCommand QuitarImagenCommand { get; }
    public RelayCommand GuardarConversionCommand { get; }
    public RelayCommand EliminarConversionCommand { get; }

    public ProductosViewModel(ProductoService service, ProductoConversionService convService)
    {
        _service = service;
        _convService = convService;
        NuevoCommand = new RelayCommand(Nuevo);
        EditarCommand = new RelayCommand(Editar, () => Selected != null);
        EliminarCommand = new RelayCommand(Eliminar, () => Selected != null);
        GuardarCommand = new RelayCommand(Guardar);
        CancelarCommand = new RelayCommand(() => ModoEdicion = false);
        SeleccionarImagenCommand = new RelayCommand(SeleccionarImagen);
        QuitarImagenCommand = new RelayCommand(QuitarImagen, () => !string.IsNullOrEmpty(ProductoEdit?.ImagenPath));
        GuardarConversionCommand = new RelayCommand(GuardarConversion);
        EliminarConversionCommand = new RelayCommand(EliminarConversion, () => ConversionEdit?.Id > 0);
        Cargar();
    }

    private void Cargar()
    {
        Productos.Clear();
        var lista = string.IsNullOrWhiteSpace(_searchText) ? _service.GetAll() : _service.Search(_searchText);
        foreach (var p in lista) Productos.Add(p);

        Categorias.Clear();
        foreach (var c in _service.GetCategorias()) Categorias.Add(c);
    }

    private void CargarProductosBase(int? excluirId = null)
    {
        ProductosBase.Clear();
        // Solo productos que NO son derivados pueden ser base
        foreach (var p in _service.GetAll())
        {
            if (excluirId.HasValue && p.Id == excluirId.Value) continue;
            if (!p.EsDerivado) ProductosBase.Add(p);
        }
    }

    private void Buscar() => Cargar();

    private void Nuevo()
    {
        ProductoEdit = new Producto();
        EsDerivado = false;
        ConversionEdit = null;
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Editar()
    {
        if (Selected == null) return;
        ProductoEdit = new Producto
        {
            Id = Selected.Id, Codigo = Selected.Codigo, Nombre = Selected.Nombre,
            Descripcion = Selected.Descripcion, PrecioVenta = Selected.PrecioVenta,
            PrecioCosto = Selected.PrecioCosto, Stock = Selected.Stock,
            StockMinimo = Selected.StockMinimo, CategoriaId = Selected.CategoriaId,
            UnidadMedida = Selected.UnidadMedida, Activo = Selected.Activo
        };

        var conv = Selected.Conversion;
        if (conv != null)
        {
            EsDerivado = true;
            ConversionEdit = new ProductoConversion
            {
                Id = conv.Id,
                ProductoId = conv.ProductoId,
                ProductoBaseId = conv.ProductoBaseId,
                Factor = conv.Factor
            };
        }
        else
        {
            EsDerivado = false;
            ConversionEdit = null;
        }

        CargarProductosBase(ProductoEdit.Id);
        ModoEdicion = true;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Guardar()
    {
        if (ProductoEdit == null) return;
        try
        {
            _service.Guardar(ProductoEdit);
            ModoEdicion = false;
            Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GuardarConversion()
    {
        if (ProductoEdit == null || ProductoEdit.Id == 0)
        {
            MessageBox.Show("Guarda el producto primero antes de configurar la conversión.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        if (!EsDerivado)
        {
            // Si estaba configurado como derivado y ahora no, eliminar la conversión
            _convService.Eliminar(ProductoEdit.Id);
            ConversionEdit = null;
            Cargar();
            return;
        }
        if (ConversionEdit == null || ConversionEdit.ProductoBaseId == 0)
        {
            MessageBox.Show("Selecciona el producto base y el factor de conversión.", "Atención", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        try
        {
            ConversionEdit.ProductoId = ProductoEdit.Id;
            _convService.Guardar(ConversionEdit);
            MessageBox.Show("Conversión guardada correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            Cargar();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void EliminarConversion()
    {
        if (ProductoEdit == null) return;
        var r = MessageBox.Show("¿Quitar la relación de conversión? El producto quedará independiente.", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;
        _convService.Eliminar(ProductoEdit.Id);
        EsDerivado = false;
        ConversionEdit = null;
        Cargar();
    }

    private void SeleccionarImagen()
    {
        if (ProductoEdit == null) return;

        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Seleccionar imagen del producto",
            Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp|Todos los archivos|*.*",
            Multiselect = false
        };
        if (dlg.ShowDialog() != true) return;

        var info = new FileInfo(dlg.FileName);

        if (info.Length >= 1_048_576)
        {
            MessageBox.Show(
                $"La imagen pesa {info.Length / 1024} KB. No se permiten imágenes de 1 MB o más.\nSelecciona una imagen más pequeña.",
                "Imagen demasiado grande", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        byte[] bytes = File.ReadAllBytes(dlg.FileName);

        if (info.Length > 204_800)
        {
            bytes = ComprimirImagen(bytes, 200_000);
            MessageBox.Show(
                $"La imagen fue comprimida automáticamente a {bytes.Length / 1024} KB para optimizar el almacenamiento.",
                "Imagen comprimida", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        ProductoEdit.ImagenData = bytes;
        ProductoEdit.ImagenPath = dlg.FileName;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private static byte[] ComprimirImagen(byte[] original, int maxBytes)
    {
        for (int calidad = 80; calidad >= 30; calidad -= 10)
        {
            var comprimido = CodificarJpeg(original, calidad);
            if (comprimido.Length <= maxBytes)
                return comprimido;
        }

        try
        {
            using var ms = new MemoryStream(original);
            var src = BitmapFrame.Create(ms, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            var scaled = new TransformedBitmap(src, new System.Windows.Media.ScaleTransform(0.5, 0.5));
            var encoder = new JpegBitmapEncoder { QualityLevel = 60 };
            encoder.Frames.Add(BitmapFrame.Create(scaled));
            using var out2 = new MemoryStream();
            encoder.Save(out2);
            return out2.ToArray();
        }
        catch
        {
            return CodificarJpeg(original, 30);
        }
    }

    private static byte[] CodificarJpeg(byte[] original, int calidad)
    {
        using var msIn = new MemoryStream(original);
        var bitmap = BitmapFrame.Create(msIn, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        var encoder = new JpegBitmapEncoder { QualityLevel = calidad };
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var msOut = new MemoryStream();
        encoder.Save(msOut);
        return msOut.ToArray();
    }

    private void QuitarImagen()
    {
        if (ProductoEdit == null) return;
        ProductoEdit.ImagenData = null;
        ProductoEdit.ImagenPath = null;
        OnPropertyChanged(nameof(ProductoEdit));
    }

    private void Eliminar()
    {
        if (Selected == null) return;
        var r = MessageBox.Show($"¿Eliminar '{Selected.Nombre}'?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r == MessageBoxResult.Yes)
        {
            try
            {
                _service.Eliminar(Selected.Id);
                Cargar();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
```

- [ ] **Paso 2: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded.

- [ ] **Paso 3: Commit**

```
git add ComercialPerezGonzales/ViewModels/Inventario/ProductosViewModel.cs
git commit -m "feat: ProductosViewModel con gestión de conversiones"
```

---

## Tarea 9: Actualizar `ProductosView.xaml` — columna stock y sección conversión

**Archivos:**
- Modificar: `ComercialPerezGonzales/Views/Inventario/ProductosView.xaml`

**Interfaces:**
- Consume: `ProductosViewModel.EsDerivado`, `ConversionEdit`, `ProductosBase`, `GuardarConversionCommand`, `EliminarConversionCommand`
- La columna Stock del DataGrid muestra `StockEfectivo`
- Los productos derivados muestran un badge "Derivado de: X × N" en el DataGrid
- El formulario de edición incluye sección "Conversión de unidades"

- [ ] **Paso 1: Actualizar columna Stock en el DataGrid**

Localizar en `ProductosView.xaml`:

```xml
<DataGridTextColumn Header="Stock" Binding="{Binding Stock, StringFormat={}{0:N2}}" Width="80"/>
```

Reemplazar con:

```xml
<DataGridTextColumn Header="Stock" Binding="{Binding StockEfectivo, StringFormat={}{0:N2}}" Width="80"/>
```

- [ ] **Paso 2: Agregar columna "Conversión" en el DataGrid**

Después de la columna "Unidad", agregar:

```xml
<DataGridTemplateColumn Header="Conversión" Width="160">
    <DataGridTemplateColumn.CellTemplate>
        <DataTemplate>
            <TextBlock Foreground="#22C55E" FontSize="13" VerticalAlignment="Center" Margin="12,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding EsDerivado}" Value="True">
                                <Setter Property="Text" Value="{Binding Conversion.ProductoBaseNombre, StringFormat='Base: {0}'}"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </DataTemplate>
    </DataGridTemplateColumn.CellTemplate>
</DataGridTemplateColumn>
```

- [ ] **Paso 3: Agregar sección "Conversión de unidades" en el formulario de edición**

El formulario está en el `ScrollViewer`. Localizar el bloque de imagen del producto (`<!-- Imagen del producto -->`). Justo **antes** de ese bloque, agregar:

```xml
<!-- Conversión de unidades -->
<Border Background="#0D1B2A" CornerRadius="6" Padding="16" Margin="0,20,0,0">
    <StackPanel>
        <TextBlock Text="Conversión de unidades" Foreground="#22C55E"
                   FontSize="16" FontWeight="SemiBold" Margin="0,0,0,12"/>

        <CheckBox Content="Es una presentación (paquete/caja) de otro producto"
                  IsChecked="{Binding DataContext.EsDerivado, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                  Foreground="White" FontSize="15" Margin="0,0,0,12"/>

        <StackPanel Visibility="{Binding DataContext.EsDerivado,
                        RelativeSource={RelativeSource AncestorType=ScrollViewer},
                        Converter={StaticResource BoolToVisibility}}">

            <Grid Margin="0,0,0,12">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*"/>
                    <ColumnDefinition Width="16"/>
                    <ColumnDefinition Width="120"/>
                </Grid.ColumnDefinitions>

                <StackPanel Grid.Column="0">
                    <TextBlock Text="Producto base" Foreground="#888" FontSize="14" Margin="0,0,0,4"/>
                    <ComboBox ItemsSource="{Binding DataContext.ProductosBase, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                              SelectedValuePath="Id"
                              SelectedValue="{Binding DataContext.ConversionEdit.ProductoBaseId, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                              DisplayMemberPath="Nombre"
                              FontSize="15"
                              Background="#152238" Foreground="White"/>
                </StackPanel>

                <StackPanel Grid.Column="2">
                    <TextBlock Text="Factor (uds. base)" Foreground="#888" FontSize="14" Margin="0,0,0,4"/>
                    <TextBox Text="{Binding DataContext.ConversionEdit.Factor, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                             Background="#0D1B2A" Foreground="White"
                             BorderBrush="#166534" Padding="8,6" FontSize="16"/>
                </StackPanel>
            </Grid>

            <StackPanel Orientation="Horizontal">
                <Button Command="{Binding DataContext.GuardarConversionCommand, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                        Background="#166534" Foreground="White" BorderThickness="0"
                        Padding="12,8" Cursor="Hand" FontSize="14" Margin="0,0,8,0">
                    <StackPanel Orientation="Horizontal">
                        <iconPacks:PackIconMaterial Kind="LinkVariant" Width="14" Height="14" VerticalAlignment="Center"/>
                        <TextBlock Text=" Guardar conversión" VerticalAlignment="Center"/>
                    </StackPanel>
                </Button>
                <Button Command="{Binding DataContext.EliminarConversionCommand, RelativeSource={RelativeSource AncestorType=ScrollViewer}}"
                        Background="#7F1D1D" Foreground="White" BorderThickness="0"
                        Padding="12,8" Cursor="Hand" FontSize="14">
                    <StackPanel Orientation="Horizontal">
                        <iconPacks:PackIconMaterial Kind="LinkVariantOff" Width="14" Height="14" VerticalAlignment="Center"/>
                        <TextBlock Text=" Quitar conversión" VerticalAlignment="Center"/>
                    </StackPanel>
                </Button>
            </StackPanel>
        </StackPanel>
    </StackPanel>
</Border>
```

- [ ] **Paso 4: Compilar**

```
dotnet build "ComercialPerezGonzales/ComercialPerezGonzales.csproj"
```
Esperado: Build succeeded, 0 errores.

- [ ] **Paso 5: Commit**

```
git add ComercialPerezGonzales/Views/Inventario/ProductosView.xaml
git commit -m "feat: actualizar ProductosView con columna StockEfectivo y sección conversión"
```

---

## Tarea 10: Verificación manual final

- [ ] **Paso 1: Ejecutar la aplicación**

```
dotnet run --project ComercialPerezGonzales/ComercialPerezGonzales.csproj
```

- [ ] **Paso 2: Verificar flujo completo de conversión**

  1. Ir a Inventario → seleccionar "Agua mineral 500ml" → Editar
  2. Crear un nuevo producto "Paquete Agua Mineral x9" (Nuevo → completar datos → Guardar)
  3. Editar "Paquete Agua Mineral x9" → activar checkbox "Es una presentación..." → seleccionar "Agua mineral 500ml" como base → factor = 9 → Guardar conversión
  4. Verificar que en la lista el paquete muestra `StockEfectivo = FLOOR(50 / 9) = 5`
  5. Verificar que la columna "Conversión" del paquete muestra "Base: Agua mineral 500ml"

- [ ] **Paso 3: Verificar venta con stock insuficiente**

  1. Ir al POS → agregar al carrito "Paquete Agua Mineral x9" con cantidad = 10 (requiere 90 unidades, solo hay 50)
  2. Intentar cobrar → debe aparecer mensaje de stock insuficiente con la cantidad disponible (5 paquetes)

- [ ] **Paso 4: Verificar descuento de stock**

  1. Ir al POS → vender 2 "Paquete Agua Mineral x9" → confirmar venta
  2. Ir a Inventario → "Agua mineral 500ml" debe mostrar `stock = 50 - 18 = 32`
  3. "Paquete Agua Mineral x9" debe mostrar `StockEfectivo = FLOOR(32 / 9) = 3`

- [ ] **Paso 5: Commit final si todo funciona**

```
git add .
git commit -m "feat: funcionalidad completa de productos compuestos con conversión de unidades"
```
