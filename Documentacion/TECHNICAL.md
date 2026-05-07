# DevNetControl - Documentacion Tecnica

## 1. Que es DevNetControl

Sistema SaaS centralizado para gestionar multiples servidores VPS remotos a traves de SSH. Permite a administradores integrar sus VPS, crear revendedores (resellers) y gestionar usuarios finales (clientes VPN) sin necesidad de usar la terminal.

### Roles en el Sistema

| Rol | Descripcion |
|-----|-------------|
| **SuperAdmin** | Dueno de la plataforma. Crea y gestiona tenants (clientes SaaS). |
| **Admin** | Dueno de su marca (White Label). Gestiona sus VPS, resellers y creditos. |
| **Reseller** | Revendedor dentro de un tenant. Compra creditos y los vende a sub-resellers o clientes. |
| **SubReseller** | Sub-revendedor. Puede crear clientes finales con sus creditos. |
| **Customer** | Usuario final del servicio VPN en los nodos de su tenant. |

### Jerarquia de Creditos

```
SuperAdmin
  └── Admin (creditos ilimitados, no consume al crear)
        └── Reseller (consume creditos propios al crear sub-resellers/usuarios)
              └── SubReseller (consume creditos propios al crear usuarios)
                    └── Customer (usuario final, no crea nada)
```

**Reglas de creacion:**
- Admin crea Reseller: se descuentan los `initialCredits` asignados (creacion sin costo extra)
- Reseller crea SubReseller: se descuentan los `initialCredits` del padre
- Al eliminar un SubReseller: se reembolsan los creditos restantes al padre
- Resellers pueden cargar creditos a sus SubResellers (descuenta de su propio saldo)

### Flujo de Negocio

1. SuperAdmin crea tenants y asigna nodos VPS globales
2. Admin (por tenant) crea resellers con planes y creditos asignados
3. Reseller crea sub-resellers o clientes, consumiendo sus propios creditos
4. Usuarios se provisionan automaticamente en VPS via SSH
5. Creditos se deducen al asignar planes o extender servicios
6. Servicio en background desactiva usuarios expirados automaticamente

---

## 2. Stack Tecnologico

### Backend
- **Framework**: .NET 10.0 (ASP.NET Core Web API)
- **ORM**: Entity Framework Core 10.0.7
- **Base de datos**: SQLite (desarrollo) / PostgreSQL (produccion)
- **SSH**: SSH.NET v2024.2.0 (comunicacion con nodos VPS)
- **Validacion**: FluentValidation
- **Auth**: JWT Bearer con Refresh Token
- **Password Hashing**: BCrypt.Net-Next
- **Encriptacion**: AES-256 (passwords de nodos VPS)
- **Documentacion API**: Swagger/OpenAPI

### Frontend
- **Framework**: React 19 (SPA)
- **Build Tool**: Vite 6
- **Routing**: React Router DOM v7
- **Estado**: Zustand 5
- **HTTP Client**: Axios 1.7
- **Estilos**: Tailwind CSS 3.4
- **Iconos**: Lucide React

### Infraestructura
- **Contenedores**: Docker + docker-compose
- **Servidor HTTP**: Kestrel (integrado en .NET)
- **Reverse Proxy recomendado**: Nginx o Caddy en produccion

---

## 3. Arquitectura

```
DevNetControl/
├── DevNetControl.Api/          # Backend (.NET 10 Web API)
│   ├── Domain/                 # Entidades y enums
│   ├── Dtos/                   # Data Transfer Objects
│   ├── Controllers/            # Endpoints de la API
│   ├── Validators/             # Validaciones FluentValidation
│   ├── Infrastructure/
│   │   ├── Persistence/       # EF Core DbContext, migraciones
│   │   ├── Security/          # JWT, encriptacion, claims
│   │   ├── Services/          # Logica de negocio
│   │   ├── RateLimiting/      # Rate limiting custom
│   │   └── Middleware/        # GlobalExceptionHandler
│   ├── Program.cs              # Configuracion y startup
│   └── wwwroot/               # Archivos estaticos (debug)
│
├── DevNetControl.Web/          # Frontend (React SPA)
│   ├── src/
│   │   ├── pages/             # 11 paginas completas
│   │   ├── components/        # Layout + modales
│   │   ├── lib/               # API client + debug tool
│   │   ├── store/             # Zustand auth store
│   │   └── hooks/             # Custom hooks
│   └── vite.config.js         # Proxy /api -> localhost:5066
│
├── docker-compose.yml          # Orquestacion (API + PostgreSQL)
└── Dockerfile                  # Build del backend
```

---

## 4. Modelo de Dominio

### Entidades

**User**
- `Id` (Guid), `TenantId` (Guid), `UserName`, `PasswordHash`
- `Role` (enum: SuperAdmin, Admin, Reseller, SubReseller, Customer)
- `Credits` (decimal), `MaxDevices` (int), `ServiceExpiry` (DateTime?)
- `IsTrial` (bool), `TrialExpiry` (DateTime?), `IsProvisionedOnVps` (bool)
- `IsActive` (bool), `PlanId` (Guid?), `AdditionalConnections` (int)
- `ParentId` (Guid?) - Self-referencia para jerarquia
- Navegacion: Subordinates, OwnedNodes, Sessions, PlanAccesses, NodeAccesses

**Tenant**
- `Id` (Guid), `Name`, `Subdomain`, `AdminEmail`, `IsActive`
- `TrialMaxHours` (decimal, default: 2), `TrialMaxPerReseller` (int, default: 5)
- `CreditsPerDevice` (decimal, default: 1)

**Plan**
- `Id` (Guid), `TenantId`, `Name`, `Description`, `DurationHours`, `CreditCost`
- `MaxConnections`, `MaxDevices`, `IsActive`, `IsTrial` (computed: CreditCost == 0)

**VpsNode**
- `Id` (Guid), `TenantId`, `IP`, `SshPort`, `label`, `EncryptedPassword` (AES-256)
- `OwnerId` (Guid), `LastHealthCheck`, `IsOnline`, `LatencyMs`
- Health check background cada 5 min actualiza IsOnline/LatencyMs
- Metricas en tiempo real: CPU%, RAM%, Disk% (via SSH `top`, `free`, `df`)

**CreditTransaction**
- `Id` (Guid), `TenantId`, `SourceUserId`, `TargetUserId`, `Amount`
- `SourceBalanceBefore/After`, `TargetBalanceBefore/After`
- `Type` (enum: Transfer, UserCreation, ServiceExtension, PlanPurchase, AdminCredit, Refund)

**ActivityLog**
- Registro automatico de acciones (19 tipos: UserCreated, UserDeleted, CreditsTransferred, etc.)
- Actor, Target, creditos consumidos, balances, plan/nodo involucrado

**AuditLog**
- Logs de auditoria del sistema con IP, UserId, accion y descripcion

**SessionLog**
- Tracking de sesiones: UserId, ClientIp, NodeIp, Action, Details

**NodeAccess / PlanAccess**
- Control de acceso granular a nodos y planes por usuario

**RefreshToken**
- Tokens de renovacion JWT con expiry y revocacion

**Notification**
- Notificaciones a usuarios (LowCredit, ExpirationWarning)

---

## 5. Sistema de Creditos

### Reglas de Negocio
- **1 credito = 1 dispositivo activo x 30 dias** (configurable por Tenant en `CreditsPerDevice`)
- **Admins NO consumen creditos** al crear usuarios
- **Resellers/SubResellers SI consumen creditos**: `MaxDevices * CreditsPerDevice`
- Si el comando SSH falla, los creditos se **reembolsan automaticamente**
- Las transacciones son **atomicas** con database transactions

### Tipos de Transaccion
| Tipo | Descripcion |
|------|-------------|
| `Transfer` | Transferencia entre usuarios |
| `UserCreation` | Costo de crear un usuario |
| `ServiceExtension` | Extension de servicio |
| `PlanPurchase` | Compra de plan |
| `AdminCredit` | Creditos agregados por admin |
| `Refund` | Reembolso por fallo SSH |

---

## 6. Sistema de Trials

- **Costo**: 0 creditos (gratuitos)
- **Prefijo**: usuarios llevan `trial_` automaticamente
- **Duracion**: configurable por Tenant (`TrialMaxHours`, default: 2h)
- **Limite por reseller**: configurable (`TrialMaxPerReseller`, default: 5)
- **Expiracion automatica**: `UserExpirationBackgroundService` corre cada 15 minutos
- Elimina usuarios trial vencidos del VPS y de la base de datos

---

## 7. Multi-Tenancy

### Aislamiento de Datos
- Cada entidad critica tiene `TenantId` (FK a Tenant)
- **Global Query Filters** en EF Core filtran automaticamente por TenantId
- **TenantId** se extrae del claim JWT en cada request
- Un tenant nunca ve datos de otro

### Aislamiento de Infraestructura
- Los VPS son propiedad de cada Tenant
- SSH se conecta solo a nodos del TenantId del usuario activo
- Passwords de nodos cifrados con AES-256

---

## 8. Seguridad

| Mecanismo | Detalle |
|-----------|---------|
| JWT | Access tokens de 15 min + Refresh tokens de 7 dias en DB |
| Passwords | BCrypt hashing |
| VPS Passwords | AES-256 con key derivada de SHA256 |
| SSH Commands | Sanitizacion (bloquea `; & | \ $()` etc.) |
| Rate Limiting | Custom in-memory con 6 politicas por endpoint |
| HSTS | 1 ano, includeSubDomains, preload (produccion) |
| HTTPS | Forzado en dev y produccion |
| Autorizacion | 4 politicas: SuperAdminOnly, AdminOnly, ResellerOrAbove, SubResellerOrAbove |
| Exception Handler | Nunca expone stack traces en produccion |

---

## 9. Servicios de Background

| Servicio | Frecuencia | Funcion |
|----------|-----------|---------|
| `UserExpirationBackgroundService` | Cada 15 min | Desactiva/expulsa usuarios expirados |
| `NodeHealthBackgroundService` | Cada 5 min | Health checks de nodos VPS |
| `RateLimitCleanup` | Cada 5 min | Limpia entradas expiradas de rate limiting |

---

## 10. Endpoints de la API

### Auth (`/api/auth`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| POST | `/login` | Public | Autenticacion + JWT (RateLimit) |
| POST | `/change-password` | Auth | Cambio de password (RateLimit) |
| POST | `/refresh-token` | Public | Renovar JWT con refresh token |
| GET | `/test-db` | Public | Test de conexion a BD |

### Users (`/api/user`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| POST | `/create` | SubReseller+ | Crear subusuario (RateLimit) |
| POST | `/bulk-create` | Reseller+ | Creacion masiva CSV (RateLimit) |
| POST | `/bulk/delete` | SubReseller+ | Eliminacion masiva |
| POST | `/bulk/extend-service` | SubReseller+ | Extension masiva |
| POST | `/bulk/toggle-suspend` | Admin+ | Suspension masiva |
| POST | `/{id}/extend-service` | Auth | Extender servicio |
| POST | `/{id}/remove-from-vps` | Admin+ | Remover de VPS |
| POST | `/{id}/suspend` | SubReseller+ | Suspender usuario |
| POST | `/{id}/toggle-suspend` | Admin+ | Toggle suspension |
| POST | `/{id}/add-connection` | SubReseller+ | Agregar conexiones |
| POST | `/{id}/renew-plan` | SubReseller+ | Renovar plan |
| POST | `/{id}/nodes` | Admin+ | Asignar nodos a usuario |
| PUT | `/{id}` | Admin+ | Actualizar usuario |
| DELETE | `/{id}` | Admin+ | Eliminar usuario |
| GET | `/me` | Auth | Perfil actual |
| GET | `/me/limits` | Auth | Limites del plan actual |
| GET | `/me/hierarchy` | Auth | Arbol jerarquico |
| GET | `/my-subusers` | Auth | Subusuarios directos |
| GET | `/my-resellers` | Reseller+ | Mis resellers |
| DELETE | `/{id}/sub-reseller` | Reseller+ | Eliminar sub-reseller (reembolsa creditos) |
| POST | `/{id}/add-credits` | Reseller+ | Cargar creditos a sub-reseller |
| GET | `/active-connections` | Auth | Conexiones activas por usuario |
| GET | `/dashboard-stats` | Auth | Estadisticas para dashboard |
| GET | `/clipboard-data/{id}` | Auth | Datos para copiar al portapapeles |

### Admin (`/api/admin`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/dashboard-data` | Admin+ | Datos del panel |
| GET | `/users` | Admin+ | Todos los usuarios del tenant |
| GET | `/users/{id}` | Admin+ | Detalle de usuario |
| PUT | `/users/{id}` | Admin+ | Actualizar rol y creditos |
| DELETE | `/users/{id}` | Admin+ | Eliminar usuario |
| POST | `/users/{id}/add-credits` | Admin+ | Agregar creditos |
| POST | `/users/{id}/add-connection` | Admin+ | Agregar conexion |
| POST | `/users/{id}/renew-plan` | Admin+ | Renovar plan |
| PUT | `/users/{id}/update-basic` | Admin+ | Actualizar datos basicos |

### Plans (`/api/plan`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/` | Auth | Listar planes |
| POST | `/` | Admin+ | Crear plan |
| PUT | `/{id}` | Admin+ | Actualizar plan |
| DELETE | `/{id}` | Admin+ | Eliminar plan |
| POST | `/{id}/toggle` | Admin+ | Toggle activo/inactivo |
| GET | `/my-plans` | Auth | Mis planes accesibles |
| GET | `/user/{userId}/plans` | Admin+ | Planes de usuario |
| POST | `/user/{userId}/plans` | Admin+ | Asignar planes a usuario |
| POST | `/bulk-delete` | Admin+ | Eliminacion masiva |

### VPS Nodes (`/api/vpsnode`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| POST | `/` | Reseller+ | Crear nodo VPS |
| GET | `/` | Auth | Listar nodos accesibles |
| GET | `/{id}` | Auth | Detalle de nodo |
| PUT | `/{id}` | Auth | Actualizar nodo |
| DELETE | `/{id}` | Auth | Eliminar nodo |
| POST | `/{id}/test-connection` | Auth | Test SSH |
| POST | `/{id}/execute` | Reseller+ | Ejecutar comando (RateLimit) |
| GET | `/{id}/metrics` | Auth | Metricas del sistema |
| GET | `/{id}/health` | Reseller+ | Health check individual |
| GET | `/health` | Admin+ | Health check de todos los nodos (con CPU%, RAM%, Disk%, userCount) |
| POST | `/bulk-delete` | Admin+ | Eliminacion masiva |

### Credits (`/api/credit`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| POST | `/transfer` | Auth | Transferir creditos (RateLimit) |
| GET | `/balance` | Auth | Saldo actual |
| GET | `/history` | Auth | Historial de transacciones |

### Audit (`/api/audit`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/history` | Auth | Historial de transacciones con nombres |
| GET | `/history/summary` | Auth | Resumen de creditos |
| GET | `/logs` | Admin+ | Logs de auditoria del sistema |

### Activity (`/api/activity`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/` | Auth | Listar actividades (paginado, filtros) |
| GET | `/recent` | Auth | Actividades recientes |
| GET | `/{id}` | Auth | Detalle de actividad |
| GET | `/user/{userId}` | Auth | Actividades por usuario |
| GET | `/stats` | Admin+ | Estadisticas de actividades |
| GET | `/credits/summary` | Admin+ | Resumen creditos consumidos/agregados |

### Notifications (`/api/notification`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/mine` | Auth | Mis notificaciones |
| POST | `/mark-read/{id}` | Auth | Marcar como leida |
| GET | `/unread-count` | Auth | Contador no leidas |

### Session Logs (`/api/sessionlog`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/` | Auth | Listar logs (paginado, busqueda) |
| GET | `/{id}` | Auth | Detalle de log |
| POST | `/` | Auth | Registrar log |
| GET | `/user/{userId}/active` | Auth | Logs activos de usuario |
| GET | `/stats` | Admin+ | Estadisticas |
| DELETE | `/{id}` | Admin+ | Eliminar log |
| POST | `/cleanup` | Admin+ | Limpiar logs antiguos |

### SuperAdmin (`/api/superadmin`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| POST | `/tenants` | SuperAdmin | Crear tenant |
| GET | `/tenants` | SuperAdmin | Listar tenants |
| GET | `/stats` | SuperAdmin | Estadisticas globales |
| PUT | `/tenants/{id}/toggle` | SuperAdmin | Activar/desactivar tenant |
| POST | `/provision-node` | SuperAdmin | Asignar nodo a tenant |

### Monitor (`/api/monitor`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/user/{userId}/status` | Auth | Estado de usuario en VPS |
| POST | `/user/{userId}/enforce` | Auth | Forzar cierre de sesiones |

### Node Access (`/api/nodeaccess`)
| Metodo | Ruta | Auth | Descripcion |
|--------|------|------|-------------|
| GET | `/user/{userId}` | Admin+ | Accesos de usuario |
| POST | `/` | Admin+ | Asignar acceso a nodo |
| DELETE | `/user/{userId}/node/{nodeId}` | Admin+ | Revocar acceso |
| GET | `/my-nodes` | Reseller+ | Mis nodos accesibles |

---

## 11. Frontend - Paginas

| Pagina | Ruta | Roles | Descripcion |
|--------|------|-------|-------------|
| Login | `/login` | Public | Autenticacion |
| Dashboard | `/dashboard` | Auth | Metricas, usuarios online, expiracion, nodos con carga, actividad |
| Users | `/users` | Auth | CRUD de clientes, creacion, edicion, bulk, copiar datos |
| Resellers | `/resellers` | Admin+, Reseller+ | Gestion jerarquica de resellers (cada rol ve solo sus sub-resellers) |
| Plans | `/plans` | Admin+ | CRUD de planes, presets de duracion |
| Nodes | `/nodes` | Admin+ | CRUD de nodos VPS, test SSH |
| NodeDetail | `/nodes/:id` | Admin+ | Metricas, comandos remotos |
| Logs | `/logs` | Auth | Activity logs + session logs (incluye eliminaciones con detalle) |
| AdminPanel | `/admin` | Admin+ | Gestion global de usuarios del tenant |
| SuperAdminPanel | `/superadmin` | SuperAdmin | Gestion de tenants SaaS |

---

## 12. Configuracion

### Variables de Entorno (Produccion)

| Variable | Descripcion | Requerida |
|----------|-------------|-----------|
| `DEVNETCONTROL_JWT_KEY` | Clave secreta para JWT (minimo 32 chars) | Si |
| `DEVNETCONTROL_ENCRYPTION_KEY` | Clave para cifrado AES de passwords VPS | Si |
| `ConnectionStrings__DefaultConnection` | Connection string de PostgreSQL | Si |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Si |

### Puertos

| Servicio | Puerto |
|----------|--------|
| API (dev) | 5066 |
| API (Docker) | 5000 -> 8080 |
| Frontend (dev) | 3000 |
| PostgreSQL (Docker) | 5432 (interno) |

### Credenciales por Defecto (Seed)

| Rol | Usuario | Password | Tenant |
|-----|---------|----------|--------|
| SuperAdmin | `superadmin` | `superadmin123` | Platform |
| Admin | `admin` | `admin123` | Default Tenant |

---

## 13. Migraciones

Ultima migracion: `AddAdditionalConnectionsField`

Total: 43 migraciones. Todas usan sintaxis SQLite (las migraciones se regeneran automaticamente para PostgreSQL al cambiar el connection string).

---

*Ultima actualizacion: 07 de mayo de 2026*
