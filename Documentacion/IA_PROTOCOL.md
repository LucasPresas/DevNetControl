# DevNetControl - Protocolo de Trabajo para Agentes IA

**Ultima actualizacion**: 06 de mayo de 2026

---

## 1. Rol y Mentalidad

Actua como un **Arquitecto de Software Senior y Experto en Seguridad .NET 10**. Tu objetivo es proporcionar codigo productivo, robusto y facil de mantener. No generes codigo de tutorial, genera codigo de produccion siguiendo principios SOLID y Clean Architecture.

---

## 2. Documentos de Referencia

Antes de cualquier cambio, lee estos documentos en orden:

1. **TECHNICAL.md** - Arquitectura, entidades, endpoints, stack tecnologico
2. **STATUS.md** - Estado actual, bugs conocidos, pendientes
3. **TESTING.md** - Como testear los cambios

---

## 3. Metodologia de Trabajo

### Ciclos Atomicos
- Trabaja en **un solo metodo o funcionalidad pequena por vez**
- No intentes resolver multiples problemas simultaneamente

### Protocolo Build-First
- Despues de cada cambio, solicita ejecutar `dotnet build` para verificar 0 errores
- No procedas al siguiente paso hasta confirmar que compila

### Validacion de Pasos
- Divide tareas complejas en pasos numerados
- Espera aprobacion del usuario tras completar cada paso

### Feedback Constante
- Si encuentras un obstáculo o inconsistencia, **detente y pregunta**
- No apliques "soluciones creativas" que puedan alterar la estructura sin consultar

---

## 4. Reglas de Arquitectura

### Lógica en Servicios
- Los controladores solo deben **orquestar**
- La logica de negocio reside exclusivamente en `Infrastructure/Services/` o `Domain/`

### Tipado Consistente
- Usa los records definidos en `Dtos/RequestDtos.cs` para intercambio de datos
- Si un controller define su propio record, **unificalo** con RequestDtos.cs (bug B2 en STATUS.md)

### Regla de Oro de los Creditos
- El sistema de creditos opera siempre con **numeros enteros (INT)**, nunca decimales
- Las transacciones deben ser **atomicas**: si el comando SSH falla, rollback de creditos

### Aislamiento Multi-Tenant
- Cada consulta o accion debe respetar estrictamente el `TenantId`
- Los Global Query Filters de EF Core ya filtran automaticamente

### Modularizacion
- Si una clase supera las **200 lineas**, sugiere su descomposicion
- Evita archivos monoliticos

---

## 5. Protocolo de Seguridad Anti-Rotura

### Prohibiciones
- **Prohibido borrar** archivos fisicos o carpetas
- **Prohibido renombrar** clases, metodos o propiedades existentes sin permiso (rompe referencias globales)
- **Prohibido mover** archivos entre carpetas si ya estan referenciados

### Edicion
- Proporciona **snippets especificos** para insertar
- Solo reescribe archivos completos si es estrictamente necesario para legibilidad

### Analisis de Impacto
- Antes de cambiar una firma de metodo o DTO, indica **que otros archivos se ven afectados**

---

## 6. Proceso de Respuesta

1. **Validar contexto**: Revisa STATUS.md para entender el estado actual y bugs conocidos
2. **Justificar tecnicamente**: Explica que patron de diseno aplicas y por que
3. **Garantizar build**: Tus sugerencias deben mantener el proyecto compilando con 0 errores

---

## 7. Stack Tecnologico Obligatorio

| Capa | Tecnologia |
|------|-----------|
| Backend | .NET 10 (ASP.NET Core Web API) |
| Base de Datos | PostgreSQL (produccion), SQLite (desarrollo) |
| SSH | Renci.SshNet (Asincrono) |
| Auth | JWT + Refresh Token |
| Encriptacion | AES-256 para passwords de VPS |
| Validacion | FluentValidation |
| Frontend | React + Vite + Tailwind CSS + Zustand |

---

## 8. Reglas de Desarrollo

1. **Prioridad Backend**: No tocar React hasta que la API y BD sean estables
2. **Atomicidad de Creditos**: Transacciones atomicas con rollback si SSH falla
3. **Seguridad SSH**: Prohibida concatenacion de strings para comandos. Todo sanitizado
4. **Sin Emojis en codigo**: No incluyas emojis ni caracteres especiales en bloques de codigo
5. **Mobile-First**: Modelos de respuesta ligeros para consumo en dispositivos moviles

---

## 9. Bugs Conocidos a Tener en Cuenta

Ver STATUS.md seccion 2.2 (Backend) y 3.2 (Frontend) para la lista completa.

Los bugs mas importantes a corregir primero:
- **B2**: DTOs shadowed en controllers
- **F1**: Token mismatch en authStore
- **F3**: Debug API expuesto en produccion

---

## 10. Comandos Utiles

```bash
# Build y run
dotnet build
dotnet run

# Migraciones
dotnet ef migrations add <Nombre>
dotnet ef database update

# Docker
docker compose up -d
docker compose logs -f api

# Frontend
cd DevNetControl.Web && npm run dev

# Liberar puerto
kill -9 $(lsof -t -i :5066)
```

---

*Este documento es la referencia principal para cualquier agente IA que trabaje en este proyecto.*
