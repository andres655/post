# CajaPyme: vision del proyecto

## Intencion

CajaPyme nace para convertir una operacion diaria de venta, caja, inventario y reportes en una herramienta simple, confiable y entendible para pequenos negocios.

La intencion no es construir solo una pantalla de cobro. El objetivo es crear un sistema operativo comercial para negocios que necesitan vender rapido, controlar efectivo, conocer su inventario, registrar gastos y tomar mejores decisiones sin depender de hojas de calculo ni procesos manuales.

La primera version esta inspirada en un negocio de pollo horneado, pero la arquitectura permite evolucionar hacia cafeterias, restaurantes, colmados, ferreterias, tiendas de conveniencia y otros comercios de alto movimiento.

## Mision

Ayudar a pequenos negocios a vender, controlar y crecer con una plataforma POS accesible, moderna y preparada para operaciones reales.

CajaPyme debe permitir que un negocio:

- Venda con rapidez y menos errores.
- Controle caja, pagos, gastos y retiros.
- Mantenga inventario actualizado.
- Registre produccion, mermas y consumo de insumos.
- Consulte reportes utiles para decisiones diarias.
- Tenga trazabilidad por usuario y rol.
- Pueda crecer desde una caja local hasta multiples sucursales.

## Vision de producto

CajaPyme busca posicionarse como un POS practico para negocios pequenos y medianos que necesitan orden, no complejidad.

El producto debe sentirse como una herramienta de trabajo: rapido, claro, resistente al error y facil de aprender. El usuario principal no necesariamente es tecnico; puede ser cajero, supervisor, dueno, administrador o personal operativo.

La experiencia ideal es que una persona pueda abrir caja, vender, imprimir ticket, registrar gastos, consultar cierre e inventario sin entrenamiento largo.

## Propuesta de valor

CajaPyme ofrece control operativo con una experiencia sencilla:

- POS con ventas rapidas, pagos mixtos, credito y devoluciones.
- Caja con apertura, cierre, retiros y auditoria.
- Catalogo de productos, categorias, tipos de producto y codigos de barra.
- Inventario con ajustes, minimos, alertas y movimientos.
- Produccion con recetas, insumos, mermas y costeo.
- Reportes diarios, rentabilidad, anulaciones y auditoria operacional.
- Seguridad por roles.
- Base preparada para crecer hacia sucursales, cajas multiples y sincronizacion.

## Enfoque de marketing

### Cliente ideal

Negocios pequenos que ya venden todos los dias, pero tienen problemas para controlar caja, inventario o rentabilidad.

Ejemplos:

- Restaurantes pequenos.
- Negocios de pollo horneado o comida preparada.
- Cafeterias.
- Colmados.
- Mini markets.
- Ferreterias pequenas.
- Tiendas familiares.

### Mensaje principal

"CajaPyme te ayuda a vender rapido y controlar tu negocio sin complicarte."

### Diferenciadores

- Pensado para negocios reales, no solo para demos.
- Flujo de caja e inventario integrado.
- Reportes accionables para el dueno.
- Interfaz clara para cajeros.
- Arquitectura limpia para evolucionar sin rehacer todo.
- Preparado para local, LAN o despliegue remoto.

### Tono de marca

Claro, confiable y cercano. El lenguaje debe evitar tecnicismos innecesarios frente al cliente final.

Ejemplos de tono:

- "Vende mas rapido."
- "Controla cada movimiento de caja."
- "Conoce tu inventario antes de quedarte sin producto."
- "Reportes simples para decisiones reales."

## Arquitectura

El proyecto sigue Clean Architecture con vertical slices en Application.

La separacion principal es:

- `Domain`: entidades, enums e invariantes del negocio.
- `Application`: casos de uso, commands, queries, handlers, validators y DTOs.
- `Infrastructure`: EF Core, SQL Server, Identity, migraciones y seed.
- `Web`: Blazor, componentes, paginas, endpoints, servicios de presentacion y configuracion.
- `tests`: pruebas unitarias, de application e integracion.

## Principios arquitectonicos

### Domain no depende de nadie

El dominio debe mantenerse limpio. No debe conocer EF Core, Blazor, Identity, archivos, HTTP ni detalles de UI.

### Application contiene los casos de uso

Las reglas importantes deben vivir en Application:

- Calculo de venta.
- Calculo de impuestos.
- Validacion de pagos.
- Reglas de credito.
- Reglas de devolucion.
- Seleccion de caja/sucursal.
- Settings del negocio.
- Reglas de inventario.

La UI no debe decidir montos finales ni confiar en valores enviados por el navegador.

### Infrastructure implementa detalles externos

Aqui viven los detalles tecnicos:

- `AppDbContext`.
- Configuraciones EF Core.
- Migrations.
- Identity.
- Seed.
- Servicios de almacenamiento.
- Implementaciones de interfaces como `IClock`, `IFileStorageService` o `ICurrentUserService`.

### Web debe ser presentacion y orquestacion ligera

Blazor debe encargarse de:

- Mostrar datos.
- Capturar interacciones.
- Llamar handlers de Application.
- Mostrar errores y estados.
- Navegar entre pantallas.

No debe concentrar reglas de negocio importantes.

## Estado actual de la arquitectura Web

Se comenzo a limpiar el Web layer con componentes compartidos:

- `TablePager`
- `PageHeader`
- `AppModal`
- `ConfirmDialog`
- `SaveButton`
- `LoadingState`
- `EmptyState`
- `SuccessAlert`
- `ErrorAlert`
- `CashSummary`

Tambien se separaron paginas grandes hacia code-behind parcial:

- `Products.razor` + `Products.razor.cs`
- `Pos.razor` + `Pos.razor.cs`
- `Users.razor` + `Users.razor.cs`
- `Inventory.razor` + `Inventory.razor.cs`

Esto reduce ruido visual en los `.razor`, pero todavia hay oportunidad de extraer servicios de pagina o view models cuando la logica local siga creciendo.

## Diseno UX/UI

La direccion visual busca una experiencia moderna, limpia y profesional, tomando Material Design 3 como referencia.

### Principios de diseno

- Claridad antes que decoracion.
- Acciones principales visibles y consistentes.
- Tablas legibles y paginadas.
- Estados vacios utiles.
- Mensajes de error claros.
- Botones con contraste suficiente.
- Modales consistentes.
- Interfaz usable en escritorio y mobile.
- Minima carga cognitiva para cajeros y supervisores.

### Lenguaje visual

El sistema visual se apoya en:

- Verde como color de accion positiva.
- Rojo para errores, anulaciones o acciones destructivas.
- Amarillo/ambar para alertas y advertencias.
- Fondos claros y bordes sutiles.
- Tipografia limpia, escaneable y profesional.
- Componentes repetibles para mantener consistencia.

### Componentes recomendados como base

Los componentes compartidos deben seguir creciendo:

- `DataTableShell` para tablas completas.
- `FilterCard` para filtros por fecha, busqueda y selectores.
- `StatusChip` para estados activo/inactivo/pagado/anulado.
- `IconActionButton` para acciones de tabla.
- `FormField` o helpers de formularios si la repeticion aumenta.

## Plan de continuidad

### Fase 1: Estabilizar POS basico

Objetivo: que el sistema sea usable en una operacion diaria real.

Prioridades:

- Flujo completo de escaner/codigo de barras.
- Seleccion real de sucursal y caja.
- Clientes basicos.
- Ventas a credito.
- Devoluciones parciales.
- Referencias obligatorias para pagos no efectivos.
- Validaciones del servidor para totales, impuestos y pagos.
- Pruebas de Application para los flujos principales.

### Fase 2: Fortalecer arquitectura

Objetivo: reducir deuda tecnica y evitar que la UI concentre reglas.

Prioridades:

- Mover calculos y reglas restantes desde `.razor.cs` hacia Application.
- Crear o consolidar interfaces de infraestructura:
  - `IClock`
  - `IFileStorageService`
  - `ICurrentUserService`
- Revisar que Domain no dependa de infraestructura.
- Mantener handlers pequenos y orientados a caso de uso.
- Separar servicios de pagina cuando haya demasiada orquestacion local.

### Fase 3: Mejorar experiencia Web

Objetivo: que el producto se sienta coherente, rapido y profesional.

Prioridades:

- Completar reutilizacion de componentes compartidos.
- Crear `DataTableShell`.
- Crear `FilterCard`.
- Normalizar chips, botones de accion y estados.
- Reducir CSS especifico por pagina.
- Mantener paginacion de 10 items donde aplique.
- Revisar mobile de pantallas operativas.

### Fase 4: Reportes y gestion

Objetivo: dar valor al dueno y al supervisor.

Prioridades:

- Dashboard gerencial mas accionable.
- Comparativas por fecha.
- Margen por producto.
- Productos mas vendidos.
- Alertas de stock y reposicion.
- Resumen de caja por usuario.
- Exportaciones consistentes.

### Fase 5: Escalabilidad operativa

Objetivo: preparar el sistema para varios puntos de venta.

Prioridades:

- Multi-sucursal robusto.
- Multiples cajas por sucursal.
- Permisos por rol mas finos.
- Auditoria ampliada.
- Sincronizacion/offline si el negocio lo requiere.
- Backups y recuperacion documentados.
- Estrategia de despliegue en produccion.

## Riesgos actuales

- Las paginas grandes aun pueden acumular demasiada orquestacion local.
- El CSS global sigue siendo grande y necesita evolucionar a componentes o archivos por seccion.
- Cualquier valor calculado en UI debe tratarse como no confiable.
- La experiencia mobile debe probarse en flujos reales, no solo en tablas.
- Las reglas de credito, devoluciones y pagos mixtos requieren pruebas fuertes.

## Criterios de calidad

Antes de considerar una funcionalidad lista:

- Compila sin errores.
- Tiene validaciones en Application.
- No depende de calculos enviados por UI.
- Respeta roles y permisos.
- Tiene mensajes de error comprensibles.
- Tiene pruebas cuando toca reglas de negocio.
- Mantiene consistencia visual con componentes compartidos.
- No rompe el flujo de caja, inventario o auditoria.

## Norte del producto

CajaPyme debe crecer como una herramienta que da tranquilidad.

El cajero debe sentir rapidez.
El supervisor debe sentir control.
El dueno debe sentir claridad.
El equipo tecnico debe poder evolucionarlo sin miedo.

Ese es el norte: un POS simple por fuera, solido por dentro y preparado para crecer con el negocio.
