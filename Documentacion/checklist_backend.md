🚀 DevNetControl - Progress Report
🟢 Fase 1: Cimientos y Persistencia (¡Completado!)
[x] Arquitectura Limpia: Estructura de carpetas definida.

[x] Modelado de Dominio: Entidades User, VpsNode, Tenant, Plan, CreditTransaction, NodeAccess, PlanAccess, SessionLog, ActivityLog, AuditLog, Notification, RefreshToken creadas.

[x] Configuración Híbrida: Switch SQLite/PostgreSQL funcional.

[x] Mapeo Eficiente: Configuraciones de EF Core separadas (CreditTransactionConfiguration).

[x] Auto-Seeding: Admin inicial (admin / admin123) y planes por defecto vía DbInitializer.

🟢 Fase 2: Seguridad y Autenticación (100% - Completada!)
[x] Hashing de Contraseñas: BCrypt implementado y funcionando en el Seed.

[x] JWT (JSON Web Tokens): Middleware configurado en Program.cs (Validación de Issuer/Audience/Lifetime/SigningKey).

[x] Refresh Token: Mecanismo de renovación JWT implementado (NUEVO).

[x] Login Endpoint: Controlador que emite el token + refresh token.

[x] Roles y Políticas: [Authorize(Roles = "Admin")] testeado y funcionando + políticas centralizadas (AdminOnly, ResellerOrAbove, SubResellerOrAbove).

[x] Cambio de Contraseña: Endpoint POST /api/auth/change-password agregado.

[x] JWT Key desde variables de entorno: Soporte para DEVNETCONTROL_JWT_KEY en producción.

[x] Rate Limiting: Configurado en endpoints sensibles (login, creación) (NUEVO).

[x] HTTPS Enforcement: Configurado para producción (NUEVO).

[x] Sanitización SSH: SshSanitizerService para prevenir inyección (NUEVO).

🟢 Fase 3: Lógica de Negocio (¡Completada!)
[x] Gestión de Créditos: CreditService con transferencias, balance, historial, carga manual y balance snapshots.

[x] CRUD de Usuarios: Crear subusuarios, listar, ver detalle, editar perfil, eliminar con validaciones FK.

[x] Registro de Transacciones: Historial completo con dirección (enviado/recibido) y snapshots.

[x] Jerarquía: Árbol completo de sub-users con endpoint GET /api/user/me/hierarchy.

[x] Panel Admin: Listar todos los usuarios, detalle completo, editar rol/créditos, agregar créditos manualmente, eliminar usuarios.

[x] Creación de Subusuarios: Endpoint POST /api/user/create-subuser con descuento de créditos.

[x] Usuarios Trial: Endpoint POST /api/user/create-trial (0 créditos, expiración automática).

[x] Extensión de Servicio: Endpoint POST /api/user/{id}/extend-service con cobro de créditos.

🟣 Fase 4: Integración con VPS (SSH & Scripts) - ¡Completada!
[x] Capa de Conectividad: SSH.NET instalado y SshService implementado.

[x] Cifrado de Seguridad: EncryptionService (AES-256) listo para proteger las claves de los nodos.

[x] CRUD de Nodos: Crear, listar, ver detalle, editar y eliminar nodos VPS.

[x] Test de Conexión: Endpoint POST /api/vpsnode/{id}/test-connection.

[x] Ejecución de Comandos: Endpoint POST /api/vpsnode/{id}/execute para ejecutar comandos remotos.

[x] Monitoreo Básico: Endpoint GET /api/vpsnode/{id}/metrics (CPU, RAM, disco, uptime).

[x] SshUserManager: Gestión completa de usuarios vía SSH (crear, eliminar, extender, cambiar password).

[x] Node Health Checks: Monitoreo automático de disponibilidad de nodos (NUEVO).

[x] NodeHealthBackgroundService: Servicio de monitoreo en background (NUEVO).

🟢 Fase 5: Auditoría y Notificaciones (¡Completada!)
[x] ActivityLog: Registro detallado de actividades con Actor/Target y créditos consumidos (NUEVO).

[x] ActivityController: Endpoints para consultar actividades con filtros (NUEVO).

[x] AuditService: Servicio de auditoría de acciones administrativas (NUEVO).

[x] NotificationService: Sistema de notificaciones a usuarios (NUEVO).

[x] NotificationController: Endpoints para gestionar notificaciones (NUEVO).

[x] SessionLog: Tracking de sesiones activas.

[x] SessionLogController: Endpoints para consultar logs de sesión (NUEVO).

[x] AuditController: Historial de transacciones con nombres de usuario.

🟢 Fase 6: SaaS Multi-Tenant + Frontend Dark Mode (¡Completada!)
[x] Entidad Tenant con Subdomain, Name, IsActive, TrialMaxHours, CreditsPerDevice.

[x] TenantId agregado a User, VpsNode, CreditTransaction, ActivityLog, Notification.

[x] Global Query Filters en EF Core (aislamiento automatico por tenant).

[x] TenantId claim en JWT para aislamiento a nivel API.

[x] Rol SuperAdmin para gestion de tenants desde plataforma.

[x] SuperAdminController (crear tenant, toggle activo, stats globales, provisionar nodos).

[x] Dark Mode toggle en el frontend (persiste en localStorage).

[x] Frontend React completo: Login, Dashboard, Users, Credits, Plans, Nodes, Resellers, Logs, AdminPanel, SuperAdminPanel.

[x] Zustand store para estado global con auth JWT.

[x] Axios configurado con interceptores para expiración de sesión.

🟢 Fase 7: Operaciones Avanzadas (¡Completada!)
[x] BulkOperationService: Operaciones masivas vía CSV (NUEVO).

[x] PlanValidationService: Validación de planes y restricciones (NUEVO).

[x] NodeAccessController: Control de acceso a nodos (NUEVO).

[x] MonitorController: Monitoreo de nodos y estadísticas de tenant (NUEVO).

[x] Rate Limiting: IRateLimitStore, RateLimitPolicy, middleware completo (NUEVO).

[x] UserExpirationBackgroundService: Expiración automática de usuarios trial cada 15 min.