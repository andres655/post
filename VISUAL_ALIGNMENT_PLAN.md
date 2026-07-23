# Plan De Alineacion Visual

Este plan define el camino para que la capa Web quede 100% alineada con las pantallas y componentes definidos en Stitch, manteniendo intactas la logica de negocio, navegacion, permisos, rutas y handlers existentes.

## Fuente De Verdad

- Stitch es la referencia visual principal.
- Design system base: `Nexus Retail System`.
- Biblioteca base: `Biblioteca de Componentes Compartidos v2`.
- Pantallas de referencia existentes: `Terminal de Ventas`, `Gestion de Inventario`, `Panel de Control` y `Gestion de Clientes`.
- Si una pantalla no existe en Stitch, primero se crea o actualiza en Stitch y despues se implementa en codigo.

## Objetivo

Consolidar una experiencia visual moderna, consistente con Material Design 3, usando componentes compartidos y reduciendo duplicacion en paginas Razor.

El alcance es visual y de estructura UI. No se debe cambiar:

- Logica de negocio.
- Casos de uso de Application.
- Handlers.
- Rutas.
- Navegacion.
- Roles o autorizacion.
- Contratos de datos.

## Fases

### 1. Inventario Visual

Revisar todas las paginas Razor y mapearlas contra Stitch:

- `Login`
- `Home`
- `POS`
- `Inventory`
- `Products`
- `Categories`
- `Analytics`
- `DayStatus`
- `CashOpen`
- `CashCurrent`
- `CashClose`
- `CashHistory`
- `DailyReport`
- `OperationalAudit`
- `ProfitabilityReport`
- `CancellationHistory`
- `SaleReturns`
- `Expenses`
- `Production`
- `Settings`
- `Users`
- `Error`
- `NotFound`
- Componentes compartidos

Salida esperada:

- Pantalla Razor identificada.
- Ruta.
- Pantalla Stitch equivalente.
- Nivel de alineacion visual.
- Brechas visuales.
- Componentes compartidos recomendados.

### 2. Crear Pantallas Faltantes En Stitch

Antes de tocar codigo, crear o refinar en Stitch las pantallas que no tengan referencia directa.

Prioridad alta:

- `Products`
- `Categories`
- `CashOpen`
- `CashCurrent`
- `CashClose`
- `CashHistory`

Prioridad media:

- `Analytics`
- `DailyReport`
- `OperationalAudit`
- `ProfitabilityReport`
- `CancellationHistory`
- `SaleReturns`
- `Production`
- `Expenses`
- `Settings`
- `DayStatus`

Prioridad baja:

- `Login`
- `Error`
- `NotFound`

### 3. Consolidar Shared Components

Usar estos componentes como base obligatoria para patrones repetidos:

- `PageHeader`
- `AppModal`
- `ConfirmDialog`
- `SaveButton`
- `SuccessAlert`
- `ErrorAlert`
- `LoadingState`
- `EmptyState`
- `TablePager`
- `CashSummary`

Reglas:

- No duplicar headers manuales si `PageHeader` cubre el caso.
- No escribir modales completos en paginas si `AppModal` cubre el caso.
- Usar `ConfirmDialog` para confirmaciones destructivas.
- Usar `LoadingState` para cargas.
- Usar `EmptyState` para tablas o paneles sin datos.
- Usar `SuccessAlert` y `ErrorAlert` para feedback.
- Usar `SaveButton` para acciones principales con estado de guardado.
- Usar `TablePager` para paginacion de tablas.
- Usar `CashSummary` en vistas de caja.

### 4. Reemplazar Patrones Duplicados En Paginas

Actualizar las paginas por lotes pequenos y compilar despues de cada lote.

Lote caja:

- `CashOpen`
- `CashCurrent`
- `CashClose`
- `CashHistory`

Lote catalogo:

- `Products`
- `Categories`
- `Inventory`

Lote dashboard:

- `Home`
- `Analytics`
- `DayStatus`

Lote reportes:

- `DailyReport`
- `OperationalAudit`
- `ProfitabilityReport`
- `CancellationHistory`

Lote operaciones:

- `Production`
- `Expenses`
- `SaleReturns`

Lote soporte:

- `Settings`
- `Users`
- `Login`
- `Error`
- `NotFound`

### 5. Separar CSS Por Responsabilidad

Objetivo:

- `wwwroot/app.css` solo debe contener tokens, reset/base global y estilos realmente compartidos.
- Los estilos especificos de pagina deben moverse a `.razor.css`.
- Los estilos de componentes compartidos deben vivir junto al componente en `Components/Shared/*.razor.css`.

Prioridad:

- Mover estilos de tablas, cards, headers y modales especificos de pantalla.
- Mantener tokens globales de color, tipografia, spacing, elevacion y estados.
- Eliminar bloques globales duplicados cuando exista componente compartido equivalente.

### 6. Validacion Visual Y Tecnica

Despues de cada lote:

- Ejecutar `dotnet build src\SmallBusinessPOS.Web\SmallBusinessPOS.Web.csproj`.
- Ejecutar `git diff --check`.
- Revisar que no se hayan cambiado rutas, roles ni handlers.
- Confirmar que los estados principales sigan presentes: loading, empty, success, error, disabled y modal.
- Comparar visualmente contra la pantalla Stitch correspondiente.

### 7. Criterio De Finalizacion

La alineacion visual se considera completa cuando:

- Cada pagina Razor tiene referencia equivalente en Stitch.
- Cada pagina sigue el sistema visual `Nexus Retail System`.
- Los patrones repetidos usan componentes compartidos.
- `app.css` no concentra estilos especificos de pantallas.
- Las pantallas funcionan en escritorio y movil.
- Los estados de UI son consistentes y accesibles.
- El build pasa sin errores.

## Estado Actual

- Componentes compartidos principales: consolidados.
- Varios patrones duplicados ya fueron reemplazados por componentes compartidos.
- Queda como trabajo recomendado mover mas CSS especifico desde `wwwroot/app.css` hacia `.razor.css` por pagina.
- Queda crear o validar en Stitch las pantallas que no tienen referencia directa.
