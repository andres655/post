# Inventario Visual

Este documento mapea las pantallas Razor del proyecto contra las pantallas disponibles en Stitch para medir alineacion visual. El alcance es solo visual: no contempla cambios de logica, navegacion ni funcionalidad.

## Proyecto Stitch

- Proyecto: `Sistema POS Integral Escalable`
- Project ID: `1304585729831696895`
- Design system: `Nexus Retail System`
- Pantalla de shared components v2: `Biblioteca de Componentes Compartidos v2`
- Screen ID shared v2: `a2d0a68a3fe84206a3637458355b4029`

## Fuente De Verdad Visual

Stitch queda definido como la fuente de verdad visual principal del proyecto. Cualquier cambio visual de una pantalla debe partir primero de una pantalla existente o nueva en Stitch, y despues traducirse a Razor/CSS sin cambiar funcionalidad.

### Referencias Principales

| Referencia Stitch | Uso |
|---|---|
| `Nexus Retail System` | Design system base: color, tipografia, radios, estados, spacing y jerarquia |
| `Biblioteca de Componentes Compartidos v2` | Referencia para componentes compartidos, layout, modales, alertas, loading, empty states, botones y paginacion |
| `Terminal de Ventas` | Referencia principal para POS y pantallas operativas de alta densidad |
| `Gestion de Inventario` | Referencia para catalogo, tablas, filtros, acciones y paneles laterales |
| `Panel de Control` | Referencia para dashboards, KPIs, resumenes y accesos rapidos |
| `Gestion de Clientes` | Referencia para listas administrativas, usuarios/clientes, estados, roles y modales |

### Regla De Trabajo

1. Si una pantalla ya existe en Stitch, se implementa tomando esa pantalla como referencia visual.
2. Si una pantalla no existe en Stitch, primero se crea o actualiza en Stitch.
3. Despues de validar la referencia en Stitch, se aplica en el codigo.
4. En codigo solo se cambian estructura visual, clases y CSS.
5. No se cambia logica de negocio, navegacion, handlers, permisos ni rutas.
6. Los patrones repetidos deben salir de `Components/Shared`.
7. Los estilos especificos deben vivir en `.razor.css`, no en `wwwroot/app.css`.

## Pantallas Existentes En Stitch

| Pantalla Stitch | Equivalente en proyecto | Estado |
|---|---|---|
| `Terminal de Ventas` | `src/SmallBusinessPOS.Web/Components/Pages/Pos/Pos.razor` | Alta alineacion |
| `Gestion de Inventario` | `src/SmallBusinessPOS.Web/Components/Pages/Catalog/Inventory.razor` | Alta alineacion |
| `Panel de Control` | `src/SmallBusinessPOS.Web/Components/Pages/Dashboard/Home.razor` | Media/alta |
| `Gestion de Clientes` | `src/SmallBusinessPOS.Web/Components/Pages/Admin/Users.razor` | Media/alta |
| `Biblioteca de Componentes Compartidos v2` | `src/SmallBusinessPOS.Web/Components/Shared/*` y `src/SmallBusinessPOS.Web/Components/Layout/*` | Alta alineacion |
| `QuickPOS Management Suite` | Referencia general movil | Referencia secundaria |

## Mapeo De Paginas Razor

| Pagina | Ruta | Pantalla Stitch directa | Estado visual |
|---|---:|---|---|
| `Login.razor` | `/login` | No | Media |
| `Home.razor` | `/` | `Panel de Control` | Media/alta |
| `Pos.razor` | `/pos` | `Terminal de Ventas` | Alta |
| `Inventory.razor` | `/inventory` | `Gestion de Inventario` | Alta |
| `Products.razor` | `/products` | No | Media |
| `Categories.razor` | `/categories` | No | Media |
| `Analytics.razor` | `/analytics` | No | Media |
| `DayStatus.razor` | `/day-status` | No | Media |
| `Users.razor` | `/users` | `Gestion de Clientes` + shared v2 | Media/alta |
| `Settings.razor` | `/settings` | No | Media |
| `CashOpen.razor` | `/cash/open` | No | Media |
| `CashCurrent.razor` | `/cash/current` | Shared `CashSummary` | Media/alta |
| `CashClose.razor` | `/cash/close` | Shared `CashSummary` | Media/alta |
| `CashHistory.razor` | `/cash/history` | No | Media |
| `Production.razor` | `/production` | No | Media |
| `Expenses.razor` | `/expenses` | No | Media |
| `SaleReturns.razor` | `/sales/returns` | No | Media |
| `CancellationHistory.razor` | `/sales/cancellations` | No | Media |
| `DailyReport.razor` | `/reports/daily` | No | Media |
| `OperationalAudit.razor` | `/audit` | No | Media |
| `ProfitabilityReport.razor` | `/reports/profitability` | No | Media |
| `Error.razor` | `/Error` | No | Media/alta |
| `NotFound.razor` | `/not-found` | No | Media/alta |

## Shared Components Consolidados

Estos componentes quedan definidos como la base reutilizable para alinear el proyecto con Stitch. Cada componente debe mantener su logica actual, exponer parametros simples y concentrar su estilo en `.razor.css`.

| Componente | Razor | CSS scoped | Estado |
|---|---|---|---|
| `PageHeader` | `src/SmallBusinessPOS.Web/Components/Shared/PageHeader.razor` | `src/SmallBusinessPOS.Web/Components/Shared/PageHeader.razor.css` | Consolidado |
| `AppModal` | `src/SmallBusinessPOS.Web/Components/Shared/AppModal.razor` | `src/SmallBusinessPOS.Web/Components/Shared/AppModal.razor.css` | Consolidado |
| `ConfirmDialog` | `src/SmallBusinessPOS.Web/Components/Shared/ConfirmDialog.razor` | `src/SmallBusinessPOS.Web/Components/Shared/ConfirmDialog.razor.css` | Consolidado |
| `SaveButton` | `src/SmallBusinessPOS.Web/Components/Shared/SaveButton.razor` | `src/SmallBusinessPOS.Web/Components/Shared/SaveButton.razor.css` | Consolidado |
| `SuccessAlert` | `src/SmallBusinessPOS.Web/Components/Shared/SuccessAlert.razor` | `src/SmallBusinessPOS.Web/Components/Shared/SuccessAlert.razor.css` | Consolidado |
| `ErrorAlert` | `src/SmallBusinessPOS.Web/Components/Shared/ErrorAlert.razor` | `src/SmallBusinessPOS.Web/Components/Shared/ErrorAlert.razor.css` | Consolidado |
| `LoadingState` | `src/SmallBusinessPOS.Web/Components/Shared/LoadingState.razor` | `src/SmallBusinessPOS.Web/Components/Shared/LoadingState.razor.css` | Consolidado |
| `EmptyState` | `src/SmallBusinessPOS.Web/Components/Shared/EmptyState.razor` | `src/SmallBusinessPOS.Web/Components/Shared/EmptyState.razor.css` | Consolidado |
| `TablePager` | `src/SmallBusinessPOS.Web/Components/Shared/TablePager.razor` | `src/SmallBusinessPOS.Web/Components/Shared/TablePager.razor.css` | Consolidado |
| `CashSummary` | `src/SmallBusinessPOS.Web/Components/Shared/CashSummary.razor` | `src/SmallBusinessPOS.Web/Components/Shared/CashSummary.razor.css` | Consolidado |

### Reglas De Uso

1. Usar `PageHeader` para encabezados con titulo, subtitulo, icono y acciones.
2. Usar `AppModal` como contenedor base de modales.
3. Usar `ConfirmDialog` para confirmaciones destructivas o importantes.
4. Usar `SaveButton` para acciones primarias con estado `Saving`.
5. Usar `SuccessAlert` y `ErrorAlert` para feedback de resultado.
6. Usar `LoadingState` para cargas de pagina, cards o paneles.
7. Usar `EmptyState` para tablas o paneles sin datos.
8. Usar `TablePager` para paginacion de tablas.
9. Usar `CashSummary` para resumenes de caja actuales o cierres.
10. Evitar duplicar estos patrones dentro de paginas individuales.

## Brecha Principal

El proyecto ya tiene muchas paginas modernizadas, pero Stitch no tiene una pantalla directa para la mayoria. Por eso varias paginas se parecen al sistema visual, pero no estan 100% validadas contra una referencia especifica de Stitch.

## Prioridad Para Llegar A 100%

1. Crear en Stitch las pantallas faltantes por lote.
2. Comparar cada pantalla Stitch contra su `.razor`.
3. Aplicar solo cambios visuales.
4. Mantener logica, navegacion y funcionalidades existentes.
5. Mover estilos especificos a `.razor.css`.
6. Dejar `app.css` solo para tokens, base global y overrides realmente compartidos.
7. Validar con `dotnet build`.

## Pantallas Faltantes Recomendadas En Stitch

| Prioridad | Pantallas |
|---|---|
| Alta | `Products`, `Categories`, `CashOpen`, `CashCurrent`, `CashClose`, `CashHistory` |
| Media | `Analytics`, `DailyReport`, `OperationalAudit`, `ProfitabilityReport`, `CancellationHistory`, `SaleReturns` |
| Media | `Production`, `Expenses`, `Settings`, `DayStatus` |
| Baja | `Login`, `Error`, `NotFound` |

## Criterio De Finalizacion

El proyecto estara 100% alineado visualmente cuando:

- Cada pagina Razor tenga pantalla equivalente en Stitch.
- Cada pagina use el mismo sistema visual `Nexus Retail System`.
- Los patrones repetidos usen componentes `Shared`.
- `app.css` no contenga estilos especificos de pantalla.
- Cada pagina tenga estados consistentes: loading, empty, success, error, disabled y modal.
- El build pase sin errores ni advertencias.
