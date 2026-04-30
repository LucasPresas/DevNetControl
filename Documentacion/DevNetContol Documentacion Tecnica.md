# Prompt para Panel de Gestión VPS

Quiero diseñar un panel web de gestión para múltiples VPS que ofrecen conexión por SSH.  
El panel se monta en un VPS central (propio) y debe permitir a los "admins" integrar sus VPS para gestionar usuarios sin necesidad de usar la terminal.

## Stack tecnológico
- **Backend:** ASP.NET Core 10 (API REST + módulo SSH).
- **Frontend:** React (SPA moderna, responsive, con soporte para Dark Mode).
- **Base de datos:** PostgreSQL.
- **Seguridad:** HTTPS obligatorio, autenticación con JWT, roles (admin, reseller, usuario)
- ** Diseño: 100% Responsive, mobile First.

## Características principales
- Los admins pueden añadir uno o varios VPS y asignarlos a sus resellers.
- Los resellers pueden crear usuarios finales y otros resellers.
- Los usuarios pueden ser creados en el VPS que el reseller/admin elija, con opciones de:
  - Usuario/contraseña.
  - Planes de servicio (ej. mensual, semanal, etc.).
  - Extender servicio (ej. cargar un mes más).
  - Editar credenciales.
- Creación de usuarios de prueba: válidos por X minutos (ej. 180 min) con límite máximo de horas.
- Dashboard para admins:
  - Estado de todos los VPS integrados.
  - Recursos consumidos (conexiones, usuarios activos, expirados, etc.).
- Dashboard para resellers:
  - Gestión de sus usuarios y sub‑resellers.
  - Estadísticas básicas de uso.

## Sistema de créditos
- El admin asigna créditos (moneda virtual) a sus resellers.
- Los resellers consumen créditos para crear planes/usuarios (ejemplo: 1 crédito = 1 conexión SSH/VPN).
- Los resellers pueden asignar créditos a sus sub‑resellers, creando un esquema jerárquico de negocio.
- El panel debe llevar control de créditos disponibles, consumidos y transferidos.

## Notas de arquitectura
- El VPS central solo envía peticiones SSH (ej. crear usuario, modificar expiración).
- El tráfico de los usuarios finales se consume en los VPS de los clientes, por lo que la carga en el VPS central es baja.
- Se recomienda separar backend (API REST), frontend (React SPA), y base de datos (PostgreSQL) para mayor escalabilidad.
- El módulo SSH puede implementarse en .NET usando librerías como Renci.SshNet.

## Requisitos visuales
- Frontend React debe ser 100% responsive Movile First (adaptado a celulares).
- Opción Dark Mode.
- Evitar frameworks JS pesados adicionales para mantener la carga mínima en el VPS central.

# Opcional: 
- Poder crear con todo esto luego una WPA para celulares