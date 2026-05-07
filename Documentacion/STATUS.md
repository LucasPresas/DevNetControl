# DevNetControl - Estado del Proyecto

**Ultima actualizacion**: 07 de mayo de 2026

---

## 1. Resumen General

| Area | Estado | Progreso |
|------|--------|----------|
| Backend | Funcional (Fases 1-8 completadas) | ~95% |
| Frontend | Funcional (11 paginas completas) | ~85% |
| Seguridad | Implementado | ~90% |
| Testing | Sin tests automatizados | ~10% |
| DevOps | Docker configurado, sin CI/CD | ~40% |
| Documentacion | Actualizada (5 documentos) | ~90% |

---

## 2. Backend - Estado Detallado

### 2.1 Completado

**Dominio y Entidades (12)**
- User, Tenant, Plan, VpsNode, CreditTransaction
- ActivityLog, AuditLog, SessionLog, NodeAccess, PlanAccess
- Notification, RefreshToken

**Servicios (15)**
- CreditService - Transferencias, cargas, balance con snapshots
- SshService - Comandos SSH, metricas, sesiones activas, enforcement
- SshUserManager - Provision de usuarios en VPS (crear, eliminar, extender, cambiar password)
- SshSanitizerService - Sanitizacion de comandos SSH
- UserProvisioningService - Creacion de usuarios con descuento de creditos
- UserOperationService - Add connections, renew plan, extend service
- TokenService - Generacion y validacion JWT + Refresh Tokens
- EncryptionService - Encriptacion AES-256 de passwords VPS
- AuditService - Registro de auditoria con deteccion de IP
- ActivityLogService - 13 metodos de logging automatico
- NotificationService - Alertas de credito bajo, expiracion
- NodeHealthService - Health checks de nodos via SSH
- BulkOperationService - Operaciones masivas CSV
- PlanValidationService - Validacion de MaxDevices/MaxConnections
- DbInitializer - Seed de SuperAdmin, Admin, 4 planes por defecto

**Middleware y Seguridad**
- GlobalExceptionHandler - 8 tipos de excepcion mapeados
- Rate Limiting custom - 6 politicas (auth-login, auth-change-password, user-create, credit-transfer, vps-execute, api-general)
- JWT Bearer - Access tokens 15 min, Refresh tokens 7 dias en DB
- BCrypt - Hashing de contraseñas
- AES-256 - Encriptacion de passwords VPS
- SSH Sanitization - Bloqueo de `; & | \ $()` y patrones de inyeccion
- HSTS - 1 ano, includeSubDomains, preload (produccion)
- 4 politicas de autorizacion: SuperAdminOnly, AdminOnly, ResellerOrAbove, SubResellerOrAbove

**Controladores (14) - ~90+ endpoints**
- AuthController, UserController, AdminController, PlanController
- VpsNodeController, CreditController, AuditController, SessionLogController
- ActivityController, NotificationController, MonitorController, NodeAccessController
- SuperAdminController, TestController

**Background Services**
- UserExpirationBackgroundService - Cada 15 min (trials y pagos expirados)
- NodeHealthBackgroundService - Cada 5 min (disponibilidad de nodos)

**Base de Datos**
- 43 migraciones aplicadas
- SQLite (desarrollo) / PostgreSQL (produccion) - deteccion automatica
- Global Query Filters para multi-tenancy

### 2.2 Bugs Conocidos (Backend)

| # | Gravedad | Descripcion | Archivo | Estado |
|---|----------|-------------|---------|--------|
| B1 | Baja | Servicios registrados 2 veces en DI | `Program.cs:69-74` | **Corregido** |
| B2 | Media | DTOs shadowed: controllers definen records que contradicen RequestDtos.cs | `PlanController`, `VpsNodeController` | Pendiente |
| B3 | Baja | `VpsNode.label` en minuscula (viola convenciones C#) | `Domain/VpsNode.cs` | Pendiente |
| B4 | Media | ProvisionNode en SuperAdminController incompleto | `SuperAdminController.cs` | Pendiente |
| B5 | Media | AddCreditsAsync: SourceUserId = TargetUserId | `CreditService.cs` | Pendiente |
| B6 | Baja | EntityConfigurations no aplicadas (codigo muerto) | `Persistence/Configurations/` | Pendiente |
| B7 | Baja | JWT key hardcoded en appsettings.json (fallback) | `appsettings.json` | Pendiente |
| B8 | Media | UpdateUser duplicado en AdminController y UserController | `AdminController.cs`, `UserController.cs` | Pendiente |

### 2.3 Mejoras Implementadas Recientemente

| Feature | Descripcion |
|---------|-------------|
| Sub-reseller deletion | `DELETE /api/user/{id}/sub-reseller` con reembolso de creditos |
| Sub-reseller credit load | `POST /api/user/{id}/add-credits` para cargar creditos a sub-resellers |
| Dashboard stats | `GET /api/user/dashboard-stats` con usuarios online, por vencer, expirados |
| Node health con carga | `GET /api/vpsnode/health` ahora incluye CPU%, RAM%, Disk%, userCount |
| Activity log de eliminaciones | `LogSubResellerDeletedAsync` registra reembolso, hijos eliminados, saldo |
| FK cleanup en delete | Eliminacion de ActivityLogs/Notifications/Sessions al borrar usuarios (evita 409) |

---

## 3. Frontend - Estado Detallado

### 3.1 Completado

**Paginas (10)**
- Login, Dashboard, Users, Resellers, Plans, Nodes, NodeDetail, Logs, AdminPanel, SuperAdminPanel
- Modulo Credits eliminado (integrado en dashboard y resellers)

**Componentes**
- Layout - Sidebar con secciones condicionales por rol, responsive, top bar con balance y toggle Dark/Light mode
- EditUserModal - Edicion de usuarios con diff de campos
- AddConnectionsModal - Agregar conexiones extra
- RenewPlanModal - Renovar plan con seleccion de plan

**Temas**
- Dark Mode (default): paleta slate/azul oscuro
- Light Mode: paleta Beige & Crema (`#FDF9F3`, acento verde `#4B9B7B`)
- Toggle en top bar, persistencia en localStorage

**Dashboard**
- Estadisticas: usuarios activos/expirados/online/por vencer, resellers, conexiones
- Alerta visual para usuarios que vencen en 3 dias
- Nodos con barras de carga (CPU%, RAM%, Disk%) - Admin only
- Actividad reciente con detalle de eliminaciones

### 3.2 Bugs Conocidos (Frontend)

| # | Gravedad | Descripcion | Archivo | Estado |
|---|----------|-------------|---------|--------|
| F1 | Media | Token mismatch: `data.token` vs `data.accessToken` | `authStore.js:23` | Pendiente |
| F2 | Baja | useDarkMode hook existe pero no se usa (implementado en Layout) | `hooks/useDarkMode.js` | **Resuelto** |
| F3 | Alta | Debug API expuesto en produccion | `main.jsx` | Pendiente |
| F4 | Baja | console.log excesivo en interceptors | `lib/api.js` | Pendiente |
| F5 | Baja | alert() en Nodes.jsx | `pages/Nodes.jsx` | Pendiente |
| F6 | Baja | Resellers_backup.jsx archivo muerto | `pages/` | Pendiente |
| F7 | Media | Role numerico vs string en Dashboard | `Dashboard.jsx` | Pendiente |
| F8 | Baja | AdminPanel/SuperAdminPanel clases inconsistentes | `pages/` | Pendiente |
| F9 | Media | Credits transfer requiere UUID manual | `pages/Credits.jsx` | Pendiente |

### 3.3 Mejoras Implementadas Recientemente

| Feature | Descripcion |
|---------|-------------|
| Sidebar por rol | Resellers no ven Planes/Nodos/Creditos |
| Creditos eliminado | Modulo Credits removido, logica integrada en resellers |
| Delete sub-reseller | Usa endpoint correcto segun rol, muestra reembolso |
| Cargar creditos | Boton de creditos visible para resellers |
| Logs de eliminacion | Muestran reembolso, hijos eliminados, saldo antes/despues |
| Dashboard mejorado | 4 tarjetas de stats, alertas, nodos con carga |
| Dark/Light toggle | Boton en top bar, paleta Beige & Crema para light mode |

---

## 4. Seguridad - Estado Detallado

### 4.1 Implementado

| Mecanismo | Estado |
|-----------|--------|
| JWT con Refresh Token | Completo |
| BCrypt password hashing | Completo |
| AES-256 passwords VPS | Completo |
| SSH command sanitization | Completo |
| Rate limiting 6 politicas | Completo |
| HSTS + HTTPS | Completo |
| Multi-tenant isolation (EF filters) | Completo |
| Global exception handler | Completo |
| 4 politicas de autorizacion | Completo |

### 4.2 Pendiente de Seguridad

| Item | Prioridad |
|------|-----------|
| CSRF tokens (si se agrega cookie auth) | Baja |
| Content Security Policy headers | Media |
| Security headers (X-Frame-Options, X-Content-Type-Options) | Media |
| Audit log de intentos de login fallidos | Media |
| Password complexity validation | Baja |
| 2FA para admin accounts | Baja |
| Rotacion automatica de JWT key | Baja |

---

## 5. Testing - Estado Detallado

### 5.1 Actual

| Tipo | Estado |
|------|--------|
| Tests unitarios | No implementados |
| Tests de integracion | No implementados |
| Tests E2E | No implementados |
| Testing manual | Swagger UI + Debug tool en consola |

### 5.2 Pendiente

Ver TESTING.md para plan detallado de testing.

---

## 6. DevOps - Estado Detallado

### 6.1 Implementado

| Item | Estado |
|------|--------|
| Dockerfile (multi-stage) | Completo |
| docker-compose.yml (API + PostgreSQL) | Completo |
| .env.production | Configurado |
| .dockerignore | Configurado |

### 6.2 Pendiente

| Item | Prioridad |
|------|-----------|
| CI/CD pipeline (GitHub Actions) | Alta |
| Reverse proxy (Nginx/Caddy) | Alta |
| SSL/TLS certificado (Let's Encrypt) | Alta |
| Backup automatico de PostgreSQL | Media |
| Health check endpoint para load balancer | Media |
| Logging estructurado (Serilog) | Media |
| Monitoring (Prometheus/Grafana) | Baja |
| Docker para frontend (build estatico) | Media |

---

## 7. Checklist - Pendientes por Resolver (Version Actual)

### Backend - Bugs y Mejoras Inmediatas

- [ ] **B1**: Eliminar registros duplicados de servicios en Program.cs
- [ ] **B2**: Unificar DTOs - usar RequestDtos.cs en todos los controllers, eliminar records locales
- [ ] **B3**: Renombrar `VpsNode.label` a `Label` (requiere migracion)
- [ ] **B4**: Completar ProvisionNode (agregar EncryptedPassword + OwnerId)
- [ ] **B5**: Corregir AddCreditsAsync para que SourceUserId sea el admin que carga
- [ ] **B6**: Aplicar EntityConfigurations o eliminar archivos
- [ ] **B7**: Eliminar JWT key hardcoded de appsettings.json (dejar solo en env var)
- [ ] **B8**: Resolver duplicacion de UpdateUser (AdminController vs UserController)

### Frontend - Bugs y Mejoras Inmediatas

- [ ] **F1**: Corregir `token: data.token` a `token: data.accessToken` en authStore
- [ ] **F3**: Gatear debugApi.js con `import.meta.env.DEV`
- [ ] **F4**: Eliminar o gatear console.log de interceptors para produccion
- [ ] **F5**: Reemplazar alert() en Nodes.jsx con message state
- [ ] **F6**: Eliminar Resellers_backup.jsx
- [ ] **F7**: Corregir role comparison (numeros vs strings) en Dashboard
- [ ] **F8**: Unificar estilos de AdminPanel/SuperAdminPanel con CSS custom properties
- [ ] **F9**: Agregar busqueda de usuario en transferencia de creditos
- [ ] **F2**: Conectar useDarkMode hook o eliminarlo

### Funcionalidad Faltante

- [ ] Notificaciones visuales toast en frontend
- [ ] Graficos en dashboard (uso de creditos, tendencias)
- [ ] PWA para moviles (manifest, service worker)
- [ ] Edicion de configuracion de Tenant desde SuperAdminPanel
- [ ] User search/select en transferencia de creditos
- [ ] Bulk operations UI completa (crear usuarios masivos desde CSV)
- [ ] 2FA para cuentas Admin
- [ ] Exportar reportes CSV/PDF

### Testing

- [ ] Tests unitarios para CreditService, UserProvisioningService, SshUserManager
- [ ] Tests de integracion para endpoints principales (auth, users, credits, nodes)
- [ ] Tests E2E con Playwright para flujos criticos

### DevOps

- [ ] CI/CD pipeline
- [ ] Reverse proxy + SSL
- [ ] Backup automatico
- [ ] Logging estructurado

---

## 8. Mejoras Planificadas (Version 2)

### Arquitectura
- Migrar a Clean Architecture con proyectos separados (Domain, Application, Infrastructure, API)
- Implementar CQRS con MediatR
- Agregar caching distribuido (Redis) para queries frecuentes
- Migrar a PostgreSQL exclusivamente (eliminar SQLite)

### Funcionalidad
- **Sistema de facturacion**: Integrar Stripe/PayPal para compra de creditos
- **Dashboard analytics**: Graficos de uso, tendencias, predicciones
- **Notificaciones push**: WebSockets o Server-Sent Events en tiempo real
- **Sistema de tickets**: Soporte integrado para customers
- **API rate limiting por tenant**: Limites configurables por tenant
- **Webhooks**: Notificar eventos externos (usuario creado, credito bajo, etc.)
- **Exportar reportes**: CSV/PDF de actividades, creditos, usuarios
- **Sistema de plugins**: Extensiones para tipos de VPS (no solo SSH)
- **Multi-idioma**: i18n para frontend y emails

### Frontend
- **PWA completa**: Instalable en celulares, offline support
- **Responsive mejorado**: Optimizar tablas grandes en mobile
- **Animaciones y transiciones**: Mejor UX
- **Paginacion server-side**: Para listas grandes (usuarios, logs)

### Seguridad
- **2FA**: TOTP para cuentas Admin/SuperAdmin
- **Audit log inmutable**: Logs de auditoria que no se pueden borrar
- **IP whitelist**: Restringir acceso a panel por IP
- **Session management**: Ver sesiones activas, cerrar remotamente
- **Password policies**: Complejidad, expiracion, historial

### DevOps
- **CI/CD**: GitHub Actions con tests automaticos
- **Monitoring**: Prometheus + Grafana dashboards
- **Alerting**: Notificaciones de errores, nodos caidos
- **Auto-scaling**: Kubernetes o Docker Swarm para alta disponibilidad
- **Blue-green deployments**: Deploy sin downtime

---

## 9. Credenciales por Defecto (Seed)

| Rol | Usuario | Password | Tenant |
|-----|---------|----------|--------|
| SuperAdmin | `superadmin` | `superadmin123` | Platform |
| Admin | `admin` | `admin123` | Default Tenant |

**IMPORTANTE**: Cambiar estas credenciales inmediatamente en produccion.

---

## 10. Comandos Utiles

```bash
# Backend
dotnet restore
dotnet build
dotnet run                          # Puerto 5066
dotnet ef migrations add <Nombre>
dotnet ef database update
kill -9 $(lsof -t -i :5066)        # Liberar puerto

# Frontend
cd DevNetControl.Web
npm install
npm run dev                         # Puerto 3000
npm run build

# Docker
docker-compose up -d
docker-compose down
docker-compose logs -f api

# Swagger
http://localhost:5066/swagger

# Debug API (consola del navegador)
DEBUG.showHelp()
```

---

## 11. Estructura de Documentacion

| Archivo | Proposito |
|---------|-----------|
| `TECHNICAL.md` | Documentacion tecnica completa del proyecto |
| `STATUS.md` | Este archivo - estado, bugs, pendientes, roadmap |
| `TESTING.md` | Plan de testing - como testear manual y automaticamente |
| `DEPLOYMENT.md` | Guia de despliegue a produccion en VPS |
| `IA_PROTOCOL.md` | Protocolo de trabajo para agentes IA |

---

*Documento vivo - actualizar con cada cambio significativo*
