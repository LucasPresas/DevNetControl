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
│   └── SessionLog.cs             # Logs de sesiones
├── Dtos/                          # Data Transfer Objects
│   └── RequestDtos.cs            # Requests y responses centralizados
├── Infrastructure/
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs    # DbContext con filtros globales
│   │   ├── DbInitializer.cs          # Seed de datos iniciales
│   │   ├── DbContextFactory.cs       # Factory para migrations
│   │   └── Migrations/              # Migraciones EF Core
│   ├── Security/
│   │   ├── TokenService.cs          # Generación JWT
│   │   ├── ClaimsHelper.cs          # Helpers para claims JWT
│   │   └── EncryptionService.cs     # Encriptación de passwords VPS
│   ├── Services/
│   │   ├── UserProvisioningService.cs    # Crear usuario, extender servicio
│   │   ├── CreditService.cs             # Transferencia y carga de créditos
│   │   ├── SshUserManager.cs            # Provisión de usuarios vía SSH
│   │   ├── SshService.cs                # Ejecución de comandos SSH
│   │   └── UserExpirationBackgroundService.cs  # Expiración background
│   └── Middleware/
│       └── GlobalExceptionHandler.cs    # Manejo global de excepciones
├── Controllers/
│   ├── AuthController.cs          # Login, cambio de password
│   ├── UserController.cs          # Gestión de usuarios y jerarquía
│   ├── AdminController.cs         # Operaciones administrativas
│   ├── PlanController.cs          # CRUD de planes
│   ├── VpsNodeController.cs       # Gestión de nodos VPS
│   ├── CreditController.cs        # Transferencia y saldo de créditos
│   ├── AuditController.cs         # Historial de transacciones
│   ├── SuperAdminController.cs    # Operaciones de SuperAdmin
│   └── SessionLogController.cs    # Gestión de logs de sesión
├── Validators/
│   └── RequestValidators.cs       # Validaciones FluentValidation
└── Program.cs                     # Configuración y startup
```

## Funcionalidad Actual

### Dominio (Entidades)
- **User**: Gestión con roles (SuperAdmin, Admin, Reseller, SubReseller, Customer), jerarquía padre-hijo, créditos, planes y estado de provisión VPS
- **VpsNode**: Nodos VPS con acceso SSH encriptado, labels personalizados
- **Tenant**: Aislamiento multi-tenant con subdominios
- **Plan**: Planes con costo en créditos, duración, conexiones máximas y dispositivos
- **CreditTransaction**: Auditoría completa (SourceUserId, TargetUserId, Type, Amount)
- **NodeAccess/PlanAccess**: Control de acceso granular por usuario
- **SessionLog**: Tracking de sesiones activas

### Servicios Implementados
- **UserProvisioningService**: Crear usuario, extender servicio, remover de VPS (con integración SSH)
- **CreditService**: Transferir créditos entre usuarios, agregar créditos con auditoría
- **SshUserManager**: Provisión de usuarios vía SSH, gestión de expiración, eliminación
- **SshService**: Ejecución de comandos SSH, test de conexión, métricas del sistema
- **UserExpirationBackgroundService**: Monitoreo y desactivación de usuarios expirados
- **TokenService**: Generación y validación de JWT
- **EncryptionService**: Encriptación de contraseñas de nodos VPS

### API Endpoints

#### Auth (`/api/auth`)
- `POST /login` - Autenticación y generación de JWT
- `POST /change-password` - Cambio de contraseña autenticado
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

#### SuperAdmin (`/api/superadmin`)
- `POST /provision-node` - Asignar nodo a tenant (SuperAdmin)

### Seguridad
- Autenticación JWT con claims (UserId, TenantId, Role)
- Políticas de autorización: `AdminOnly`, `SuperAdminOnly`, `ResellerOrAbove`, `SubResellerOrAbove`
- Filtros globales de EF Core por TenantId (aislamiento de datos)
- Hasheo de contraseñas con BCrypt
- Encriptación de passwords de nodos VPS
- Validación de modelos con FluentValidation

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
- **Autenticación**: JWT Bearer conroles y claims
- **Puerto**: 5066 (http://127.0.0.1:5066)
- **Documentación**: Swagger UI habilitado en desarrollo
- **Validación**: FluentValidation integrado
- **Manejo de errores**: Middleware global de excepciones

## Migraciones EF Core

Última migración aplicada: `20260501192052_AddedUserCreatedAt`

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
- `AddedUserCreatedAt` - Campo CreatedAt en User

## Estado de Build y Ejecución

✅ Build exitoso: 0 errores (relacionados a cambios críticos)
✅ Migraciones aplicadas correctamente
✅ Servidor levanta sin errores
✅ SessionLogController: Implementado completamente
✅ RequestValidators: Completo para todos los DTOs
✅ GlobalExceptionHandler: Middleware completo con 8 tipos de excepciones
✅ DbInitializer: Genera 4 planes por defecto (Basic, Pro, Enterprise, Trial)
⚠️ Puerto 5066 debe estar libre para ejecutar
⚠️ UserController: Requiere completar UserProvisioningService (pre-existente)

## Pendientes por Implementar

### Crítico ✅ COMPLETADO (01/05/2026)
- [x] **SessionLogController**: Endpoints para consultar logs de sesión activos/históricos
- [x] **Validators/RequestValidators.cs**: Validaciones FluentValidation para todos los DTOs
- [x] **Seed de planes**: Crear planes por defecto en DbInitializer
- [x] **Manejo de errores**: Completar GlobalExceptionHandler.cs (middleware global)

### Seguridad
- [ ] Rate limiting en endpoints sensibles (login, creación de usuarios)
- [ ] Refresh token mechanism para JWT
- [ ] HTTPS enforcement en producción
- [ ] Sanitización de inputs en comandos SSH
- [ ] Logging de auditoría para acciones administrativas

### Funcionalidad
- [ ] **Notificaciones**: Alertas de crédito bajo, expiración cercana
- [ ] **Dashboard**: Estadísticas para Admin/Reseller (gráficos de uso)
- [ ] **Bulk operations**: Creación masiva de usuarios vía CSV
- [ ] **Trial system**: Lógica completa de trials y TrialExpiry
- [ ] **Plan restrictions**: Validar MaxDevices y MaxConnections en tiempo real
- [ ] **Node health checks**: Monitoreo automático de disponibilidad de nodos

### Frontend (DevNetControl.Web)
- [ ] Cliente React/Angular para consumir la API
- [ ] Autenticación con JWT en frontend
- [ ] Dashboard interactivo con métricas
- [ ] Gestión de usuarios y planes desde UI

### Técnico y DevOps
- [ ] **Tests unitarios** para servicios críticos (CreditService, UserProvisioningService)
- [ ] **Tests de integración** para endpoints principales
- [ ] **Docker** setup para API y base de datos
- [ ] **CI/CD** pipeline (GitHub Actions / GitLab CI)
- [ ] **Logging estructurado** (Serilog / NLog)
- [ ] **Monitoring** (Prometheus / Application Insights)

### Base de Datos
- [ ] Índices en columnas frecuentes (TenantId, ParentId, UserName)
- [ ] Backup strategy y point-in-time recovery
- [ ] Connection pooling en producción
- [ ] Considerar migración a PostgreSQL para producción

### Documentación
- [ ] Completar documentación de API en Swagger con ejemplos
- [ ] Documentación de arquitectura y patrones utilizados
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
