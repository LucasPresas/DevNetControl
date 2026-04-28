🟢 Fase 1: Cimientos y Persistencia (¡Completado!)
[x] Arquitectura Limpia: Estructura de carpetas (Domain, Infrastructure, Api).

[x] Modelado de Dominio: Entidades User (con jerarquía de Resellers), VpsNode y CreditTransaction.

[x] Configuración Híbrida: Capacidad de switch entre SQLite y PostgreSQL en Program.cs.

[x] Mapeo Eficiente: Uso de IEntityTypeConfiguration para separar la lógica de la base de datos del modelo.

[x] Auto-Seeding: El DbInitializer que crea el Admin inicial automáticamente.

🟡 Fase 2: Seguridad y Autenticación (Próximo paso)
[x] Hashing de Contraseñas: Instalar BCrypt.Net para dejar de guardar claves en texto plano.

[ ] JWT (JSON Web Tokens): Configurar el middleware para que la API emita y valide tokens.

[ ] Login Endpoint: Crear el servicio que verifique credenciales y devuelva el token.

[ ] Roles y Políticas: Asegurar que un Customer no pueda ver lo que hace un Admin.

🔵 Fase 3: Lógica de Negocio (El "Core")
[ ] Gestión de Créditos: Crear el servicio para transferir créditos entre Resellers y SubResellers (con validación de saldo).

[ ] CRUD de Usuarios: Endpoints para que un Reseller cree y gestione a sus propios SubResellers o Clientes.

[ ] Registro de Transacciones: Auditoría automática de cada movimiento de saldo.

🟣 Fase 4: Integración con VPS (SSH & Scripts)
[ ] Capa de Conectividad: Implementar Renci.SshNet para que la API pueda hablar con los servidores VPS.

[ ] Ejecución de Comandos: Scripts automáticos para crear usuarios en Linux (en los nodos) y asignar puertos.

[ ] Monitoreo Básico: Verificar si el nodo está online desde el panel.

🔴 Fase 5: Pulido y Despliegue
[ ] Validaciones: Usar FluentValidation para que no entren datos basura a la API.

[ ] Manejo Global de Errores: Que la API devuelva mensajes claros en lugar de errores crudos de C#.

[ ] Migración Final a Postgres: Pasar a la DB de producción en Debian.

[ ] Dockerización: Crear el Dockerfile para subirlo a un servidor real.
