# 🛡️ DevNet Control - Estado del Proyecto & Roadmap SaaS

Este documento resume la arquitectura aplicada hasta la fecha y las reglas de negocio implementadas para el modelo SaaS Multi-Tenant.

---

## 1. 🏗️ Arquitectura Aplicada (Estado Actual)

### A. Core SaaS & Aislamiento
* **Multi-Tenancy:** Implementada la entidad `Tenant`. Cada cliente (ej: Lurzavic) es un inquilino independiente.
* **Aislamiento de Datos:** Aplicado `Global Query Filter` en `ApplicationDbContext`. Las consultas a `Users` y `VpsNodes` se filtran automáticamente por `TenantId`.
* **Seguridad JWT:** El `TenantId` se inyecta como un Claim en el token JWT, permitiendo al backend identificar el contexto del inquilino en cada petición.

### B. Infraestructura & Seguridad
* **Inyección de Dependencias:** Corregido el error de Scope del `DbContextFactory`. Ahora el backend es estable en entornos Windows y Linux.
* **Encryption Service:** Implementado servicio de cifrado AES-256 para proteger las credenciales (Root Password) de los nodos VPS en la base de datos.
* **SSH Service:** Estructura base para ejecución de comandos remotos en servidores Linux.

### C. Frontend (React + Zustand)
* **Estado Global:** Implementado `authStore` con Zustand para persistir token y datos del Tenant (White Label ready).
* **Interceptores:** Axios configurado para manejar expiración de sesiones y redirección al login.

---

## 2. 🟢 Fase 7 - Completada

### A. Sistema de Créditos (Implementado)
* **Regla de Consumo:** 1 Crédito = 1 Dispositivo activo x 30 días de servicio.
* **Configurable por Tenant:** `CreditsPerDevice` en la entidad Tenant (default: 1).
* **Jerarquía de Saldo:**
    * **Admins:** No consumen créditos al crear usuarios.
    * **Resellers/Subresellers:** Se descuentan créditos automaticamente (`MaxDevices * CreditsPerDevice`).
* **Validacion:** El sistema impide la creacion si `SaldoReseller < CreditosNecesarios`.
* **Reembolso automatico:** Si el comando SSH falla, los creditos son devueltos al creador.
* **Extension de servicio:** Endpoint `POST /api/user/{id}/extend-service` que cobra creditos segun dias adicionales.

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
* **CreditTransaction:** Agregado `Type` enum (Transfer, ManualAdd, UserCreationCost, ServiceExtension, TrialCreation, NodeCreationCost).

### E. Nuevos Endpoints
| Metodo | Ruta | Descripcion | Auth |
|--------|------|-------------|------|
| POST | `/api/user/create-subuser` | Crea sub-usuario con descuento de creditos (body: `maxDevices`) | SubResellerOrAbove |
| POST | `/api/user/create-trial` | Crea usuario de prueba (0 creditos, expiracion automatica) | SubResellerOrAbove |
| POST | `/api/user/{id}/extend-service` | Extiende servicio de usuario (cobra creditos al padre) | Auth |
| POST | `/api/user/{id}/remove-from-vps` | Elimina usuario del VPS | AdminOnly |

### F. Frontend Actualizado
* **Users.jsx:** Formulario de creacion con campo `maxDevices`, boton para crear usuarios de prueba, lista muestra estado de provision VPS, expiracion, y badge de prueba. Boton "Extender" en usuarios pagos.
* **Credits.jsx:** Historial muestra tipo de transaccion con iconos y labels (Transferencia, Carga manual, Creacion de usuario, Extension, etc.).
* **AdminPanel.jsx:** Muestra badges de VPS y prueba, info de dispositivos, expiracion, y conteo de nodos.

---

## 3. 📋 Pendiente (Futuras Mejoras)

* Configuracion de Tenant settings desde SuperAdminPanel (editar TrialMaxHours, CreditsPerDevice).
* Endpoint para listar usuarios expirados en VPS y accion de limpieza manual.
* Dashboard con estadisticas de usuarios activos/expirados/prueba.
* WPA/PWA para celulares.
* Sistema de notificaciones (email/webhook) al expirar usuarios.

---
*Ultima actualizacion: 30 de abril de 2026 - Fase 7 completada (Creditos + Trial Users + SSH + BackgroundService)*
