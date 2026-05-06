# 🧪 Plan de Testing - DevNetControl

## 1. 🔍 Diagnóstico de Problemas Actuales

### ✅ Problema de Eliminación - RESUELTO (05 mayo 2026)
- **Síntoma anterior**: "El usuario canceló la operación" o similares en debug
- **Causa Raíz**: Discrepancia entre frontend y backend en endpoints bulk
- **Solución aplicada**: 
  - Frontend ahora envía payload correcto en camelCase (`userIds`)
  - Backend recibe correctamente `BulkDeleteRequest` con `List<Guid> UserIds`
  - Endpoints individuales y bulk funcionando correctamente
  - Botones de eliminación implementados en Users, Plans, Nodes y Resellers

### Estado Actual de Eliminación en Frontend
- ✅ **Users**: Eliminación individual y bulk implementada
- ✅ **Planes**: Botón de eliminación en `Plans.jsx` funcionando
- ✅ **Resellers**: Botones de eliminación implementados en `Resellers.jsx`
- ✅ **Nodos**: Eliminación implementada en `Nodes.jsx`

---

## 2. 🎯 Estrategia de Testing

### A. Testing Manual con Herramientas Existentes
1. **Debug Tool** (`debugApi.js`): Ya disponible en consola del navegador
   - Comandos: `DEBUG.testDeleteUser(id)`, `DEBUG.testBulkDeleteUsers([ids])`
   - Útil para aislar problemas de API vs UI

2. **Swagger UI**: `http://localhost:5066/swagger`
   - Test de endpoints individuales
   - Verificación de respuestas HTTP y schemas

3. **Postman/Insomnia**: Para testing exhaustivo de API
   - Colección completa de endpoints
   - Pruebas automatizadas con scripts

### B. Testing Automatizado (NUEVO)

#### Opción 1: Tests de Integración con xUnit (Recomendado)
- **Framework**: xUnit (nativo .NET)
- **Librería HTTP**: Microsoft.AspNetCore.Mvc.Testing
- **Ventajas**: Pruebas contra la API real, cobertura de middleware, auth, BD en memoria

#### Opción 2: Tests E2E con Playwright/Cypress (Frontend)
- **Framework**: Playwright (Microsoft) o Cypress
- **Ventajas**: Simula usuario real, prueba flujos completos UI + API

---

## 3. 📋 Test Suite - Checklist

### 🔐 Auth & Seguridad
- [ ] Login con credenciales válidas → 200 OK + JWT
- [ ] Login con credenciales inválidas → 401 Unauthorized
- [ ] Acceso a endpoint protegido sin token → 401
- [ ] Acceso con rol insuficiente → 403 Forbidden
- [ ] Refresh token válido → nuevo JWT
- [ ] Rate limiting en `/api/auth/login` (múltiples intentos)

### 👥 Gestión de Usuarios (CRUD)
- [ ] Crear usuario (POST `/api/user/create`)
- [ ] Crear usuario con plan y nodo
- [ ] Crear usuario trial (POST `/api/user/create-trial`)
- [ ] Listar subusuarios (GET `/api/user/my-subusers`)
- [ ] Obtener perfil (GET `/api/user/me`)
- [ ] Actualizar usuario (PUT `/api/admin/users/{id}`)
- [ ] **Eliminar usuario individual** (DELETE `/api/admin/users/{id}`)
- [ ] **Eliminar usuarios bulk** (POST `/api/user/bulk/delete`)
- [ ] Eliminar usuario con subordinados → 400 Bad Request

### 💰 Créditos
- [ ] Transferir créditos (POST `/api/credit/transfer`)
- [ ] Consultar saldo (GET `/api/credit/balance`)
- [ ] Agregar créditos (POST `/api/admin/users/{id}/add-credits`)
- [ ] Créditos insuficientes → 400 Bad Request
- [ ] Extender servicio (POST `/api/user/{id}/extend-service`)

### 🖥️ Nodos VPS
- [ ] Crear nodo (POST `/api/vpsnode`)
- [ ] Listar nodos accesibles (GET `/api/vpsnode`)
- [ ] Test conexión SSH (POST `/api/vpsnode/{id}/test-connection`)
- [ ] Métricas del sistema (GET `/api/vpsnode/{id}/metrics`)
- [ ] **Eliminar nodo** (DELETE `/api/vpsnode/{id}`)
- [ ] Health check (GET `/api/monitor/node-health`)

### 📦 Planes
- [ ] Listar planes (GET `/api/plan`)
- [ ] Crear plan (POST `/api/plan`)
- [ ] **Eliminar plan** (DELETE `/api/plan/{id}`)
- [ ] Eliminar plan en uso → 400 Bad Request

### 📊 Actividades y Auditoría
- [ ] Listar actividades (GET `/api/activity`)
- [ ] Filtrar por usuario/tipo/fechas
- [ ] Estadísticas (GET `/api/activity/stats`)
- [ ] Historial de créditos (GET `/api/audit/history`)

### 🔔 Notificaciones
- [ ] Listar mis notificaciones (GET `/api/notification/mine`)
- [ ] Marcar como leída (POST `/api/notification/mark-read/{id}`)
- [ ] Contador no leídas (GET `/api/notification/unread-count`)

### 🏢 Multi-Tenancy
- [ ] Usuario no ve datos de otro tenant
- [ ] SuperAdmin puede gestionar tenants
- [ ] Aislamiento en todas las queries (Global Filter)

### 🌐 Frontend (UI)
- [ ] Login correcto → redirección a dashboard
- [ ] Login fallido → mensaje de error
- [ ] Crear usuario desde UI → éxito
- [ ] **Eliminar usuario individual desde UI** → éxito
- [ ] **Eliminar usuarios bulk desde UI** → éxito
- [ ] Dark mode toggle funciona
- [ ] Logout limpia token
- [ ] Interceptores manejan 401 (redirect a login)

---

## 4. 🛠️ Implementación de Tests Automatizados

### Paso 1: Configuración de xUnit (Backend)

```bash
# Desde la carpeta DevNetControl.Api
dotnet add package xunit
dotnet add package xunit.runner.visualstudio
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.InMemory
```

### Paso 2: Estructura de Tests

```
DevNetControl.Tests/
├── IntegrationTests/
│   ├── AuthTests.cs
│   ├── UserTests.cs
│   ├── CreditTests.cs
│   ├── NodeTests.cs
│   └── PlanTests.cs
├── UnitTests/
│   ├── CreditServiceTests.cs
│   ├── UserProvisioningServiceTests.cs
│   └── SshUserManagerTests.cs
├── Helpers/
│   ├── TestFixture.cs          # Configuración común
│   ├── AuthTestHelper.cs       # Tokens JWT para tests
│   └── MockData.cs             # Datos de prueba
└── DevNetControl.Tests.csproj
```

### Paso 3: Ejemplo de Test de Integración (Auth)

```csharp
public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    
    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Usar BD en memoria para tests
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }
    
    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        // Arrange
        var loginRequest = new { UserName = "admin", Password = "admin123" };
        
        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", content);
    }
}
```

### Paso 4: Ejemplo de Test de Usuario (Bulk Delete)

```csharp
[Fact]
public async Task BulkDelete_WithValidIds_ReturnsOk()
{
    // Arrange: Crear usuarios de prueba en BD en memoria
    var userIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
    var request = new { UserIds = userIds };
    
    // Act
    var response = await _client.PostAsJsonAsync("/api/user/bulk/delete", request);
    
    // Assert
    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    var result = await response.Content.ReadFromJsonAsync<BulkDeleteResponse>();
    Assert.Equal(2, result.SuccessCount);
}
```

---

## 5. 🚀 Pruebas E2E con Playwright (Frontend)

### Instalación
```bash
cd DevNetControl.Web
npm init playwright@latest
# Seleccionar: TypeScript, Tests en /tests, Add GitHub Actions (no)
```

### Estructura
```
DevNetControl.Web/tests/
├── auth.spec.ts           # Login/logout
├── users.spec.ts          # CRUD usuarios
├── credits.spec.ts        # Gestión de créditos
└── helpers/
    └── auth.helper.ts    # Funciones reutilizables
```

### Ejemplo: Test de Eliminación de Usuario

```typescript
// tests/users.spec.ts
import { test, expect } from '@playwright/test';

test.describe('User Deletion', () => {
  test.beforeEach(async ({ page }) => {
    // Login como admin
    await page.goto('/login');
    await page.fill('[name="userName"]', 'admin');
    await page.fill('[name="password"]', 'admin123');
    await page.click('button[type="submit"]');
    await page.waitForURL('/dashboard');
  });

  test('Delete single user', async ({ page }) => {
    await page.goto('/users');
    
    // Seleccionar primer usuario
    await page.check('table tbody tr:first-child input[type="checkbox"]');
    
    // Click eliminar
    await page.click('button:has-text("Eliminar")');
    
    // Confirmar en dialog
    page.on('dialog', dialog => dialog.accept());
    
    // Verificar mensaje de éxito
    await expect(page.locator('.bg-green-500')).toContainText('eliminado');
  });
});
```

---

## 6. 📝 Script de Testing Manual Rápido

Crear archivo `test-api.sh` para tests rápidos desde terminal:

```bash
#!/bin/bash
BASE_URL="http://localhost:5066/api"

echo "🧪 Iniciando tests manuales..."
echo ""

# 1. Login
echo "1. Testing Login..."
LOGIN_RESPONSE=$(curl -s -X POST "$BASE_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"admin123"}')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')
echo "✓ Token obtenido: ${TOKEN:0:20}..."

# 2. Crear usuario
echo "2. Testing Create User..."
CREATE_RESPONSE=$(curl -s -X POST "$BASE_URL/user/create" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"userName":"testuser1","password":"Test123!","planId":"GUID_HERE"}')

echo $CREATE_RESPONSE | jq

# 3. Listar usuarios
echo "3. Testing List Users..."
curl -s -X GET "$BASE_URL/user/my-subusers" \
  -H "Authorization: Bearer $TOKEN" | jq

# 4. Eliminar usuario (cambiar ID)
echo "4. Testing Delete User..."
curl -s -X DELETE "$BASE_URL/admin/users/GUID_HERE" \
  -H "Authorization: Bearer $TOKEN" | jq

echo ""
echo "✅ Tests completados"
```

---

## 7. 🔧 Corrección de Bugs Identificados

### ✅ Bug 1: Eliminación Bulk en Frontend - RESUELTO
**Archivo**: `DevNetControl.Web/src/pages/Users.jsx`
**Estado anterior**: El payload se enviaba incorrectamente
**Solución aplicada**: 
- Frontend envía correctamente `{ userIds: selectedIds }` en camelCase
- Backend recibe y procesa correctamente via `BulkDeleteRequest`
- Configuración de `PropertyNamingPolicy` en `Program.cs:22` funcionando correctamente

### ✅ Bug 2: Eliminación Individual Implementada en UI
**Archivo**: `Users.jsx`
**Estado**: Botón de eliminación individual implementado en la tabla de usuarios
```jsx
// Implementado en Users.jsx
<button
  onClick={() => handleDeleteUser(u.id)}
  className="btn btn-sm btn-secondary text-red-400"
  title="Eliminar usuario"
>
  <Trash2 className="w-3.5 h-3.5" />
</button>
```

### ✅ Bug 3: Endpoints de Plans, Nodes y Resellers con UI de Eliminación
**Archivos**: `Plans.jsx`, `Nodes.jsx`, `Resellers.jsx`
**Estado**: Botones de eliminación implementados y funcionando correctamente

---

## 8. ✅ Próximos Pasos Recomendados

1. **Inmediato**: Corregir bugs de eliminación en frontend (camelCase, endpoints faltantes)
2. **Corto plazo**: Implementar tests de integración con xUnit para backend
3. **Mediano plazo**: Implementar tests E2E con Playwright para frontend
4. **Largo plazo**: Configurar CI/CD con GitHub Actions para ejecutar tests automáticamente

---

*Creado: 04 de mayo de 2026 - Basado en análisis de código y documentación existente*
