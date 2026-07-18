# SmallBusinessPOS

Sistema de punto de venta para pequeños negocios. Primera versión diseñada para un negocio de pollo horneado, pero con arquitectura genérica para cafeterías, restaurantes, colmados, ferreterías y otros.

## Tecnologías

| Capa | Tecnología |
|---|---|
| Runtime | .NET 10 |
| Backend | ASP.NET Core |
| UI | Blazor Web App (Interactive Server) |
| ORM | Entity Framework Core 10 |
| Base de datos | SQL Server (LocalDB en desarrollo) |
| Autenticación | ASP.NET Core Identity |
| Validación | FluentValidation |
| Logging | Serilog |
| UI CSS | Bootstrap 5 |
| Pruebas | xUnit + FluentAssertions |

## Arquitectura

Clean Architecture con vertical slices en Application:

```
SmallBusinessPOS.sln
├── src
│   ├── SmallBusinessPOS.Domain          ← Entidades, enums, lógica de dominio pura
│   ├── SmallBusinessPOS.Application     ← Casos de uso, handlers, validators, DTOs
│   ├── SmallBusinessPOS.Infrastructure  ← EF Core, Identity, Migrations, Seed
│   └── SmallBusinessPOS.Web            ← Blazor, Program.cs, páginas
└── tests
    ├── SmallBusinessPOS.Domain.Tests
    ├── SmallBusinessPOS.Application.Tests
    └── SmallBusinessPOS.IntegrationTests
```

### Dependencias entre capas

```
Domain  →  (ninguna)
Application  →  Domain
Infrastructure  →  Application
Web  →  Application + Infrastructure
```

## Requisitos previos

- .NET 10 SDK
- SQL Server Express o LocalDB (incluido con Visual Studio)
- Visual Studio 2022 o VS Code con extensión C#

### Verificar LocalDB disponible

```powershell
sqllocaldb info
sqllocaldb start MSSQLLocalDB
```

El servidor local de base de datos para desarrollo es `(localdb)\MSSQLLocalDB`.

## Configuración inicial

### 1. Clonar y restaurar

```bash
git clone <url>
cd SmallBusinessPOS
dotnet restore
```

### 2. Configurar contraseñas de desarrollo

Las contraseñas de seed se leen de `appsettings.Development.json`. **No incluyas contraseñas reales en el repositorio.**

Para desarrollo local, el archivo incluye contraseñas de ejemplo. Para cambiarlas usa [User Secrets](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets):

```bash
cd src/SmallBusinessPOS.Web
dotnet user-secrets init
dotnet user-secrets set "SeedData:AdminPassword" "TuPasswordAdmin123!"
dotnet user-secrets set "SeedData:SupervisorPassword" "TuPasswordSupervisor123!"
dotnet user-secrets set "SeedData:CashierPassword" "TuPasswordCajero123!"
```

### 3. Ejecutar

```bash
cd src/SmallBusinessPOS.Web
dotnet run
```

La aplicación:
1. Aplica automáticamente las migraciones en desarrollo
2. Crea el seed con el negocio demo "Pollo Sabroso"
3. Crea los usuarios de prueba (ver credenciales abajo)

Acceder en: `https://localhost:5001`

### Usuarios de desarrollo

| Rol | Email | Contraseña (dev por defecto) |
|---|---|---|
| Administrador | admin@pollosaboroso.local | `Admin123!` |
| Supervisor | supervisor@pollosaboroso.local | `Supervisor123!` |
| Cajero | cajero@pollosaboroso.local | `Cajero123!` |

> ⚠️ Cambiar siempre las contraseñas en entornos de producción o staging.

## Ejecutar pruebas

```bash
dotnet test
```

### Resultado esperado

```
SmallBusinessPOS.Domain.Tests        29 pruebas ✓
SmallBusinessPOS.Application.Tests   64 pruebas ✓
SmallBusinessPOS.IntegrationTests     3 pruebas ✓
```

## Migraciones EF Core

### Crear nueva migración

```bash
dotnet ef migrations add NombreMigracion \
  --project src/SmallBusinessPOS.Infrastructure \
  --startup-project src/SmallBusinessPOS.Infrastructure \
  --output-dir Data/Migrations
```

### Aplicar migraciones manualmente

```bash
dotnet ef database update \
  --project src/SmallBusinessPOS.Infrastructure \
  --startup-project src/SmallBusinessPOS.Infrastructure
```

## Respaldo local (SQL Server Express)

Para crear un respaldo de la base de datos en SQL Server Express:

```sql
-- Ejecutar en SQL Server Management Studio o sqlcmd
BACKUP DATABASE SmallBusinessPOSDb
TO DISK = 'C:\Backups\SmallBusinessPOS_20260716.bak'
WITH FORMAT, INIT, COMPRESSION;
```

También puede configurarse como tarea programada de Windows.

## Estructura de Application (vertical slices)

```
Application
└── Features
    ├── Categories
    │   ├── CreateCategory   (handler + command + validator)
    │   ├── UpdateCategory
    │   ├── GetCategories
    │   ├── GetCategory
    │   └── DisableCategory
    └── Products
        ├── CreateProduct    (handler + command + validator)
        ├── UpdateProduct
        ├── GetProducts
        ├── GetProduct
        └── DisableProduct
```

## Decisiones técnicas

| Decisión | Justificación |
|---|---|
| Guid v7 | IDs ordenables por tiempo — mejor rendimiento en índices clustered de SQL Server |
| Sin MediatR | Handlers directos son más simples; MediatR se puede agregar si crece la complejidad |
| DbContext como Unit of Work | Elimina repositorios genéricos innecesarios, EF Core maneja el UoW nativo |
| Result<T> pattern | Errores esperados como valores, no excepciones; más claro en flujos de negocio |
| UTC siempre | Evita bugs de zona horaria; la zona del negocio se usa solo para presentación |
| Private setters en entidades | Encapsulan invariantes del dominio; EF Core los maneja vía reflection |
| Identity en Infrastructure | Domain no depende de Identity; ApplicationUser queda aislado |
| Blazor Interactive Server | Apropiado para uso local / LAN — no requiere WebAssembly ni API separada |

## Estado actual del incremento de caja y ventas

- [x] Entidades de Caja: `CashRegister`, `CashSession`, `CashMovement`
- [x] Apertura, sesión actual y cierre de caja
- [x] Pantallas `/cash/current` y `/cash/close` con resumen de caja
- [x] Módulo de Ventas: `Sale`, `SaleDetail`, `SalePayment`
- [x] Punto de Venta (UI `/pos`)
- [x] Descuento de inventario en venta
- [x] Numeración offline de ventas
- [x] Anulación de ventas con reversión de inventario y caja
- [x] Outbox para preparar sincronización futura
- [x] Ticket PDF y reimpresión auditada
- [x] Historial de anulaciones para Supervisor / Administrator
- [x] Reporte diario inicial
- [x] Módulo de gastos con afectación de caja
- [x] Producción diaria con incremento de inventario
- [x] Mermas de producción con movimiento `Waste`
- [x] Producción multi-producto con historial y reverso por `ProductionCancellation`
- [x] Consumo manual de insumos de producción con movimiento `ProductionInput`
- [x] Recetas de producción para calcular insumos automáticamente
- [x] Costeo de producción y margen estimado por producto vendido
- [x] Reporte de rentabilidad por rango de fechas
- [x] Exportación PDF/CSV de reportes diario y rentabilidad
- [x] Reporte diario ampliado con gastos, producción, ventas de pollo, disponibilidad y mermas
- [x] Dashboard gerencial con KPIs resumidos
- [x] Pruebas de integración para venta completa y rollback transaccional
- [x] Pruebas de integración para anulación de venta
- [x] Autorización por roles en pantallas y endpoints operativos
- [x] Mensajes de error UI consistentes en pantallas operativas
- [x] Retiros de caja y reporte histórico de cierres
- [x] Inventario operativo: existencias, movimientos, ajustes, mínimos y alertas
- [x] Auditoría de movimientos operativos por usuario

## Próximo incremento recomendado

- [ ] Gestión de usuarios y asignación de roles desde UI

## Licencia

Proyecto privado. Todos los derechos reservados.
