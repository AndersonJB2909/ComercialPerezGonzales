# Diseño: Productos Compuestos / Conversión de Unidades en Inventario

**Fecha:** 2026-06-26  
**Estado:** Aprobado

---

## Resumen

El negocio vende productos en múltiples presentaciones (suelta y empacada). El stock debe ser una sola fuente de verdad almacenada siempre en la unidad base. Los productos "paquete" o "caja" derivan su disponibilidad calculada del producto base mediante un factor de conversión.

---

## Base de datos

### Nueva tabla: `producto_conversiones`

```sql
CREATE TABLE producto_conversiones (
    id               INTEGER PRIMARY KEY AUTOINCREMENT,
    producto_id      INTEGER NOT NULL UNIQUE,
    producto_base_id INTEGER NOT NULL,
    factor           REAL    NOT NULL CHECK (factor > 0),
    FOREIGN KEY (producto_id)      REFERENCES productos(id),
    FOREIGN KEY (producto_base_id) REFERENCES productos(id),
    CHECK (producto_id != producto_base_id)
);

CREATE INDEX idx_conv_base ON producto_conversiones(producto_base_id);
```

**Reglas:**
- `producto_id` es el producto derivado (paquete, caja, etc.)
- `producto_base_id` es el producto cuyo campo `stock` es la fuente de verdad
- `factor` es la cantidad de unidades base que contiene 1 unidad del producto derivado
- Un producto base puede tener múltiples derivados
- Un derivado solo tiene un producto base (UNIQUE en `producto_id`)
- Los ciclos están prohibidos (un base no puede ser derivado de otro producto)

### Migración

Se agrega en `DatabaseInitializer.cs` junto a las migraciones existentes. La tabla se crea si no existe (`CREATE TABLE IF NOT EXISTS`).

---

## Modelo de dominio

### Clase `ProductoConversion`

```csharp
public class ProductoConversion
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int ProductoBaseId { get; set; }
    public decimal Factor { get; set; }
    // Propiedades de navegación (cargadas por JOIN)
    public string ProductoNombre { get; set; }
    public string ProductoBaseNombre { get; set; }
}
```

### Extensión de `Producto`

Se agrega al modelo existente:

```csharp
// Nulos si no aplica
public ProductoConversion Conversion { get; set; }

// Stock calculado: stock real si es base; stock_base / factor si es derivado
public decimal StockEfectivo => Conversion != null
    ? Math.Floor(Conversion.StockBase / Conversion.Factor)
    : Stock;
```

---

## Capa de datos: `ProductoConversionRepository`

Métodos:

| Método | Descripción |
|--------|-------------|
| `GetByProductoId(int)` | Retorna la conversión de un producto derivado, o null |
| `GetByProductoBaseId(int)` | Lista todos los derivados de un producto base |
| `Insert(ProductoConversion)` | Crea una nueva conversión |
| `Update(ProductoConversion)` | Actualiza factor |
| `Delete(int productoId)` | Elimina la relación (el producto queda independiente) |
| `ExisteCiclo(int productoId, int productoBaseId)` | Verifica que no se creen ciclos |

---

## Capa de servicio: `ProductoConversionService`

### Lógica de stock al vender

Cuando se descuenta stock de un producto (venta):

```
si producto es DERIVADO:
    descuento_base = cantidad × factor
    si stock_base < descuento_base → ERROR "Stock insuficiente"
    UpdateStock(producto_base_id, -descuento_base)
si producto es BASE:
    si stock < cantidad → ERROR "Stock insuficiente"
    UpdateStock(producto_id, -cantidad)
```

### Lógica de stock al ingresar (compra/ajuste)

```
si producto es DERIVADO:
    ingreso_base = cantidad × factor
    UpdateStock(producto_base_id, +ingreso_base)
si producto es BASE:
    UpdateStock(producto_id, +cantidad)
```

### Consulta de stock efectivo

```
si producto es DERIVADO:
    stock_efectivo = FLOOR(stock_base / factor)
si producto es BASE:
    stock_efectivo = stock
```

### Validaciones al crear/editar una conversión

1. El producto derivado no puede ser ya un producto base de otra conversión
2. No se permiten ciclos (A→B→A)
3. Factor debe ser > 0
4. Ambos productos deben existir y estar activos

---

## Capa de presentación

### Vista de Inventario — Productos (`ProductosView.xaml`)

- La columna "Stock" del DataGrid muestra `StockEfectivo` en lugar de `Stock` crudo
- Los productos derivados muestran un ícono o badge distintivo (ej: color diferente en la fila o un indicador "📦 Derivado de: Agua Mineral × 9")
- En el formulario de edición, se agrega una sección **"Conversión de unidades"**:
  - Toggle: "¿Es una presentación de otro producto?"
  - Si activo: ComboBox para seleccionar el producto base + campo numérico para el factor
  - Si ya tiene conversión: mostrar el producto base y factor con opción de editar o eliminar

### Punto de Venta — Validación de stock

En `VentaService` (o donde se procese la venta), antes de confirmar:

1. Para cada línea del ticket, si el producto es derivado → verificar `stock_base >= cantidad × factor`
2. Si alguna línea falla → mostrar mensaje claro: "Stock insuficiente: solo hay X paquetes disponibles"
3. Al confirmar la venta → descontar del producto base (no del derivado)

### Kardex

Las entradas del Kardex siempre se registran sobre el **producto base**. Si se vende un paquete de 9, el Kardex registra `-9` en Agua Mineral con referencia a la venta y nota: `"Venta vía: Paquete Agua Mineral (×9)"`.

---

## Casos de borde

| Caso | Comportamiento |
|------|---------------|
| Se elimina el producto base | Prohibido si tiene derivados activos. Mostrar error. |
| Se desactiva el producto base | Los derivados quedan con stock 0 efectivo (base inactivo). |
| Stock base = 8, se venden 1 paquete (factor=9) | Bloqueado. Mensaje: "Stock insuficiente (8 unidades, necesita 9)". |
| Ticket con 1 paquete + 3 sueltas con base=11 | Bloqueado. 1×9 + 3×1 = 12 > 11. |
| Ajuste negativo que dejaría base en negativo | Bloqueado con mensaje de validación. |

---

## Archivos a modificar / crear

| Archivo | Acción |
|---------|--------|
| `Data/DatabaseInitializer.cs` | Agregar migración de `producto_conversiones` |
| `Models/ProductoConversion.cs` | Crear modelo |
| `Models/Producto.cs` | Agregar propiedad `Conversion` y `StockEfectivo` |
| `Data/Repositories/ProductoConversionRepository.cs` | Crear repositorio |
| `Services/ProductoConversionService.cs` | Crear servicio con lógica de negocio |
| `Services/ProductoService.cs` | Integrar validación de stock efectivo |
| `Services/VentaService.cs` | Integrar descuento vía producto base |
| `ViewModels/Inventario/ProductosViewModel.cs` | Agregar manejo de conversiones |
| `Views/Inventario/ProductosView.xaml` | Agregar sección de conversión en formulario |
| `App.xaml.cs` o DI setup | Registrar nuevo repositorio y servicio |
