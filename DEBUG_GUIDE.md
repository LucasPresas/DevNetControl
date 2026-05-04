# 🧪 Guía de Debug - Delete Buttons

## ✅ Cambios Realizados

### 1. **API Logging (api.js)**
Se agregaron interceptors que loguean TODAS las requests/responses en la consola:
- ✅ URL del endpoint
- ✅ Payload enviado
- ✅ Respuesta del servidor
- ✅ Errores con detalles HTTP

### 2. **Component Logging (Users, Resellers, Plans, Nodes)**
Se agregó logging detallado en cada función `handleBulkDelete()`:
- ✅ IDs seleccionados
- ✅ Confirmación del usuario
- ✅ Payload enviado
- ✅ Respuesta exitosa o error

### 3. **Testing Tool (debugApi.js)**
Se creó una herramienta de prueba interactiva accesible desde la consola

---

## 📋 Cómo Usar

### **PASO 1: Abre DevTools**
```
F12  (o Click derecho → Inspeccionar → Console)
```

### **PASO 2: Escribe en la Consola**
```javascript
DEBUG.showHelp()
```
Verás un menú con todos los comandos disponibles.

---

## 🔧 Comandos de Prueba

### **A) Listar datos disponibles**
```javascript
// Obtener usuarios (para ver sus IDs)
await DEBUG.getUsers()

// Obtener planes (para ver sus IDs)
await DEBUG.getPlans()

// Obtener nodos (para ver sus IDs)
await DEBUG.getNodes()
```

**Resultado:** Verás un array con todos los objetos + sus `id` fields.

---

### **B) Prueba Individual de DELETE**

Copia un UUID de la respuesta anterior y usa:

```javascript
// Eliminar 1 usuario (reemplaza el UUID)
await DEBUG.testDeleteUser('f47ac10b-58cc-4372-a567-0e02b2c3d479')

// Eliminar 1 plan
await DEBUG.testDeletePlan('f47ac10b-58cc-4372-a567-0e02b2c3d479')

// Eliminar 1 nodo
await DEBUG.testDeleteNode('f47ac10b-58cc-4372-a567-0e02b2c3d479')
```

---

### **C) Prueba de BULK DELETE**

```javascript
// Eliminar múltiples usuarios (array de UUIDs)
await DEBUG.testBulkDeleteUsers([
  'id-usuario-1',
  'id-usuario-2',
  'id-usuario-3'
])

// Eliminar múltiples planes
await DEBUG.testBulkDeletePlans(['id-plan-1', 'id-plan-2'])

// Eliminar múltiples nodos
await DEBUG.testBulkDeleteNodes(['id-nodo-1', 'id-nodo-2'])
```

---

## 📊 Cómo Interpretar los Logs

### ✅ **Si funciona (Éxito)**
```
🚀 API REQUEST: POST /api/user/bulk/delete
📦 Payload: {UserIds: ['id1', 'id2']}
🔑 Auth: Bearer token present

✅ API RESPONSE: 200 POST /api/user/bulk/delete
📋 Data: {message: "Proceso completado: 2 eliminados, 0 fallidos.", ...}
```

### ❌ **Si falla (Error)**
```
🚀 API REQUEST: POST /api/user/bulk/delete
📦 Payload: {UserIds: ['id1', 'id2']}

❌ API ERROR: 400 POST /api/user/bulk/delete
⚠️ Error Details: {
  status: 400,
  statusText: "Bad Request",
  data: {message: "El usuario tiene vendedores a cargo..."}
}
```

**Qué hacer:**
1. Lee el `data.message` - te dice exactamente qué falló
2. Si es "400 Bad Request" → El payload está mal formado
3. Si es "401 Unauthorized" → Token expirado
4. Si es "500" → Error del servidor

---

## 🔍 Qué Buscar

### **Posibles Problemas & Soluciones**

| Síntoma | Probable Causa | Verificar |
|---------|----------------|----------|
| `400 Bad Request` | Parámetros mal nombrados | Que sea `UserIds`, `PlanIds`, no camelCase |
| `401 Unauthorized` | Token expirado/inválido | Token en LocalStorage |
| `403 Forbidden` | Sin permisos suficientes | Rol del usuario (debe ser Admin) |
| `404 Not Found` | Endpoint no existe o cambió | Que la URL sea exacta |
| `500 Internal Server Error` | Error en backend | Ver logs del servidor .NET |
| Sin logs en consola | JS no se ejecutó | Que los cambios estén en la build |

---

## 🚀 Flujo Completo de Testing

```javascript
// 1. Ver qué hay disponible
await DEBUG.getUsers()

// 2. Copiar un ID de la respuesta (ej: "abc-123-def")

// 3. Probar delete individual
await DEBUG.testDeleteUser('abc-123-def')

// 4. Si funciona → probar UI desde el navegador
// 5. Si no funciona → revisar error en consola

// 6. Si sigue sin funcionar → probar bulk
await DEBUG.testBulkDeleteUsers(['abc-123-def', 'xyz-789-ghi'])
```

---

## 📝 Reporte de Bug

Si aún no funciona, **copia y pega TODO el contenido de la consola** y envíame:

1. La salida de `DEBUG.getUsers()` (para ver estructura)
2. La salida del intento de delete
3. El error HTTP exacto (status + message)

Esto me dará toda la información para diagnosticar.

---

## ✨ Ejemplo Completo

```javascript
// Abre la consola (F12 → Console)

// 1. Obtén usuarios
const users = await DEBUG.getUsers()
// Response: [{id: "123", userName: "user1"}, ...]

// 2. Prueba delete sobre uno real
await DEBUG.testDeleteUser("123")
// Si funciona: ✅ API RESPONSE: 200
// Si error: ❌ API ERROR: 400/401/500 + mensaje

// 3. Revisa qué pasó en los logs
```

¡Ahora ejecuta los comandos y comparte los logs que ves! 🔍
