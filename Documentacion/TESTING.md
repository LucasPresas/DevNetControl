# DevNetControl - Plan de Testing

**Ultima actualizacion**: 07 de mayo de 2026

---

## 1. Herramientas Disponibles

### Backend
- **Swagger UI**: `http://localhost:5066/swagger` - Test visual de endpoints
- **Debug HTML**: `http://localhost:5066/` - Boton simple para test de DB
- **Console Debug**: `window.DEBUG` en el navegador (solo dev) - Funciones para test rapido

### Frontend
- **Debug Tool**: Abrir consola del navegador (`F12`) y usar `DEBUG.showHelp()`
- **Network tab**: Ver requests/respuestas en devtools

### Manual
- **cURL / HTTPie**: Testing desde terminal
- **Postman/Insomnia**: Coleccion completa de endpoints

---

## 2. Credenciales para Testing

| Rol | Usuario | Password | Tenant |
|-----|---------|----------|--------|
| SuperAdmin | `superadmin` | `superadmin123` | Platform |
| Admin | `admin` | `admin123` | Default Tenant |

---

## 3. Checklist de Testing Manual

### 3.1 Auth y Seguridad

- [ ] Login con credenciales validas -> 200 OK + JWT + RefreshToken
- [ ] Login con credenciales invalidas -> 401 Unauthorized
- [ ] Login con tenant inactivo -> 401 con mensaje "Tu organizacion esta desactivada"
- [ ] Acceso a endpoint protegido sin token -> 401
- [ ] Acceso con rol insuficiente -> 403 Forbidden
- [ ] Refresh token valido -> nuevo JWT + nuevo RefreshToken
- [ ] Refresh token usado/revocado -> 401
- [ ] Rate limiting en `/api/auth/login` (5 intentos en 60s) -> 429
- [ ] Cambio de password con password actual correcto -> 200
- [ ] Cambio de password con password actual incorrecto -> 400

**cURL de ejemplo:**
```bash
# Login
curl -X POST http://localhost:5066/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"admin123"}'

# Rate limit test (ejecutar 6 veces rapidamente)
for i in {1..6}; do curl -s -o /dev/null -w "%{http_code}\n" -X POST http://localhost:5066/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"wrong"}'; done
```

### 3.2 Gestion de Usuarios (CRUD)

- [ ] Crear usuario (POST `/api/user/create`) con plan y nodo
- [ ] Crear usuario de prueba (POST `/api/user/create-trial`) -> 0 creditos, prefijo `trial_`
- [ ] Listar subusuarios (GET `/api/user/my-subusers`)
- [ ] Obtener perfil (GET `/api/user/me`)
- [ ] Obtener limites del plan (GET `/api/user/me/limits`)
- [ ] Obtener arbol jerarquico (GET `/api/user/me/hierarchy`)
- [ ] Obtener estadisticas dashboard (GET `/api/user/dashboard-stats`)
- [ ] Actualizar usuario (PUT `/api/admin/users/{id}`)
- [ ] Eliminar usuario individual (DELETE `/api/admin/users/{id}`)
- [ ] Eliminar sub-reseller (DELETE `/api/user/{id}/sub-reseller`) -> verifica reembolso de creditos
- [ ] Eliminar usuarios bulk (POST `/api/user/bulk/delete`) con `{ "userIds": ["..."] }`
- [ ] Suspender usuario (POST `/api/user/{id}/suspend`)
- [ ] Toggle suspension (POST `/api/user/{id}/toggle-suspend`)
- [ ] Extender servicio (POST `/api/user/{id}/extend-service`)
- [ ] Agregar conexiones (POST `/api/user/{id}/add-connection`)
- [ ] Renovar plan (POST `/api/user/{id}/renew-plan`)
- [ ] Remover de VPS (POST `/api/user/{id}/remove-from-vps`)
- [ ] Cargar creditos a sub-reseller (POST `/api/user/{id}/add-credits`) -> verifica descuento del padre

**cURL de ejemplo:**
```bash
# Obtener token primero
TOKEN=$(curl -s -X POST http://localhost:5066/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"admin123"}' | jq -r '.accessToken')

# Crear usuario
curl -X POST http://localhost:5066/api/user/create \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"userName":"testuser1","password":"Test123!","planId":"GUID_DEL_PLAN"}'

# Eliminar usuario (reemplazar GUID)
curl -X DELETE http://localhost:5066/api/admin/users/GUID_DEL_USUARIO \
  -H "Authorization: Bearer $TOKEN"
```

### 3.3 Creditos

- [ ] Consultar saldo (GET `/api/credit/balance`)
- [ ] Transferir creditos (POST `/api/credit/transfer`) entre resellers
- [ ] Transferir mas creditos que saldo disponible -> 400
- [ ] Historial de transacciones (GET `/api/credit/history`)
- [ ] Agregar creditos como admin (POST `/api/admin/users/{id}/add-credits`)
- [ ] Crear usuario como reseller -> descuento de creditos correcto
- [ ] Crear usuario como admin -> sin descuento
- [ ] Crear usuario trial -> 0 creditos consumidos
- [ ] Comando SSH falla -> reembolso de creditos automatico
- [ ] Extension de servicio -> cobro correcto segun dias

### 3.4 Planes

- [ ] Listar planes (GET `/api/plan`)
- [ ] Crear plan (POST `/api/plan`) con nombre, duracion, costo, conexiones, dispositivos
- [ ] Crear plan trial (creditCost = 0)
- [ ] Actualizar plan (PUT `/api/plan/{id}`)
- [ ] Toggle activo/inactivo (POST `/api/plan/{id}/toggle`)
- [ ] Eliminar plan (DELETE `/api/plan/{id}`)
- [ ] Eliminar plan en uso -> 400 Bad Request
- [ ] Eliminar planes bulk (POST `/api/plan/bulk-delete`)
- [ ] Mis planes accesibles (GET `/api/plan/my-plans`)

### 3.5 Nodos VPS

- [ ] Crear nodo (POST `/api/vpsnode`) con label, IP, puerto, password
- [ ] Listar nodos accesibles (GET `/api/vpsnode`)
- [ ] Detalle de nodo (GET `/api/vpsnode/{id}`)
- [ ] Actualizar nodo (PUT `/api/vpsnode/{id}`)
- [ ] Eliminar nodo (DELETE `/api/vpsnode/{id}`)
- [ ] Eliminar nodos bulk (POST `/api/vpsnode/bulk-delete`)
- [ ] Test conexion SSH (POST `/api/vpsnode/{id}/test-connection`)
- [ ] Ejecutar comando remoto (POST `/api/vpsnode/{id}/execute`)
- [ ] Metricas del sistema (GET `/api/vpsnode/{id}/metrics`)
- [ ] Health check individual (GET `/api/vpsnode/{id}/health`)
- [ ] Health check de todos los nodos (GET `/api/vpsnode/health`) -> verifica CPU%, RAM%, Disk%, userCount
- [ ] Comando con caracteres peligrosos -> sanitizacion bloquea

**Test de sanitizacion SSH:**
```bash
# Estos comandos deberian ser rechazados o sanitizados
curl -X POST http://localhost:5066/api/vpsnode/ID_NODO/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"command": "ls; rm -rf /"}'

curl -X POST http://localhost:5066/api/vpsnode/ID_NODO/execute \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"command": "echo $(whoami)"}'
```

### 3.6 Multi-Tenancy

- [ ] Usuario de Tenant A no ve datos de Tenant B
- [ ] Crear tenant como SuperAdmin (POST `/api/superadmin/tenants`)
- [ ] Listar tenants (GET `/api/superadmin/tenants`)
- [ ] Toggle tenant activo/inactivo (PUT `/api/superadmin/tenants/{id}/toggle`)
- [ ] Tenant inactivo -> usuarios no pueden login
- [ ] Estadisticas globales (GET `/api/superadmin/stats`)
- [ ] Provisionar nodo a tenant (POST `/api/superadmin/provision-node`)

### 3.7 Auditoria y Logs

- [ ] Listar actividades (GET `/api/activity`) con paginacion y filtros
- [ ] Filtrar por tipo de accion, usuario, fechas
- [ ] Estadisticas de actividades (GET `/api/activity/stats`) -> Admin
- [ ] Resumen de creditos (GET `/api/activity/credits/summary`) -> Admin
- [ ] Historial de transacciones con nombres (GET `/api/audit/history`)
- [ ] Resumen de creditos (GET `/api/audit/history/summary`)
- [ ] Logs de auditoria del sistema (GET `/api/audit/logs`) -> Admin
- [ ] Listar session logs (GET `/api/sessionlog`) con paginacion
- [ ] Logs activos de usuario (GET `/api/sessionlog/user/{userId}/active`)
- [ ] Estadisticas de sesiones (GET `/api/sessionlog/stats`)
- [ ] Limpiar logs antiguos (POST `/api/sessionlog/cleanup`)

### 3.8 Notificaciones

- [ ] Mis notificaciones (GET `/api/notification/mine`) con paginacion
- [ ] Marcar como leida (POST `/api/notification/mark-read/{id}`)
- [ ] Contador no leidas (GET `/api/notification/unread-count`)
- [ ] Alerta de credito bajo se genera automaticamente
- [ ] Alerta de expiracion se genera automaticamente

### 3.9 Acceso a Nodos y Planes

- [ ] Listar accesos a nodos de usuario (GET `/api/nodeaccess/user/{userId}`)
- [ ] Asignar acceso a nodo (POST `/api/nodeaccess`)
- [ ] Revocar acceso a nodo (DELETE `/api/nodeaccess/user/{userId}/node/{nodeId}`)
- [ ] Mis nodos accesibles (GET `/api/nodeaccess/my-nodes`)
- [ ] Planes de usuario (GET `/api/plan/user/{userId}/plans`)
- [ ] Asignar planes a usuario (POST `/api/plan/user/{userId}/plans`)

### 3.10 Operaciones Masivas

- [ ] Bulk create users (POST `/api/user/bulk-create`) con CSV
- [ ] Bulk extend service (POST `/api/user/bulk/extend-service`)
- [ ] Bulk delete users (POST `/api/user/bulk/delete`)
- [ ] Bulk toggle suspend (POST `/api/user/bulk/toggle-suspend`)
- [ ] Bulk delete plans (POST `/api/plan/bulk-delete`)
- [ ] Bulk delete nodes (POST `/api/vpsnode/bulk-delete`)

### 3.11 Monitor

- [ ] Estado de usuario en VPS (GET `/api/monitor/user/{userId}/status`)
- [ ] Forzar cierre de sesiones (POST `/api/monitor/user/{userId}/enforce`)

### 3.12 Frontend (UI)

- [ ] Login correcto -> redireccion a dashboard
- [ ] Login fallido -> mensaje de error
- [ ] Crear usuario desde UI -> exito
- [ ] Eliminar usuario individual desde UI -> exito
- [ ] Eliminar usuarios bulk desde UI -> exito
- [ ] Transferir creditos desde UI -> exito
- [ ] Crear plan desde UI -> exito
- [ ] Crear nodo desde UI -> exito
- [ ] Test SSH desde UI -> muestra resultado
- [ ] Ejecutar comando desde NodeDetail -> muestra output
- [ ] Dark mode funciona correctamente
- [ ] Light mode funciona correctamente (paleta Beige & Crema)
- [ ] Toggle Dark/Light persiste entre sesiones
- [ ] Logout limpia token y redirige a login
- [ ] 401 interceptado -> redirige a login
- [ ] Sidebar muestra secciones correctas segun rol (Reseller no ve Planes/Nodos/Creditos)
- [ ] Balance muestra "Saldo Agotado" cuando credits == 0
- [ ] Badges de trial/VPS en tablas de usuarios
- [ ] Paginacion funciona en Resellers y Logs
- [ ] Modales de edicion funcionan correctamente
- [ ] Filtros de busqueda funcionan en Users, Resellers, Logs
- [ ] Dashboard muestra usuarios online, por vencer, expirados
- [ ] Dashboard muestra nodos con barras de carga (Admin)
- [ ] Logs muestran eliminaciones con detalle de reembolso
- [ ] Toggle Dark/Light mode funciona y persiste

---

## 4. Testing con Debug Tool (Consola del Navegador)

Abrir la app en el navegador (`http://localhost:3000`), loguearse, abrir `F12` y usar:

```javascript
// Ver ayuda
DEBUG.showHelp()

// Test de endpoints
DEBUG.getUsers(token)
DEBUG.getPlans(token)
DEBUG.getNodes(token)

// Test de eliminacion individual
DEBUG.testDeleteUser(token, userId)
DEBUG.testDeletePlan(token, planId)
DEBUG.testDeleteNode(token, nodeId)

// Test de eliminacion bulk
DEBUG.testBulkDeleteUsers(token, [userId1, userId2])
DEBUG.testBulkDeletePlans(token, [planId1, planId2])
DEBUG.testBulkDeleteNodes(token, [nodeId1, nodeId2])
```

---

## 5. Testing Automatizado (Futuro)

### 5.1 Tests Unitarios (xUnit)

**Estructura propuesta:**
```
DevNetControl.Tests/
├── UnitTests/
│   ├── CreditServiceTests.cs
│   ├── UserProvisioningServiceTests.cs
│   ├── SshUserManagerTests.cs
│   ├── SshSanitizerServiceTests.cs
│   └── TokenServiceTests.cs
├── IntegrationTests/
│   ├── AuthTests.cs
│   ├── UserTests.cs
│   ├── CreditTests.cs
│   ├── PlanTests.cs
│   └── NodeTests.cs
├── Helpers/
│   ├── TestFixture.cs
│   └── MockData.cs
└── DevNetControl.Tests.csproj
```

**Paquetes necesarios:**
```bash
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
dotnet add package Moq
```

### 5.2 Ejemplo de Test Unitario (CreditService)

```csharp
public class CreditServiceTests
{
    [Fact]
    public async Task TransferCredits_WithSufficientBalance_DeductsAndAdds()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestTransfer")
            .Options;
        
        using var context = new ApplicationDbContext(options);
        var sourceUser = new User { Id = Guid.NewGuid(), UserName = "source", Credits = 100, TenantId = Guid.NewGuid() };
        var targetUser = new User { Id = Guid.NewGuid(), UserName = "target", Credits = 50, TenantId = Guid.NewGuid() };
        context.Users.AddRange(sourceUser, targetUser);
        await context.SaveChangesAsync();
        
        var service = new CreditService(context, null); // null = no activity log service
        
        // Act
        await service.TransferCreditsAsync(sourceUser.Id, targetUser.Id, 30, "Test");
        
        // Assert
        var updatedSource = await context.Users.FindAsync(sourceUser.Id);
        var updatedTarget = await context.Users.FindAsync(targetUser.Id);
        Assert.Equal(70, updatedSource.Credits);
        Assert.Equal(80, updatedTarget.Credits);
    }
    
    [Fact]
    public async Task TransferCredits_WithInsufficientBalance_ThrowsException()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase("TestInsufficient")
            .Options;
        
        using var context = new ApplicationDbContext(options);
        var sourceUser = new User { Id = Guid.NewGuid(), UserName = "source", Credits = 10, TenantId = Guid.NewGuid() };
        var targetUser = new User { Id = Guid.NewGuid(), UserName = "target", Credits = 50, TenantId = Guid.NewGuid() };
        context.Users.AddRange(sourceUser, targetUser);
        await context.SaveChangesAsync();
        
        var service = new CreditService(context, null);
        
        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.TransferCreditsAsync(sourceUser.Id, targetUser.Id, 50, "Test"));
    }
}
```

### 5.3 Tests E2E (Playwright)

**Instalacion:**
```bash
cd DevNetControl.Web
npm init playwright@latest
```

**Ejemplo de test:**
```typescript
import { test, expect } from '@playwright/test';

test.describe('Auth', () => {
  test('Login con credenciales validas', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[name="userName"]', 'admin');
    await page.fill('[name="password"]', 'admin123');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
    await expect(page).toHaveURL('/dashboard');
  });

  test('Login con credenciales invalidas muestra error', async ({ page }) => {
    await page.goto('/login');
    await page.fill('[name="userName"]', 'admin');
    await page.fill('[name="password"]', 'wrong');
    await page.click('button[type="submit"]');
    await expect(page.locator('.bg-red-500')).toBeVisible();
  });
});
```

---

## 6. Script de Testing Rapido (Terminal)

Crear archivo `test-api.sh` en la raiz del proyecto:

```bash
#!/bin/bash
BASE_URL="http://localhost:5066/api"

echo "=== DevNetControl API Tests ==="
echo ""

# 1. Login
echo "1. Login..."
LOGIN=$(curl -s -X POST "$BASE_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"admin123"}')
TOKEN=$(echo $LOGIN | jq -r '.accessToken')

if [ "$TOKEN" = "null" ] || [ -z "$TOKEN" ]; then
  echo "FAIL: No se pudo obtener token"
  exit 1
fi
echo "PASS: Token obtenido"

# 2. Profile
echo "2. Profile..."
PROFILE=$(curl -s -X GET "$BASE_URL/user/me" \
  -H "Authorization: Bearer $TOKEN")
echo "PASS: $(echo $PROFILE | jq -r '.userName')"

# 3. Balance
echo "3. Credit Balance..."
BALANCE=$(curl -s -X GET "$BASE_URL/credit/balance" \
  -H "Authorization: Bearer $TOKEN")
echo "PASS: $(echo $BALANCE | jq -r '.balance')"

# 4. Plans
echo "4. List Plans..."
PLANS=$(curl -s -X GET "$BASE_URL/plan" \
  -H "Authorization: Bearer $TOKEN")
PLAN_COUNT=$(echo $PLANS | jq 'length')
echo "PASS: $PLAN_COUNT planes encontrados"

# 5. Nodes
echo "5. List Nodes..."
NODES=$(curl -s -X GET "$BASE_URL/vpsnode" \
  -H "Authorization: Bearer $TOKEN")
NODE_COUNT=$(echo $NODES | jq 'length')
echo "PASS: $NODE_COUNT nodos encontrados"

# 6. Activities
echo "6. Recent Activities..."
ACTIVITIES=$(curl -s -X GET "$BASE_URL/activity/recent?limit=5" \
  -H "Authorization: Bearer $TOKEN")
ACT_COUNT=$(echo $ACTIVITIES | jq 'length')
echo "PASS: $ACT_COUNT actividades recientes"

echo ""
echo "=== Todos los tests pasaron ==="
```

Ejecutar con:
```bash
chmod +x test-api.sh
./test-api.sh
```

---

## 7. Criterios de Aceptacion

Un feature se considera "completado" cuando:

1. **Backend**: Endpoint responde correctamente con datos validos y maneja errores
2. **Frontend**: UI funciona, muestra exitos/errores, no crashea
3. **Multi-tenant**: El feature respeta el aislamiento por TenantId
4. **Roles**: El feature respeta las politicas de autorizacion
5. **Credits**: Si consume creditos, el descuento es correcto y se registra la transaccion
6. **SSH**: Si involucra SSH, los comandos estan sanitizados y hay rollback en fallo
7. **Build**: `dotnet build` sin errores
8. **Manual test**: Al menos un test manual del checklist de arriba paso

---

*Este documento debe ejecutarse periodicamente para verificar que el proyecto funciona correctamente despues de cambios.*
