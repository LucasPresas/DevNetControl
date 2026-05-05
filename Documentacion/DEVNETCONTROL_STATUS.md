# DevNetControl - Estado del Proyecto

## Descripción General

Sistema de gestión de usuarios y servicios VPS con arquitectura multi-tenant, jerarquía de revendedores y control de créditos. Permite a administradores crear resellers, asignar planes, gestionar nodos VPS y auditar transacciones de créditos.

## Arquitectura

```
DevNetControl.Api/
├── Domain/                         # Entidades y enums
│   ├── User.cs                    # Usuarios con roles y jerarquía
│   ├── VpsNode.cs                 # Nodos VPS
│   ├── Tenant.cs                  # Aislamiento multi-tenant
│   ├── Plan.cs                    # Planes con costo en créditos
│   ├── CreditTransaction.cs       # Auditoría de transacciones
│   ├── NodeAccess.cs              # Control de acceso a nodos
│   ├── PlanAccess.cs              # Control de acceso a planes
│   ├── SessionLog.cs             # Logs de sesiones
│   ├── ActivityLog.cs            # Auditoría de actividades (NUEVO)
│   ├── AuditLog.cs               # Logs de auditoría (NUEVO)
│   ├── Notification.cs           # Sistema de notificaciones (NUEVO)
│   └── RefreshToken.cs           # Refresh tokens JWT (NUEVO)
├── Dtos/                          # Data Transfer Objects
│   └── RequestDtos.cs            # Requests y responses centralizados
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs    # DbContext con filtros globales
│   │   ├── DbInitializer.cs          # Seed de datos iniciales
│   │   ├── DbContextFactory.cs       # Factory para migrations
│   │   ├── Configurations/          # Configuraciones de entidades (NUEVO)
│   │   │   └── CreditTransactionConfiguration.cs
│   │   └── Migrations/              # Migraciones EF Core
│   ├── Security/
│   │   ├── TokenService.cs          # Generación JWT + Refresh Token
│   │   ├── ClaimsHelper.cs          # Helpers para claims JWT
│   │   └── EncryptionService.cs     # Encriptación de passwords VPS
│   ├── Services/
│   │   ├── UserProvisioningService.cs    # Crear usuario, extender servicio
│   │   ├── CreditService.cs             # Transferencia y carga de créditos
│   │   ├── SshUserManager.cs            # Provisión de usuarios vía SSH
│   │   ├── SshService.cs                # Ejecución de comandos SSH
│   │   ├── SshSanitizerService.cs       # Sanitización de comandos SSH (NUEVO)
│   │   ├── UserExpirationBackgroundService.cs  # Expiración background
│   │   ├── AuditService.cs              # Auditoría de acciones (NUEVO)
│   │   ├── ActivityLogService.cs        # Registro de actividades (NUEVO)
│   │   ├── NotificationService.cs       # Gestión de notificaciones (NUEVO)
│   │   ├── NodeHealthService.cs         # Health checks de nodos (NUEVO)
│   │   ├── NodeHealthBackgroundService.cs # Monitoreo background (NUEVO)
│   │   ├── PlanValidationService.cs      # Validación de planes (NUEVO)
│   │   └── BulkOperationService.cs      # Operaciones masivas (NUEVO)
│   ├── RateLimiting/               # Rate limiting (NUEVO)
│   │   ├── IRateLimitStore.cs
│   │   ├── RateLimitPolicy.cs
│   │   ├── InMemoryRateLimitStore.cs
│   │   ├── RateLimitingExtensions.cs
│   │   ├── RateLimitService.cs
│   │   ├── RateLimitAttribute.cs
│   │   └── RateLimitMiddleware.cs
│   └── Middleware/
│       └── GlobalExceptionHandler.cs    # Manejo global de excepciones
├── Controllers/
│   ├── AuthController.cs          # Login, cambio de password, refresh token
│   ├── UserController.cs          # Gestión de usuarios y jerarquía
│   ├── AdminController.cs         # Operaciones administrativas
│   ├── PlanController.cs          # CRUD de planes
│   ├── VpsNodeController.cs       # Gestión de nodos VPS
│   ├── CreditController.cs        # Transferencia y saldo de créditos
│   ├── AuditController.cs         # Historial de transacciones
│   ├── SuperAdminController.cs    # Operaciones de SuperAdmin
│   ├── SessionLogController.cs    # Gestión de logs de sesión
│   ├── ActivityController.cs      # Registro de actividades (NUEVO)
│   ├── NotificationController.cs  # Sistema de notificaciones (NUEVO)
│   ├── MonitorController.cs       # Monitoreo de nodos (NUEVO)
│   ├── NodeAccessController.cs    # Control de acceso a nodos (NUEVO)
│   └── TestController.cs          # Endpoints de prueba (NUEVO)
├── Validators/
│   └── RequestValidators.cs       # Validaciones FluentValidation
└── Program.cs                     # Configuración y startup
```

## Funcionalidad Actual

### Dominio (Entidades)
- **User**: Gestión con roles (SuperAdmin, Admin, Reseller, SubReseller, Customer), jerarquía padre-hijo, créditos, planes y estado de provisión VPS
- **VpsNode**: Nodos VPS con acceso SSH encriptado, labels personalizados, health checks
- **Tenant**: Aislamiento multi-tenant con configuraciones (TrialMaxHours, CreditsPerDevice)
- **Plan**: Planes con costo en créditos, duración, conexiones máximas y dispositivos
- **CreditTransaction**: Auditoría completa (SourceUserId, TargetUserId, Type, Amount, balace snapshots)
- **NodeAccess/PlanAccess**: Control de acceso granular por usuario
- **SessionLog**: Tracking de sesiones activas
- **ActivityLog**: Auditoría de acciones con Actor/Target, créditos consumidos (NUEVO)
- **AuditLog**: Logs de auditoría del sistema (NUEVO)
- **Notification**: Sistema de notificaciones a usuarios (NUEVO)
- **RefreshToken**: Refresh tokens para JWT con expiry (NUEVO)

### Servicios Implementados
- **UserProvisioningService**: Crear usuario, extender servicio, remover de VPS (con integración SSH)
- **CreditService**: Transferir créditos entre usuarios, agregar créditos con auditoría y balance snapshots
- **SshUserManager**: Provisión de usuarios vía SSH, gestión de expiración, eliminación
- **SshService**: Ejecución de comandos SSH, test de conexión, métricas del sistema
- **SshSanitizerService**: Sanitización de comandos SSH para prevenir inyección (NUEVO)
- **UserExpirationBackgroundService**: Monitoreo y desactivación de usuarios expirados
- **TokenService**: Generación y validación de JWT + Refresh Tokens
- **EncryptionService**: Encriptación de contraseñas de nodos VPS
- **AuditService**: Registro de auditoría de acciones administrativas (NUEVO)
- **ActivityLogService**: Registro detallado de actividades con créditos (NUEVO)
- **NotificationService**: Gestión de notificaciones a usuarios (NUEVO)
- **NodeHealthService**: Health checks de nodos VPS (NUEVO)
- **NodeHealthBackgroundService**: Monitoreo automático de disponibilidad (NUEVO)
- **PlanValidationService**: Validación de planes y restricciones (NUEVO)
- **BulkOperationService**: Operaciones masivas (CSV) (NUEVO)

### API Endpoints

#### Auth (`/api/auth`)
- `POST /login` - Autenticación y generación de JWT
- `POST /change-password` - Cambio de contraseña autenticado
- `POST /refresh` - Refresh token para renovar JWT (NUEVO)
- `GET /test-db` - Test de conexión a base de datos

#### Users (`/api/user`)
- `POST /create` - Crear usuario (SubReseller o superior)
- `POST /{id}/extend-service` - Extender servicio VPS
- `POST /{id}/remove-from-vps` - Remover usuario de VPS (Admin)
- `GET /me` - Perfil del usuario actual
- `GET /me/hierarchy` - Árbol jerárquico de usuarios
- `GET /my-subusers` - Listar subusuarios directos
- `POST /create-reseller` - Crear reseller (Admin)
- `POST /{id}/load-credits` - Cargar créditos (Admin)
- `POST /create-subuser` - Crear sub-usuario con descuento de créditos (NUEVO)
- `POST /create-trial` - Crear usuario de prueba (NUEVO)

#### Admin (`/api/admin`)
- `GET /dashboard-data` - Datos del panel administrativo
- `GET /users` - Listar todos los usuarios del tenant
- `GET /users/{id}` - Detalle de usuario con relaciones
- `PUT /users/{id}` - Actualizar rol y créditos
- `DELETE /users/{id}` - Eliminar usuario (con validaciones)
- `POST /users/{id}/add-credits` - Agregar créditos

#### Plans (`/api/plan`)
- `GET /` - Listar planes disponibles
- `POST /` - Crear plan (Admin)
- `DELETE /{id}` - Eliminar plan (Admin)

#### VPS Nodes (`/api/vpsnode`)
- `POST /` - Crear nodo VPS (Reseller+)
- `GET /` - Listar nodos accesibles
- `GET /{id}` - Detalle de nodo
- `PUT /{id}` - Actualizar nodo
- `DELETE /{id}` - Eliminar nodo
- `POST /{id}/test-connection` - Test SSH
- `POST /{id}/execute` - Ejecutar comando (Reseller+)
- `GET /{id}/metrics` - Métricas del sistema
- `GET /health` - Health checks de nodos (NUEVO)
- `POST /{id}/check-health` - Check manual de salud (NUEVO)

#### Credits (`/api/credit`)
- `POST /transfer` - Transferir créditos
- `GET /balance` - Consultar saldo actual

#### Audit (`/api/audit`)
- `GET /history` - Historial de transacciones con nombres de usuario

#### Session Logs (`/api/sessionlog`) ✅ NUEVO
- `GET /` - Listar logs paginado con búsqueda (page, pageSize, search)
- `GET /{id}` - Detalle de log específico
- `POST /` - Registrar nuevo log de sesión
- `GET /user/{userId}/active` - Logs activos de usuario (Admin o usuario mismo)
- `GET /stats` - Estadísticas (Admin)
- `DELETE /{id}` - Eliminar log (Admin)
- `POST /cleanup` - Limpiar logs antiguos (Admin)

#### Activity (`/api/activity`) ✅ NUEVO
- `GET /` - Listar actividades con filtros (actionType, actor, target, dates)
- `GET /recent` - Actividades recientes (limit)
- `GET /{id}` - Detalle de actividad con transacción relacionada
- `GET /user/{userId}` - Actividades por usuario
- `GET /stats` - Estadísticas de actividades (Admin)
- `GET /credits/summary` - Resumen de créditos consumidos/agregados (Admin)

#### Notifications (`/api/notification`) ✅ NUEVO
- `GET /mine` - Notificaciones del usuario (paginado, filtro leídas)
- `POST /mark-read/{id}` - Marcar notificación como leída
- `GET /unread-count` - Contador de no leídas

#### SuperAdmin (`/api/superadmin`)
- `POST /provision-node` - Asignar nodo a tenant (SuperAdmin)

#### Monitor (`/api/monitor`) ✅ NUEVO
- `GET /node-health` - Estado de salud de todos los nodos
- `GET /tenant-stats` - Estadísticas del tenant (Admin)

#### Node Access (`/api/nodeaccess`) ✅ NUEVO
- `GET /` - Listar accesos a nodos
- `POST /` - Asignar acceso a nodo
- `DELETE /{id}` - Remover acceso

#### Rate Limiting ✅ NUEVO
- Configurado en endpoints sensibles (login, creación de usuarios)
- Políticas por IP y usuario
- Headers de rate limit en respuestas

### Seguridad
- Autenticación JWT con claims (UserId, TenantId, Role)
- Refresh Token mechanism implementado (NUEVO)
- Políticas de autorización: `AdminOnly`, `SuperAdminOnly`, `ResellerOrAbove`, `SubResellerOrAbove`
- Filtros globales de EF Core por TenantId (aislamiento de datos)
- Hasheo de contraseñas con BCrypt
- Encriptación de passwords de nodos VPS (AES-256)
- Validación de modelos con FluentValidation
- Rate Limiting en endpoints sensibles (login, creación) (NUEVO)
- Sanitización de comandos SSH para prevenir inyección (NUEVO)
- HTTPS enforcement configurado (NUEVO)

## Cómo Debería Funcionar

1. **SuperAdmin** crea tenants y asigna nodos VPS globales
2. **Admin** (por tenant) crea resellers con planes y créditos asignados
3. **Reseller** crea sub-resellers o clientes, consumiendo sus propios créditos
4. Usuarios creados se **provisionan automáticamente en VPS** vía SSH
5. **Créditos** se deducen al asignar planes o extender servicios
6. **Auditoría completa** de transacciones con origen y destino
7. **Jerarquía** permite visualizar la red de usuarios
8. Servicio en background **desactiva usuarios expirados** automáticamente
9. **Acceso granular** a nodos y planes según configuración

## Configuración Actual

- **Framework**: .NET 10.0
- **Base de datos**: SQLite (desarrollo) / PostgreSQL (producción opcional)
- **Autenticación**: JWT Bearer con claims + Refresh Token
- **Puerto**: 5066 (http://127.0.0.1:5066)
- **Documentación**: Swagger UI habilitado en desarrollo
- **Validación**: FluentValidation integrado
- **Manejo de errores**: Middleware global de excepciones (8 tipos)
- **Rate Limiting**: Configurado en endpoints sensibles
- **Frontend**: React + Vite + Tailwind CSS (DevNetControl.Web) (NUEVO)
- **Estado**: Frontend implementado con dark mode, auth JWT, páginas completas

## Migraciones EF Core

Última migración aplicada: `20260504023351_AddAdditionalConnectionsField`

Migraciones existentes:
- `InitialLinuxSetup` - Esquema inicial
- `AddCreditTransactions` - Transacciones de créditos
- `AddTenantIsolation` - Filtros multi-tenant
- `AddCreditSystemAndTrialSupport` - Sistema de créditos y trials
- `AddPlansSessionLogsAndNodeAccess` - Planes y accesos
- `AddUserIsActive` - Campo IsActive
- `ChangeDurationDaysToDurationHours` - Refactor de duración
- `AddPlanAccessTable` - Control de acceso a planes
- `FixMultiTenancyHierarchy` - Correcciones multi-tenant
- `FixMultiTenancyAndDbSets` - Ajustes de DbSets
- `RefactorMultiTenancyFinal` - Refactor final multi-tenant
- `UpdatePendingChanges` - Actualizaciones pendientes
- `AddTenantIsolation` - Aislamiento multi-tenant
- `AddUserIsActive` - Campo IsActive
- `ChangeDurationDaysToDurationHours` - Duración en horas
- `AddPlanAccessTable` - Control acceso planes
- `RefactorMultiTenancyFinal` - Refactor final
- `AddNotification` - Sistema de notificaciones (NUEVO)
- `AddAuditLog` - Logs de auditoría (NUEVO)
- `AddRefreshToken` - Refresh tokens (NUEVO)
- `AddNodeHealthChecks` - Health checks de nodos (NUEVO)
- `AddActivityLogTable` - Actividades (NUEVO)
- `FixDuplicateUserForeignKeys` - Corrección FK duplicadas (NUEVO)
- `FixUserDeletionForeignKeyConstraints` - Constraints eliminación (NUEVO)
- `AddCreditTransactionBalanceSnapshots` - Snapshots de balance (NUEVO)
- `AddAdditionalConnectionsField` - Conexiones adicionales (NUEVO)

## Estado de Build y Ejecución

✅ Build exitoso: 0 errores
✅ Migraciones aplicadas correctamente
✅ Servidor levanta sin errores
✅ SessionLogController: Implementado completamente
✅ ActivityController: Implementado completamente (NUEVO)
✅ NotificationController: Implementado completamente (NUEVO)
✅ MonitorController: Implementado completamente (NUEVO)
✅ RequestValidators: Completo para todos los DTOs
✅ GlobalExceptionHandler: Middleware completo con 8 tipos de excepciones
✅ DbInitializer: Genera 4 planes por defecto (Basic, Pro, Enterprise, Trial)
✅ Rate Limiting: Implementado y configurado (NUEVO)
✅ Refresh Token: Implementado en AuthController (NUEVO)
✅ Frontend React: Completo con dark mode y auth (NUEVO)
⚠️ Puerto 5066 debe estar libre para ejecutar

## Pendientes por Implementar

### Crítico ✅ COMPLETADO (04/05/2026)
- [x] **SessionLogController**: Endpoints para consultar logs de sesión activos/históricos
- [x] **ActivityController**: Registro y consulta de actividades (NUEVO)
- [x] **NotificationController**: Sistema de notificaciones (NUEVO)
- [x] **Validators/RequestValidators.cs**: Validaciones FluentValidation para todos los DTOs
- [x] **Seed de planes**: Crear planes por defecto en DbInitializer
- [x] **Manejo de errores**: Completar GlobalExceptionHandler.cs (middleware global)
- [x] **Rate Limiting**: Implementado en endpoints sensibles
- [x] **Refresh Token**: Mecanismo JWT renovable
- [x] **Frontend React**: Completo con auth, dark mode, páginas funcionales

### Seguridad ✅ COMPLETADO
- [x] Rate limiting en endpoints sensibles (login, creación de usuarios)
- [x] Refresh token mechanism para JWT
- [x] HTTPS enforcement en producción
- [x] Sanitización de inputs en comandos SSH
- [x] Logging de auditoría para acciones administrativas

### Funcionalidad Pendiente
- [ ] **Notificaciones UI**: Mostrar alertas de crédito bajo, expiración en frontend
- [ ] **Dashboard gráficos**: Estadísticas visuales para Admin/Reseller (gráficos de uso)
- [ ] **Bulk operations UI**: Interfaz para creación masiva de usuarios vía CSV
- [ ] **Trial system completo**: Lógica completa de trials y TrialExpiry en UI
- [x] **Plan restrictions**: Validar MaxDevices y MaxConnections en tiempo real (Backend OK)
- [x] **Node health checks**: Monitoreo automático de disponibilidad de nodos (Backend OK)

### Frontend (DevNetControl.Web) ✅ COMPLETADO
- [x] Cliente React para consumir la API
- [x] Autenticación con JWT en frontend (Zustand store)
- [x] Dark mode toggle (persiste en localStorage)
- [x] Dashboard interactivo con métricas
- [x] Gestión de usuarios y planes desde UI
- [x] Páginas: Login, Dashboard, Users, Credits, Plans, Nodes, Resellers, Logs, AdminPanel, SuperAdminPanel
- [ ] **Mejoras UI**: Gráficos en dashboard, notificaciones visuales, WPA para móviles

### Técnico y DevOps Pendiente
- [ ] **Tests unitarios** para servicios críticos (CreditService, UserProvisioningService)
- [ ] **Tests de integración** para endpoints principales
- [x] **Docker** setup para API y base de datos (docker-compose.yml presente)
- [ ] **CI/CD** pipeline (GitHub Actions / GitLab CI)
- [ ] **Logging estructurado** (Serilog / NLog)
- [ ] **Monitoring** (Prometheus / Application Insights)

### Base de Datos Pendiente
- [ ] Índices en columnas frecuentes (TenantId, ParentId, UserName)
- [ ] Backup strategy y point-in-time recovery
- [ ] Connection pooling en producción
- [ ] Considerar migración a PostgreSQL para producción

### Documentación Pendiente
- [ ] Completar documentación de API en Swagger con ejemplos
- [x] Documentación de arquitectura y patrones utilizados (ESTE ARCHIVO)
- [ ] Guía de despliegue paso a paso
- [ ] Postman collection para testing de API

## Credenciales por Defecto (Seed)

- **SuperAdmin**:
  - Usuario: `superadmin`
  - Password: `superadmin123`
  - Tenant: `Platform`

- **Admin** (Default Tenant):
  - Usuario: `admin`
  - Password: `admin123`
  - Tenant: `Default Tenant`

## Comandos Útiles

```bash
# Restaurar dependencias
dotnet restore

# Build del proyecto
dotnet build

# Crear migración
dotnet ef migrations add <NombreMigracion>

# Aplicar migraciones
dotnet ef database update

# Levantar servidor
dotnet run

# Liberar puerto si está ocupado
kill -9 $(lsof -t -i :5066)
```

## Notas de Desarrollo

- El proyecto sigue arquitectura en capas (Domain, Infrastructure, API)
- Se utilizan DTOs centralizados en `RequestDtos.cs`
- Inyección de dependencias configurada en `Program.cs`
- Filtros globales de EF Core aseguran aislamiento multi-tenant
- Servicios de background implementados correctamente
- Código sencillo, legible y siguiendo buenas prácticas

---
**Última actualización**: 04 de mayo de 2026 - Sincronizado con estado real del código (Fase 8 completada: Auditoría + Notificaciones + Monitoreo + Operaciones Avanzadas). Frontend React completo.
