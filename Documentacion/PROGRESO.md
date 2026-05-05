# 🛡️ DevNet Control - Estado del Proyecto & Roadmap SaaS

Este documento resume la arquitectura aplicada hasta la fecha y las reglas de negocio implementadas para el modelo SaaS Multi-Tenant.

---

## 1. 🏗️ Arquitectura Aplicada (Estado Actual)

### A. Core SaaS & Aislamiento
* **Multi-Tenancy:** Implementada la entidad `Tenant`. Cada cliente (ej: Lurzavic) es un inquilino independiente.
* **Aislamiento de Datos:** Aplicado `Global Query Filter` en `ApplicationDbContext`. Las consultas a `Users`, `VpsNodes`, `ActivityLogs`, `Notifications` se filtran automáticamente por `TenantId`.
* **Seguridad JWT:** El `TenantId` se inyecta como un Claim en el token JWT, permitiendo al backend identificar el contexto del inquilino en cada petición.
* **Refresh Token:** Implementado mecanismo de renovación de JWT sin re-login.

### B. Infraestructura & Seguridad
* **Inyección de Dependencias:** Corregido el error de Scope del `DbContextFactory`. Ahora el backend es estable en entornos Windows y Linux.
* **Encryption Service:** Implementado servicio de cifrado AES-256 para proteger las credenciales (Root Password) de los nodos VPS en la base de datos.
* **SSH Service:** Estructura base para ejecución de comandos remotos en servidores Linux.
* **SSH Sanitizer:** Servicio dedicado para prevenir inyección de comandos shell (NUEVO).
* **Rate Limiting:** Configurado en endpoints sensibles (login, creación de usuarios) con políticas por IP (NUEVO).
* **HTTPS Enforcement:** Configurado para entornos de producción (NUEVO).

### C. Frontend (React + Zustand + Tailwind)
* **Estado Global:** Implementado `authStore` con Zustand para persistir token y datos del Tenant (White Label ready).
* **Interceptores:** Axios configurado para manejar expiración de sesiones y redirección al login.
* **Dark Mode:** Toggle implementado con persistencia en localStorage (NUEVO).
* **Páginas Completas:** Login, Dashboard, Users, Credits, Plans, Nodes, NodeDetail, Resellers, Logs, AdminPanel, SuperAdminPanel (NUEVO).
* **Componentes:** Modales para editar usuario, agregar conexiones, renovar planes (NUEVO).

---

## 2. 🟢 Fase 7 - Completada (30 abril 2026)

### A. Sistema de Créditos (Implementado)
* **Regla de Consumo:** 1 Crédito = 1 Dispositivo activo x 30 días de servicio.
* **Configurable por Tenant:** `CreditsPerDevice` en la entidad Tenant (default: 1).
* **Jerarquía de Saldo:**
    * **Admins:** No consumen créditos al crear usuarios.
    * **Resellers/Subresellers:** Se descuentan créditos automaticamente (`MaxDevices * CreditsPerDevice`).
* **Validacion:** El sistema impide la creacion si `SaldoReseller < CreditosNecesarios`.
* **Reembolso automatico:** Si el comando SSH falla, los creditos son devueltos al creador.
* **Extension de servicio:** Endpoint `POST /api/user/{id}/extend-service` que cobra creditos segun dias adicionales.
* **Balance Snapshots:** CreditTransaction ahora guarda balances antes/después (NUEVO).

### B. Gestion de Pruebas (Demos) - Implementado
* **Costo:** 0 Creditos (gratuitas).
* **Prefijo:** Los usuarios de prueba llevan prefijo `trial_` automaticamente.
* **Control de Tiempo:** Duracion configurable por Tenant (`TrialMaxHours`, default: 2 horas).
* **Limite por Reseller:** Configurable por Tenant (`TrialMaxPerReseller`, default: 5).
* **Expiracion Automatica:** `UserExpirationBackgroundService` corre cada 15 minutos, elimina usuarios de prueba vencidos del VPS y de la base de datos.

### C. SSH Scripts Refactorizados
* **`SshUserManager`** nuevo servicio dedicado para gestion de usuarios en VPS:
    * `CreateUserOnVpsAsync`: Crea usuario con `useradd`, setea password con `chpasswd`, configura expiracion con `chage -E`.
    * `DeleteUserFromVpsAsync`: Elimina usuario con `userdel -r`, limpia config SSH.
    * `ExtendUserExpiryOnVpsAsync`: Extiende expiracion con `chage -E`.
    * `ChangeUserPasswordOnVpsAsync`: Cambia contraseña via `chpasswd`.
    * `GetUserExpiryAsync`: Obtiene info de expiracion via `chage -l`.
    * `ListExpiredUsersAsync`: Lista usuarios expirados en el VPS.
* **Seguridad:** Validacion de username con regex (`^[a-zA-Z0-9_]{3,50}$`), escape de argumentos shell.

### D. Entidades Actualizadas
* **Tenant:** Agregados `TrialMaxHours`, `TrialMaxPerReseller`, `CreditsPerDevice`.
* **User:** Agregados `IsTrial`, `TrialExpiry`, `IsProvisionedOnVps`.
* **CreditTransaction:** Agregado `Type` enum (Transfer, ManualAdd, UserCreationCost, ServiceExtension, TrialCreation, NodeCreationCost), balances snapshots.
* **ActivityLog:** Registro de actividades con Actor/Target, créditos consumidos (NUEVO).
* **Notification:** Sistema de notificaciones a usuarios (NUEVO).
* **RefreshToken:** Tokens de renovación JWT (NUEVO).

### E. Nuevos Endpoints
| Metodo | Ruta | Descripcion | Auth |
|--------|------|-------------|------|
| POST | `/api/user/create-subuser` | Crea sub-usuario con descuento de creditos (body: `maxDevices`) | SubResellerOrAbove |
| POST | `/api/user/create-trial` | Crea usuario de prueba (0 creditos, expiracion automatica) | SubResellerOrAbove |
| POST | `/api/user/{id}/extend-service` | Extiende servicio de usuario (cobra creditos al padre) | Auth |
| POST | `/api/user/{id}/remove-from-vps` | Elimina usuario del VPS | AdminOnly |
| POST | `/api/auth/refresh` | Renueva JWT usando refresh token | Public |
| GET | `/api/activity` | Listar actividades con filtros | Auth |
| GET | `/api/activity/stats` | Estadísticas de actividades | AdminOnly |
| GET | `/api/notification/mine` | Notificaciones del usuario | Auth |
| GET | `/api/monitor/node-health` | Health checks de nodos | Admin+ |
| POST | `/api/vpsnode/{id}/check-health` | Check manual de salud | Admin+ |

### F. Frontend Actualizado
* **Users.jsx:** Formulario de creacion con campo `maxDevices`, boton para crear usuarios de prueba, lista muestra estado de provision VPS, expiracion, y badge de prueba. Boton "Extender" en usuarios pagos.
* **Credits.jsx:** Historial muestra tipo de transaccion con iconos y labels (Transferencia, Carga manual, Creacion de usuario, Extension, etc.).
* **AdminPanel.jsx:** Muestra badges de VPS y prueba, info de dispositivos, expiracion, y conteo de nodos.
* **Dashboard.jsx:** Métricas de usuarios, créditos, nodos, actividad reciente (NUEVO).
* **Logs.jsx:** Visualización de session logs y activity logs (NUEVO).
* **Plans.jsx:** Gestión de planes con costos y duración (NUEVO).
* **Nodes.jsx / NodeDetail.jsx:** Gestión y monitoreo de nodos VPS (NUEVO).

---

## 3. 🟢 Fase 8 - Completada (04 mayo 2026) - Auditoría y Monitoreo

### A. Sistema de Auditoría (NUEVO)
* **ActivityLog:** Registro automático de todas las acciones importantes (crear usuario, transferir créditos, etc.).
* **ActivityController:** Endpoints para consultar actividades con filtros por usuario, tipo, fechas.
* **AuditService:** Servicio para registrar acciones administrativas con contexto completo.
* **AuditController:** Historial de transacciones con nombres de usuario.

### B. Sistema de Notificaciones (NUEVO)
* **NotificationService:** Creación y gestión de notificaciones a usuarios.
* **NotificationController:** Endpoints para consultar, marcar como leídas, contador de no leídas.
* **Tipos:** Alertas de crédito bajo, expiración cercana, acciones administrativas.

### C. Monitoreo de Nodos (NUEVO)
* **NodeHealthService:** Verificación de disponibilidad de nodos VPS.
* **NodeHealthBackgroundService:** Servicio en background que chequea salud automáticamente.
* **MonitorController:** Endpoints para estado de salud y estadísticas de tenant.

### D. Operaciones Avanzadas (NUEVO)
* **BulkOperationService:** Creación masiva de usuarios vía CSV.
* **PlanValidationService:** Validación de planes y restricciones (MaxDevices, MaxConnections).
* **NodeAccessController:** Control de acceso granular a nodos.
* **Rate Limiting:** Protección contra ataques de fuerza bruta en endpoints sensibles.

---

## 4. 📋 Pendiente (Futuras Mejoras)

* **Frontend:** Gráficos en dashboard, notificaciones visuales en tiempo real, WPA/PWA para celulares.
* **Notificaciones UI:** Mostrar notificaciones como toast/alertas en el frontend.
* **Dashboard avanzado:** Gráficos de uso de créditos, usuarios activos/expirados/prueba.
* **Configuración de Tenant:** Edición de TrialMaxHours, CreditsPerDevice desde SuperAdminPanel.
* **Tests:** Unitarios para servicios críticos, integración para endpoints principales.
* **DevOps:** CI/CD pipeline, logging estructurado (Serilog), monitoring (Prometheus).
* **Base de Datos:** Índices en columnas frecuentes, backup strategy, migración a PostgreSQL en producción.

---
*Ultima actualizacion: 04 de mayo de 2026 - Fase 8 completada (Auditoría + Notificaciones + Monitoreo + Operaciones Avanzadas)*
