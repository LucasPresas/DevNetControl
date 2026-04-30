# Contenido del archivo Markdown para el usuario
markdown_content = """# Especificación de Arquitectura SaaS Multi-Tenant - DevNet Control

Este documento detalla la transición del sistema **DevNet Control** hacia un modelo **SaaS (Software as a Service)** con arquitectura Multi-Inquilino (Multi-Tenancy). El objetivo es permitir que múltiples clientes (Admins) operen su propio negocio de VPN de forma aislada sobre la misma plataforma.

---

## 1. Estructura Jerárquica del Sistema

El sistema se organiza en cuatro niveles de autoridad:

1.  **Super Admin (Dueño de la Plataforma):**
    * Control total. Gestión de **Tenants** (Clientes como Lurzavic, Cliente2).
    * Monitoreo de métricas globales, planes y facturación.
2.  **SaaS Admin (Inquilino / Tenant):**
    * Dueño de su marca (White Label).
    * Añade y gestiona sus propios **Nodos VPS** (Infraestructura propia).
    * Gestiona su propia red de ventas (Resellers/Sub-resellers).
3.  **Reseller / Sub-Reseller:**
    * Entidades de venta dentro de un Tenant. Distribuyen créditos.
4.  **Customer (Usuario Final):**
    * Consumidor final del servicio VPN en los nodos de su respectivo Tenant.

---

## 2. Pilares Técnicos del Multi-Tenancy

### A. Aislamiento de Datos (Discriminador por Columna)
Se utilizará una base de datos compartida donde cada tabla crítica (`Users`, `VpsNodes`, `Transactions`) incluirá una columna `TenantId`.
* **Filtros Globales:** Se implementará un *Global Query Filter* en EF Core para asegurar que un Tenant nunca vea datos de otro.
* **Seguridad:** El `TenantId` se extraerá directamente de los Claims del JWT tras el login.

### B. Aislamiento de Infraestructura (SSH Service)
* Los servidores VPS son propiedad de cada Tenant.
* El `SshService` gestionará la conexión solo a los nodos que correspondan al `TenantId` del usuario activo.
* Las contraseñas de los nodos se almacenan cifradas con AES-256 vinculadas al ID del inquilino.

---

## 3. Cambios en el Modelo de Dominio (C#)

```csharp
public class Tenant {
    public Guid Id { get; set; }
    public string Name { get; set; } // Ej: "Lurzavic Services"
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Todas las entidades existentes se actualizan:
public class VpsNode {
    public Guid Id { get; set; }
    public Guid TenantId { get; set; } // FK hacia el inquilino
    // ... resto de propiedades
}