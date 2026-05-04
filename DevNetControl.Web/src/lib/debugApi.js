/**
 * 🧪 DevNetControl Debug API Testing Tool
 * Uso en la consola del navegador:
 * 1. Abre DevTools (F12)
 * 2. Ve a la pestaña "Console"
 * 3. Copia y pega los comandos de prueba
 */

import api from './api'

// ===== UTILIDADES =====

const DEBUG = {
  
  // Test directo de bulk delete usuarios
  async testBulkDeleteUsers(userIds) {
    console.group('🧪 TEST: Bulk Delete Usuarios')
    console.log('📤 Enviando:', { UserIds: userIds })
    try {
      const response = await api.post('/user/bulk/delete', { UserIds: userIds })
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Test directo de bulk delete planes
  async testBulkDeletePlans(planIds) {
    console.group('🧪 TEST: Bulk Delete Planes')
    console.log('📤 Enviando:', { PlanIds: planIds })
    try {
      const response = await api.post('/plan/bulk-delete', { PlanIds: planIds })
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Test directo de bulk delete nodos
  async testBulkDeleteNodes(nodeIds) {
    console.group('🧪 TEST: Bulk Delete Nodos')
    console.log('📤 Enviando:', { NodeIds: nodeIds })
    try {
      const response = await api.post('/vpsnode/bulk-delete', { NodeIds: nodeIds })
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Test directo de single delete usuario
  async testDeleteUser(userId) {
    console.group('🧪 TEST: Delete Usuario')
    console.log('📤 Eliminando:', userId)
    try {
      const response = await api.delete(`/user/${userId}`)
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Test directo de single delete plan
  async testDeletePlan(planId) {
    console.group('🧪 TEST: Delete Plan')
    console.log('📤 Eliminando:', planId)
    try {
      const response = await api.delete(`/plan/${planId}`)
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Test directo de single delete nodo
  async testDeleteNode(nodeId) {
    console.group('🧪 TEST: Delete Nodo')
    console.log('📤 Eliminando:', nodeId)
    try {
      const response = await api.delete(`/vpsnode/${nodeId}`)
      console.log('✅ Respuesta:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    } finally {
      console.groupEnd()
    }
  },

  // Obtener lista de usuarios para test
  async getUsers() {
    console.log('📥 Obteniendo usuarios...')
    try {
      const response = await api.get('/admin/users')
      console.log('✅ Usuarios disponibles:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    }
  },

  // Obtener lista de planes para test
  async getPlans() {
    console.log('📥 Obteniendo planes...')
    try {
      const response = await api.get('/plan')
      console.log('✅ Planes disponibles:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    }
  },

  // Obtener lista de nodos para test
  async getNodes() {
    console.log('📥 Obteniendo nodos...')
    try {
      const response = await api.get('/vpsnode')
      console.log('✅ Nodos disponibles:', response.data)
      return response.data
    } catch (err) {
      console.error('❌ Error:', err.response?.data || err.message)
      throw err
    }
  },

  // Mostrar instrucciones
  showHelp() {
    console.log(`
╔════════════════════════════════════════════════════════════════════════╗
║                    🧪 DEBUG API TESTING TOOL                          ║
╚════════════════════════════════════════════════════════════════════════╝

📚 COMANDOS DISPONIBLES:

1️⃣  Listar datos:
   DEBUG.getUsers()          → Obtiene lista de usuarios
   DEBUG.getPlans()          → Obtiene lista de planes
   DEBUG.getNodes()          → Obtiene lista de nodos

2️⃣  Pruebas de DELETE individual:
   DEBUG.testDeleteUser('user-id-uuid')       → Elimina 1 usuario
   DEBUG.testDeletePlan('plan-id-uuid')       → Elimina 1 plan
   DEBUG.testDeleteNode('node-id-uuid')       → Elimina 1 nodo

3️⃣  Pruebas de BULK DELETE:
   DEBUG.testBulkDeleteUsers(['id1', 'id2'])  → Elimina múltiples usuarios
   DEBUG.testBulkDeletePlans(['id1', 'id2'])  → Elimina múltiples planes
   DEBUG.testBulkDeleteNodes(['id1', 'id2'])  → Elimina múltiples nodos

📋 EJEMPLO DE USO:

// Paso 1: Obtener datos
await DEBUG.getUsers()

// Paso 2: Copiar UUIDs de la respuesta (ej: "f47ac10b-58cc-4372-a567-0e02b2c3d479")

// Paso 3: Probar delete
await DEBUG.testBulkDeleteUsers(['f47ac10b-58cc-4372-a567-0e02b2c3d479'])

// Verifica la consola para ver logs detallados 🔍
    `)
  }
}

// Exponer globalmente en la consola
window.DEBUG = DEBUG

// Mostrar ayuda al cargar
console.log('%c🧪 DEBUG Tool cargado. Usa DEBUG.showHelp() para instrucciones', 'color: #00ff00; font-weight: bold; font-size: 14px;')

export default DEBUG
