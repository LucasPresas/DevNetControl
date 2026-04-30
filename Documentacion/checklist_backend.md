🚀 DevNetControl - Progress Report
🟢 Fase 1: Cimientos y Persistencia (¡Completado!)
[x] Arquitectura Limpia: Estructura de carpetas definida.

[x] Modelado de Dominio: Entidades User, VpsNode y CreditTransaction creadas.

[x] Configuración Híbrida: Switch SQLite/PostgreSQL funcional.

[x] Mapeo Eficiente: Configuraciones de EF Core separadas.

[x] Auto-Seeding: Admin inicial (admin / Admin123!) creado vía DbInitializer.

🟢 Fase 2: Seguridad y Autenticación (100% - Completada!)
[x] Hashing de Contraseñas: BCrypt implementado y funcionando en el Seed.

[x] JWT (JSON Web Tokens): Middleware configurado en Program.cs (Validación de Issuer/Audience/Lifetime/SigningKey).

[x] Login Endpoint: Ya tenés el controlador que emite el token.

[x] Roles y Políticas: [Authorize(Roles = "Admin")] testeado y funcionando + políticas centralizadas (AdminOnly, ResellerOrAbove, SubResellerOrAbove).

[x] Cambio de Contraseña: Endpoint POST /api/auth/change-password agregado.

[x] JWT Key desde variables de entorno: Soporte para DEVNETCONTROL_JWT_KEY en producción.

🟢 Fase 3: Lógica de Negocio (¡Completada!)
[x] Gestión de Créditos: CreditService con transferencias, balance, historial y carga manual.

[x] CRUD de Usuarios: Crear subusuarios, listar, ver detalle, editar perfil, eliminar.

[x] Registro de Transacciones: Historial completo con dirección (enviado/recibido).

[x] Jerarquía: Árbol completo de sub-users con endpoint GET /api/user/me/hierarchy.

[x] Panel Admin: Listar todos los usuarios, detalle completo, editar rol/créditos, agregar créditos manualmente, eliminar usuarios.

🟣 Fase 4: Integración con VPS (SSH & Scripts) - ¡Completada!
[x] Capa de Conectividad: SSH.NET instalado y SshService implementado.

[x] Cifrado de Seguridad: EncryptionService (AES) listo para proteger las claves de los nodos.

[x] CRUD de Nodos: Crear, listar, ver detalle, editar y eliminar nodos VPS.

[x] Test de Conexión: Endpoint POST /api/vpsnode/{id}/test-connection.

[x] Ejecución de Comandos: Endpoint POST /api/vpsnode/{id}/execute para ejecutar comandos remotos.

[x] Monitoreo Básico: Endpoint GET /api/vpsnode/{id}/metrics (CPU, RAM, disco, uptime).

🔴 Fase 5: Pulido y Despliegue (¡Completada!)
[x] Validaciones: FluentValidation implementado en todos los endpoints.

[x] Manejo Global de Errores: Middleware GlobalExceptionHandler para respuestas JSON consistentes.

[x] Dockerización: Dockerfile multi-stage y docker-compose.yml con PostgreSQL.

[x] Seguridad Producción: Variables de entorno para JWT, encryption key y DB password.