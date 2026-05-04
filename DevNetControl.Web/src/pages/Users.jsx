import { useEffect, useState } from 'react'
import api from '../lib/api'
import {
  UserPlus, Loader2, Search, Filter, Clock, Server,
  Calendar, Check, X, ChevronLeft, ChevronRight, Users as UsersIcon,
  Trash2, PlusCircle, AlertTriangle, Pencil
} from 'lucide-react'
import EditUserModal from '../components/modals/EditUserModal'
import AddConnectionsModal from '../components/modals/AddConnectionsModal'
import RenewPlanModal from '../components/modals/RenewPlanModal'

export default function Users() {
  const [users, setUsers] = useState([])
  const [plans, setPlans] = useState([])
  const [availableNodes, setAvailableNodes] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState({ userName: '', password: '', planId: '', nodeId: '' })
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)
  const [filter, setFilter] = useState('all')
  const [search, setSearch] = useState('')
  const [selectedIds, setSelectedIds] = useState([])
  const [showBulkExtend, setShowBulkExtend] = useState(false)
  const [bulkExtendDays, setBulkExtendDays] = useState(30)
  const [bulkLoading, setBulkLoading] = useState(false)

  // Modal states
  const [showEditModal, setShowEditModal] = useState(false)
  const [showAddConnectionsModal, setShowAddConnectionsModal] = useState(false)
  const [showRenewPlanModal, setShowRenewPlanModal] = useState(false)
  const [selectedUser, setSelectedUser] = useState(null)
  const [resellers, setResellers] = useState([])
  const [selectedResellerFilter, setSelectedResellerFilter] = useState('')

  useEffect(() => {
    fetchData()
  }, [])

  async function fetchData() {
    try {
      const [usersRes, plansRes, nodesRes, resellersRes] = await Promise.all([
        api.get('/user/my-subusers'),
        api.get('/plan/my-plans'),
        api.get('/nodeaccess/my-nodes'),
        api.get('/user/my-resellers'),
      ])
      setUsers(usersRes.data)
      setPlans(plansRes.data)
      setAvailableNodes(nodesRes.data)
      setResellers(resellersRes.data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  function handleToggleActive(user) {
    if (!confirm(`¿${user.isActive ? 'Suspender' : 'Activar'} a ${user.userName}?`)) return
    api.post(`/user/${user.id}/suspend`)
      .then(({ data }) => {
        setMessage({ type: 'success', text: data.message })
        fetchData()
      })
      .catch(err => {
        setMessage({ type: 'error', text: err.response?.data?.message || 'Error al cambiar estado' })
      })
  }

  function openEditModal(user) {
    setSelectedUser(user)
    setShowEditModal(true)
  }

  function openAddConnectionsModal(user) {
    setSelectedUser(user)
    setShowAddConnectionsModal(true)
  }

  function openRenewPlanModal(user) {
    setSelectedUser(user)
    setShowRenewPlanModal(true)
  }

  function handleModalSuccess() {
    fetchData()
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setMessage(null)

    try {
      const { data } = await api.post('/user/create', {
        userName: form.userName,
        password: form.password,
        planId: form.planId,
        nodeId: form.nodeId || null,
      })
      setMessage({ type: 'success', text: data.message })
      setForm({ userName: '', password: '', planId: '', nodeId: '' })
      setShowCreate(false)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al crear usuario' })
    } finally {
      setSubmitting(false)
    }
  }

  const filteredUsers = users.filter(u => {
    // Solo mostrar Customers (no resellers)
    if (u.role === 'Reseller' || u.role === 'SubReseller') return false

    if (search && !u.userName.toLowerCase().includes(search.toLowerCase())) return false
    if (filter === 'active') return u.serviceExpiry && new Date(u.serviceExpiry) > new Date() && !u.isTrial
    if (filter === 'expired') return u.serviceExpiry && new Date(u.serviceExpiry) <= new Date()
    if (filter === 'trial') return u.isTrial

    // Filtro por reseller
    if (selectedResellerFilter && u.resellerName !== selectedResellerFilter) return false

    return true
  })

  function toggleSelect(id) {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id])
  }

  function toggleSelectAll() {
    if (selectedIds.length === filteredUsers.length) {
      setSelectedIds([])
    } else {
      setSelectedIds(filteredUsers.map(u => u.id))
    }
  }

  async function handleBulkExtend() {
    if (selectedIds.length === 0) return
    setBulkLoading(true)
    setMessage(null)
    try {
      const { data } = await api.post('/user/bulk/extend-service', {
        userIds: selectedIds,
        days: bulkExtendDays,
      })
      setMessage({ type: 'success', text: data.message })
      setSelectedIds([])
      setShowBulkExtend(false)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al extender' })
    } finally {
      setBulkLoading(false)
    }
  }

  async function handleBulkDelete() {
    console.group('🗑️ handleBulkDelete INICIADO')
    console.log('📊 selectedIds:', selectedIds)
    
    if (selectedIds.length === 0) {
      console.warn('⚠️ No hay usuarios seleccionados')
      console.groupEnd()
      return
    }
    
    const confirmDelete = confirm(`Eliminar ${selectedIds.length} usuarios? Esta accion no se puede deshacer.`)
    console.log('✓ Confirmación del usuario:', confirmDelete)
    
    if (!confirmDelete) {
      console.log('❌ Usuario canceló la operación')
      console.groupEnd()
      return
    }
    
    setBulkLoading(true)
    setMessage(null)
    
    try {
      console.log('🔄 Enviando petición DELETE bulk...')
      const payload = { UserIds: selectedIds }
      console.log('📦 Payload:', JSON.stringify(payload, null, 2))
      
      const { data } = await api.post('/user/bulk/delete', payload)
      
      console.log('✅ Respuesta exitosa:', data)
      setMessage({ type: 'success', text: data.message })
      setSelectedIds([])
      fetchData()
      console.log('✓ UI actualizada - fetchData ejecutado')
    } catch (err) {
      console.error('❌ Error en handleBulkDelete:', {
        message: err.message,
        response: err.response?.data,
        status: err.response?.status,
        statusText: err.response?.statusText,
        config: {
          url: err.config?.url,
          method: err.config?.method,
          data: err.config?.data
        }
      })
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al eliminar' })
    } finally {
      setBulkLoading(false)
      console.groupEnd()
    }
  }

  const roleColors = {
    0: 'badge-danger', 1: 'badge-info', 2: 'badge-success',
    3: 'badge-info', 4: 'badge-warning'
  }
  const roleLabels = { 0: 'SuperAdmin', 1: 'Admin', 2: 'Reseller', 3: 'SubReseller', 4: 'Customer' }

  function isExpired(date) {
    if (!date) return false
    return new Date(date) < new Date()
  }

  function formatExpiry(date) {
    if (!date) return '-'
    return new Date(date).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', year: 'numeric' })
  }

  function formatDuration(hours) {
    if (!hours || hours <= 0) return '-'
    if (hours >= 24 && hours % 24 === 0) return `${hours / 24} dias`
    return `${hours} horas`
  }

  return (
    <div className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Usuarios</h1>
          <p className="text-sm text-[var(--text-muted)]">{users.length} usuarios registrados</p>
        </div>
        <div className="flex gap-2">
          {selectedIds.length > 0 && (
            <>
              <span className="flex items-center text-sm text-[var(--text-secondary)] mr-1">
                {selectedIds.length} seleccionados
              </span>
              <button
                onClick={() => setShowBulkExtend(true)}
                disabled={bulkLoading}
                className="btn btn-secondary text-sm"
              >
                <PlusCircle className="w-4 h-4" />
                Extender
              </button>
              <button
                onClick={handleBulkDelete}
                disabled={bulkLoading}
                className="btn btn-secondary text-sm text-red-400 border-red-500/30 hover:bg-red-500/10"
              >
                <Trash2 className="w-4 h-4" />
                Eliminar
              </button>
            </>
          )}
          <button
            onClick={() => setShowCreate(!showCreate)}
            className="btn btn-primary"
          >
            <UserPlus className="w-4 h-4" />
            Nuevo Usuario
          </button>
        </div>
      </div>

      {/* Message */}
      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-lg mb-4 text-sm ${
          message.type === 'success' ? 'bg-green-500/10 border border-green-500/30 text-green-400' : 'bg-red-500/10 border border-red-500/30 text-red-400'
        }`}>
          {message.type === 'success' ? <Check className="w-4 h-4" /> : <X className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Bulk Extend Modal */}
      {showBulkExtend && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
          <div className="card p-6 max-w-sm w-full">
            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Extender Servicio</h3>
            <p className="text-sm text-[var(--text-secondary)] mb-4">
              Extender {selectedIds.length} usuarios por:
            </p>
            <div className="mb-4">
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Dias</label>
              <input
                type="number"
                value={bulkExtendDays}
                onChange={(e) => setBulkExtendDays(parseInt(e.target.value) || 30)}
                className="input"
                min="1"
                max="365"
              />
            </div>
            <div className="flex gap-2">
              <button onClick={handleBulkExtend} disabled={bulkLoading} className="btn btn-primary flex-1">
                {bulkLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                Confirmar
              </button>
              <button onClick={() => setShowBulkExtend(false)} className="btn btn-secondary">
                Cancelar
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Create Form */}
      {showCreate && (
        <form onSubmit={handleSubmit} className="card p-4 mb-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4">Crear Nuevo Usuario</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Username</label>
              <input
                type="text"
                value={form.userName}
                onChange={(e) => setForm({ ...form, userName: e.target.value })}
                className="input"
                placeholder="usuario123"
                required
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Password</label>
              <input
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                className="input"
                placeholder="Minimo 6 caracteres"
                required
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Plan</label>
              <select
                value={form.planId}
                onChange={(e) => setForm({ ...form, planId: e.target.value })}
                className="input"
                required
              >
                <option value="">Seleccionar plan...</option>
                {plans.map(p => (
                  <option key={p.id} value={p.id}>{p.name} - {p.creditCost} credits - {p.maxDevices} disp</option>
                ))}
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Nodo (opcional)</label>
              <select
                value={form.nodeId}
                onChange={(e) => setForm({ ...form, nodeId: e.target.value })}
                className="input"
              >
                <option value="">Sin provisionar</option>
                {availableNodes.map(n => (
                  <option key={n.id} value={n.id}>{n.label} ({n.ip})</option>
                ))}
              </select>
            </div>
          </div>
          <div className="flex gap-2 mt-4">
            <button type="submit" disabled={submitting} className="btn btn-primary">
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              Crear
            </button>
            <button type="button" onClick={() => setShowCreate(false)} className="btn btn-secondary">
              Cancelar
            </button>
          </div>
        </form>
      )}

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            className="input pl-10"
            placeholder="Buscar usuario..."
          />
        </div>
        <div>
          <select
            value={selectedResellerFilter}
            onChange={(e) => setSelectedResellerFilter(e.target.value)}
            className="input"
          >
            <option value="">Todos los resellers</option>
            {resellers.map(r => (
              <option key={r.id} value={r.userName}>{r.userName}</option>
            ))}
          </select>
        </div>
        <div className="flex gap-1.5">
          {[
            { key: 'all', label: 'Todos' },
            { key: 'active', label: 'Activos' },
            { key: 'expired', label: 'Vencidos' },
            { key: 'trial', label: 'Prueba' },
          ].map(f => (
            <button
              key={f.key}
              onClick={() => setFilter(f.key)}
              className={`btn btn-sm ${filter === f.key ? 'btn-primary' : 'btn-secondary'}`}
            >
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
        </div>
      ) : filteredUsers.length === 0 ? (
        <div className="card flex flex-col items-center justify-center py-16">
          <UsersIcon className="w-12 h-12 text-[var(--text-muted)] mb-3" />
          <p className="text-[var(--text-muted)]">No se encontraron usuarios</p>
        </div>
      ) : (
        <div className="table-container overflow-x-auto">
          <table className="table">
            <thead>
                <tr>
                  <th className="w-10">
                    <input
                      type="checkbox"
                      checked={selectedIds.length === filteredUsers.length && filteredUsers.length > 0}
                      onChange={toggleSelectAll}
                      className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                    />
                  </th>
                  <th>Usuario</th>
                  <th>Plan / Duracion</th>
                  <th>Disp</th>
                  <th>Conexiones</th>
                  <th>Reseller</th>
                  <th>Vencimiento</th>
                  <th>Estado</th>
                  <th>Creditos</th>
                  <th>Acciones</th>
                </tr>
            </thead>
            <tbody>
                {filteredUsers.map(u => (
                <tr key={u.id}>
                  <td>
                    <input
                      type="checkbox"
                      checked={selectedIds.includes(u.id)}
                      onChange={() => toggleSelect(u.id)}
                      className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                    />
                  </td>
                  <td>
                    <div className="flex items-center gap-2">
                      <span className={`status-dot ${u.isTrial ? 'warning' : isExpired(u.serviceExpiry) ? 'offline' : 'online'}`} />
                      <div>
                        <p className="font-medium text-[var(--text-primary)]">{u.userName}</p>
                        <div className="flex items-center gap-1 mt-0.5">
                          <span className={`badge ${roleColors[u.role] || 'badge-warning'}`}>
                            {roleLabels[u.role] || u.role}
                          </span>
                          {u.isTrial && <span className="badge badge-warning">Prueba</span>}
                          {u.isProvisionedOnVps && (
                            <span className="badge badge-success flex items-center gap-0.5">
                              <Server className="w-3 h-3" /> VPS
                            </span>
                          )}
                        </div>
                      </div>
                    </div>
                  </td>
                  <td>
                    <div>
                      <p className="text-[var(--text-secondary)]">{u.planName || '-'}</p>
                      {u.planDurationHours && <p className="text-xs text-[var(--text-muted)]">{formatDuration(u.planDurationHours)}</p>}
                      {u.isTrialPlan && <span className="badge badge-warning mt-0.5">Prueba</span>}
                    </div>
                  </td>
                  <td className="text-center text-[var(--text-secondary)]">{u.maxDevices || 1}</td>
                  <td className="text-center text-[var(--text-secondary)]">
                    {((u.plan?.maxConnections || 0) + (u.additionalConnections || 0))}
                    <span className="text-xs text-[var(--text-muted)]"> ({(u.additionalConnections || 0)} extra)</span>
                  </td>
                  <td className="text-sm text-[var(--text-secondary)]">{u.resellerName || 'N/A'}</td>
                  <td>
                    <span className={`text-sm ${isExpired(u.serviceExpiry) ? 'text-red-400' : 'text-[var(--text-secondary)]'}`}>
                      <Calendar className="w-3.5 h-3.5 inline mr-1" />
                      {formatExpiry(u.serviceExpiry)}
                    </span>
                  </td>
                  <td>
                    {u.isTrial
                      ? <span className="badge badge-warning">Trial</span>
                      : isExpired(u.serviceExpiry)
                        ? <span className="badge badge-danger">Vencido</span>
                        : <span className="badge badge-success">Activo</span>
                    }
                  </td>
                  <td className="font-semibold text-[var(--text-primary)]">{u.credits?.toLocaleString() ?? 0}</td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button onClick={() => openEditModal(u)} className="btn btn-sm btn-secondary" title="Editar usuario">
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openAddConnectionsModal(u)} className="btn btn-sm btn-secondary" title="Agregar conexiones">
                        <PlusCircle className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openRenewPlanModal(u)} className="btn btn-sm btn-secondary" title="Renovar plan">
                        <Calendar className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => handleToggleActive(u)} className={`btn btn-sm ${u.isActive ? 'btn-secondary text-red-400' : 'btn-secondary text-green-400'}`} title={u.isActive ? 'Suspender' : 'Activar'}>
                        {u.isActive ? <X className="w-3.5 h-3.5" /> : <Check className="w-3.5 h-3.5" />}
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Modales */}
      <EditUserModal
        show={showEditModal}
        onClose={() => setShowEditModal(false)}
        user={selectedUser}
        availableNodes={availableNodes}
        onSuccess={handleModalSuccess}
      />
      <AddConnectionsModal
        show={showAddConnectionsModal}
        onClose={() => setShowAddConnectionsModal(false)}
        user={selectedUser}
        onSuccess={handleModalSuccess}
      />
      <RenewPlanModal
        show={showRenewPlanModal}
        onClose={() => setShowRenewPlanModal(false)}
        user={selectedUser}
        onSuccess={handleModalSuccess}
      />
    </div>
  )
}
