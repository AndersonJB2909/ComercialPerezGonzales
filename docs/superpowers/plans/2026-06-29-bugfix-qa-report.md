# Corrección de Bugs — Reporte QA 2026-06-29

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Corregir los 12 bugs + 5 problemas de diseño del reporte QA (excluye DESIGN-003 impuesto, operando en 0%).

**Architecture:** Cambios quirúrgicos en repositorios, servicios y ViewModels. Sin refactoring mayor. Sin nuevas dependencias externas (solo `System.Security.Cryptography` ya incluido en .NET).

**Tech Stack:** C# / WPF / MVVM / SQLite / Dapper

## Global Constraints
- No cambiar firmas públicas de VentaRepository.Insert() ni VentaService.ProcesarVenta() — el caller es PosViewModel
- No modificar la capa XAML (solo .cs)
- No agregar NuGet packages
- Mantener el lock (_ventaLock) en VentaService

---

### Task 1: BUG-001 — Doble descuento de stock

**Files:**
- Modify: `ComercialPerezGonzales/Services/VentaService.cs:108`

**Interfaces:**
- Consumes: `VentaRepository.Insert(venta, descontarStock: bool)`
- La lógica de stock SOLO corre por `_conversionService.DescontarStock`

- [x] Cambiar `venta.Id = _ventaRepo.Insert(venta);` → `venta.Id = _ventaRepo.Insert(venta, descontarStock: false);`
- [x] El foreach de `_conversionService.DescontarStock` permanece (cubre derivados Y no-derivados)
- [x] Verificar que `ProcesarCotizacion` ya pasa `descontarStock: false` → OK, no tocar

---

### Task 2: BUG-002 — Subtotal y Descuento inflados en Recibo

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/POS/PosViewModel.cs:512-513` (venta) y `:600-601` (cotización)

- [x] Venta: `Subtotal = CartItems.Sum(i => i.Cantidad * i.PrecioUnit)` y `Descuento = venta.Descuento + CartItems.Sum(i => i.Descuento)`
- [x] Cotización: mismas fórmulas correctas

---

### Task 3: BUG-003 + DESIGN-006 — GetNextNumero robusto

**Files:**
- Modify: `ComercialPerezGonzales/Data/Repositories/VentaRepository.cs:89-98`
- Modify: `ComercialPerezGonzales/Data/Repositories/DevolucionRepository.cs:170-179`

- [x] Usar `MAX(id)` en lugar de `ORDER BY id DESC LIMIT 1` + string parsing
- [x] Para ventas: `SELECT COALESCE(MAX(id),0) FROM ventas` → `V-{n+1:D6}`
- [x] Para NC: `SELECT COALESCE(MAX(id),0) FROM notas_credito` → `NC-{n+1:D6}`

---

### Task 4: BUG-004 — Kardex MERMA con stock_resultante incorrecto

**Files:**
- Modify: `ComercialPerezGonzales/Data/Repositories/DevolucionRepository.cs:41-57`

- [x] Mover la lectura de currentStock y el INSERT INTO kardex dentro del bloque `if (d.EstadoProducto == "STOCK")`
- [x] Para MERMA: no registrar kardex (no hay movimiento de stock físico)

---

### Task 5: BUG-005 — Anulación no restaura stock de derivados

**Files:**
- Modify: `ComercialPerezGonzales/Data/Repositories/VentaRepository.cs:100-124`

- [x] En el foreach de `Anular()`, hacer JOIN con `producto_conversiones` para saber si el producto es derivado
- [x] Si es derivado: restaurar en el producto BASE (usando factor)
- [x] Si no es derivado: restaurar en el producto mismo (comportamiento actual)

---

### Task 6: BUG-006 — Código muerto en ValidarNotaCredito

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/POS/PagoViewModel.cs:544-555`

- [x] Eliminar el `if (nc.MontoDisponible < TotalVenta)` — nunca se ejecuta pues `ValidarNotaCredito` ya tiró excepción
- [x] Simplificar: si `nc != null` → `_notaCreditoValida = true` directamente

---

### Task 7: BUG-007 — Pago combinado permite excedente

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/POS/PagoViewModel.cs:422-428`

- [x] Cambiar `MontoPagado >= TotalVenta` → `Math.Abs(MontoPagado - TotalVenta) <= 0.01m` en el caso COMBINADO

---

### Task 8: BUG-008 — IncrementarItem sin validar stock

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/POS/PosViewModel.cs` (constructor + IncrementarItem)

- [x] Agregar `ProductoConversionService _conversionService` al constructor de PosViewModel
- [x] En `IncrementarItem()`: llamar `_conversionService.ValidarStockSuficiente(item.ProductoId, item.Cantidad + 1)` antes de incrementar

---

### Task 9: BUG-009 — Búsqueda case-sensitive

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/POS/DevolucionesViewModel.cs:192-197`

- [x] Cambiar `WHERE numero = @searchStr` → `WHERE UPPER(numero) = @searchStr`

---

### Task 10: BUG-010 — Kardex stock histórico

**Files:**
- Modify: `ComercialPerezGonzales/Data/Repositories/CierreCajaRepository.cs:152-189`

- [x] Usar CTE con window function para calcular stock_resultante real en el momento de cada venta
- [x] Fórmula: `stock_actual + total_vendido_hoy - acumulado` donde acumulado = SUM(cantidad) OVER ORDER BY created_at

---

### Task 11: BUG-011 — FechaEmision faltante en OrdenCompra

**Files:**
- Modify: `ComercialPerezGonzales/ViewModels/Proveedores/ProveedoresViewModel.cs:345-351`

- [x] Agregar `FechaEmision = DateTime.Now` al objeto OrdenCompra en GuardarOrden()

---

### Task 12: BUG-012 — Contraseñas en texto plano

**Files:**
- Modify: `ComercialPerezGonzales/Data/DatabaseInitializer.cs:546-547`
- Modify: `ComercialPerezGonzales/ViewModels/Login/LoginViewModel.cs:77-149`
- Modify: `ComercialPerezGonzales/Services/DevolucionService.cs:27`

- [x] Agregar helper estático `HashPassword(string)` usando SHA-256
- [x] DatabaseInitializer: insertar hashes en vez de texto plano; migrar valores existentes
- [x] LoginViewModel: comparar hash del input contra hash guardado
- [x] DevolucionService: comparar hash del PIN

---

### Task 13: DESIGN-001 — Service Locator en VentaService

**Files:**
- Modify: `ComercialPerezGonzales/Services/VentaService.cs`

- [x] Agregar `DevolucionService _devolucionService` al constructor de VentaService
- [x] Eliminar `App.Services.GetService(...)` en ProcesarVenta

---

### Task 14: DESIGN-002 — Migraciones sin logging

**Files:**
- Modify: `ComercialPerezGonzales/Data/DatabaseInitializer.cs`

- [x] Reemplazar `catch { /* error ignorado */ }` por `catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"Migración: {ex.Message}"); }`

---

### Task 15: DESIGN-004 — ProveedoresViewModel como Singleton

**Files:**
- Modify: `ComercialPerezGonzales/App.xaml.cs:69`

- [x] Cambiar `AddTransient<ProveedoresViewModel>()` → `AddSingleton<ProveedoresViewModel>()`

---

### Task 16: DESIGN-005 — Cotizaciones eliminadas permanentemente

**Files:**
- Modify: `ComercialPerezGonzales/Data/DatabaseInitializer.cs:148-155`

- [x] Cambiar DELETE → UPDATE estado a 'ANULADA' con notas='Cotización vencida automáticamente'
