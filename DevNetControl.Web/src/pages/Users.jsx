import { useEffect, useState } from 'react'
import api from '../lib/api'
import { useAuthStore } from '../store/authStore'
import {
  UserPlus, Search, X, Loader2, Server, Activity, Edit3, RefreshCw, Plus,
  AlertTriangle, Trash2, ToggleLeft, CheckSquare, Square, Clock
} from 'lucide-react'

export default function Users() {
  const { user } = useAuthStore()
  const [users, setUsers] = useState([])
  const [search, setSearch] = useState('')
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [creating, setCreating] = useState(false)
  const [nodes, setNodes] = useState([])
  const [plans, setPlans] = useState([])
  const [error, setError] = useState('')
  const [selectedIds, setSelectedIds] = useState(new Set())
  const [selectAll, setSelectAll] = useState(false)
  const [bulkAction, setBulkAction] = useState('')
  const [bulkLoading, setBulkLoading] = useState(false)

  // Modals
  const [editUser, setEditUser] = useState(null)
  const [renewUser, setRenewUser] = useState(null)
  const [confirmDelete, setConfirmDelete] = useState(null)

  const [form, setForm] = useState({ userName: '', password: '', planId: '', nodeId: '' })
  const [editForm, setEditForm] = useState({ userName: '', password: '', nodeId: '' })
  const [renewForm, setRenewForm] = useState({ planId: '', durationHours: 720 })
  const [bulkDays, setBulkDays] = useState(30)

  useEffect(() => { loadUsers(); loadPlans(); loadNodes() }, [])

  async function loadUsers() {
    try { setLoading(true); const res = await api.get('/user/my-subusers'); setUsers(res.data) }
    catch (err) { console.error('Error loading users:', err) }
    finally { setLoading(false) }
  }

  async function loadPlans() {
    try { const res = await api.get('/plan'); setPlans(res.data) }
    catch (err) { console.error('Error loading plans:', err) }
  }

  async function loadNodes() {
    try { const res = await api.get('/vpsnode'); setNodes(res.data) }
    catch (err) { console.error('Error loading nodes:', err) }
  }

  async function handleCreate(e) {
    e.preventDefault(); setError(''); setCreating(true)
    try {
      await api.post('/user/create', {
        userName: form.userName,
        password: form.password,
        planId: form.planId || null,
        nodeId: form.nodeId || null
      })
      setShowCreate(false); setForm({ userName: '', password: '', planId: '', nodeId: '' })
      loadUsers()
    } catch (err) { setError(err.response?.data?.message || 'Error creating user') }
    finally { setCreating(false) }
  }

  async function handleEdit(e) {
    e.preventDefault(); setError(''); setCreating(true)
    try {
      const payload = {}
      if (editForm.userName) payload.userName = editForm.userName
      if (editForm.password) payload.password = editForm.password
      if (editForm.nodeId) payload.nodeId = editForm.nodeId
      await api.put(`/user/${editUser.id}`, payload)
      setEditUser(null); loadUsers()
    } catch (err) { setError(err.response?.data?.message || 'Error updating user') }
    finally { setCreating(false) }
  }

  async function handleRenew(e) {
    e.preventDefault(); setError(''); setCreating(true)
    try {
      await api.post(`/user/${renewUser.id}/renew-plan`, {
        planId: renewForm.planId,
        durationHours: parseInt(renewForm.durationHours)
      })
      // Cerrar modal
      setRenewUser(null)
      // Forzar recarga completa con spinner
      setLoading(true)
      const refreshed = await api.get('/user/my-subusers')
      setUsers([...refreshed.data])
      setLoading(false)
      console.log('✅ Plan renovado - datos actualizados:', refreshed.data.length, 'usuarios')
    } catch (err) { setError(err.response?.data?.message || 'Error renewing plan') }
    finally { setCreating(false) }
  }

  async function handleAddConnection(userId) {
    try {
      await api.post(`/user/${userId}/add-connection`, { connectionsToAdd: 1 })
      loadUsers()
    } catch (err) { alert(err.response?.data?.message || 'Error adding connection') }
  }

  async function handleToggleSuspend(userId) {
    try {
      await api.post(`/user/${userId}/suspend`)
      const refreshed = await api.get('/user/my-subusers')
      setUsers(refreshed.data)
    } catch (err) { alert(err.response?.data?.message || 'Error toggling suspend') }
  }

  async function handleDelete(userId) {
    try {
      await api.delete(`/user/${userId}`)
      setConfirmDelete(null); loadUsers()
    } catch (err) { alert(err.response?.data?.message || 'Error deleting user') }
  }

  async function handleBulkAction() {
    if (selectedIds.size === 0 || !bulkAction) return
    setBulkLoading(true)
    try {
      const ids = Array.from(selectedIds)
      let res
      if (bulkAction === 'extend') res = await api.post('/user/bulk/extend-service', { userIds: ids, days: bulkDays })
      else if (bulkAction === 'delete') res = await api.post('/user/bulk/delete', { userIds: ids })
      else if (bulkAction === 'suspend') res = await api.post('/user/bulk/toggle-suspend', { userIds: ids })
      alert(res.data?.message || 'Bulk operation completed')
      setSelectedIds(new Set()); setSelectAll(false); loadUsers()
    } catch (err) { alert(err.response?.data?.message || 'Bulk operation failed') }
    finally { setBulkLoading(false); setBulkAction('') }
  }

  function toggleSelect(id) {
    const next = new Set(selectedIds)
    if (next.has(id)) next.delete(id); else next.add(id)
    setSelectedIds(next)
    setSelectAll(next.size === filteredUsers.length && filteredUsers.length > 0)
  }

  function toggleSelectAll() {
    if (selectAll) { setSelectedIds(new Set()); setSelectAll(false) }
    else { setSelectedIds(new Set(filteredUsers.map(u => u.id))); setSelectAll(true) }
  }

  const filteredUsers = users.filter(u =>
    u.userName?.toLowerCase().includes(search.toLowerCase())
  )

  function getRoleBadge(role) {
    const c = {
      SuperAdmin: 'bg-red-500/20 text-red-300', Admin: 'bg-yellow-500/20 text-yellow-300',
      Reseller: 'bg-blue-500/20 text-blue-300', SubReseller: 'bg-cyan-500/20 text-cyan-300',
      Customer: 'bg-gray-500/20 text-gray-300'
    }
    return c[role] || 'bg-gray-500/20 text-gray-300'
  }

  function fmtId(id) { return id ? id.slice(0, 6) + '…' : '—' }

  function getNodeLabel(n) {
    if (!n) return '—'
    return n.label || n.ip || fmtId(n.id)
  }

  return (
    <div className="space-y-4 px-1 sm:px-0">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-xl sm:text-2xl font-bold text-white">Usuarios</h1>
          <p className="text-xs sm:text-sm text-gray-400 mt-0.5">{users.length} usuario{users.length !== 1 ? 's' : ''}</p>
        </div>
        <button
          onClick={() => { setShowCreate(true); setError('') }}
          className="w-full sm:w-auto px-4 py-2.5 bg-gradient-to-r from-blue-600 to-cyan-600 text-white rounded-lg hover:from-blue-500 hover:to-cyan-500 transition-all text-sm flex items-center justify-center gap-2"
        >
          <UserPlus className="w-4 h-4" />
          Nuevo Usuario
        </button>
      </div>

      {/* Search + Bulk Actions */}
      <div className="flex flex-col sm:flex-row gap-2">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
          <input
            type="text"
            placeholder="Buscar por nombre o ID…"
            value={search}
            onChange={e => setSearch(e.target.value)}
            className="w-full pl-10 pr-4 py-2 bg-gray-800 border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:border-blue-500 text-sm"
          />
        </div>
        {selectedIds.size > 0 && (
          <div className="flex flex-wrap gap-2 items-center">
            <span className="text-xs text-gray-400 whitespace-nowrap">{selectedIds.size} seleccionado(s)</span>
            <select
              value={bulkAction}
              onChange={e => setBulkAction(e.target.value)}
              className="px-2 py-1.5 bg-gray-700 border border-gray-600 rounded-lg text-white text-xs focus:outline-none"
            >
              <option value="">Acción masiva…</option>
              <option value="extend">Extender servicio</option>
              <option value="suspend">Suspender/Activar</option>
              <option value="delete">Eliminar</option>
            </select>
            {bulkAction === 'extend' && (
              <input
                type="number"
                value={bulkDays}
                onChange={e => setBulkDays(parseInt(e.target.value) || 30)}
                className="w-16 px-2 py-1.5 bg-gray-700 border border-gray-600 rounded-lg text-white text-xs focus:outline-none"
                placeholder="Días"
                min={1}
              />
            )}
            <button
              onClick={handleBulkAction}
              disabled={bulkLoading || !bulkAction}
              className="px-3 py-1.5 bg-blue-600 text-white rounded-lg hover:bg-blue-500 disabled:opacity-50 text-xs flex items-center gap-1"
            >
              {bulkLoading ? <Loader2 className="w-3 h-3 animate-spin" /> : 'Aplicar'}
            </button>
          </div>
        )}
      </div>

      {/* Users List - Mobile First Cards */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="w-8 h-8 animate-spin text-blue-400" />
        </div>
      ) : filteredUsers.length === 0 ? (
        <div className="text-center py-16 text-gray-500 text-sm">No hay usuarios</div>
      ) : (
        <>
          {/* Desktop Table (hidden on mobile) */}
          <div className="hidden md:block overflow-x-auto rounded-xl border border-gray-700">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-800/80">
                  <th className="px-3 py-3 text-left">
                    <button onClick={toggleSelectAll} className="text-gray-400 hover:text-white">
                      {selectAll ? <CheckSquare className="w-4 h-4" /> : <Square className="w-4 h-4" />}
                    </button>
                  </th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Usuario</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">ID</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Conexiones</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Plan</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Servidor</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Estado</th>
                  <th className="text-left px-3 py-3 text-gray-400 font-medium">Vence</th>
                  <th className="text-right px-3 py-3 text-gray-400 font-medium">Acciones</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-700/50">
                {filteredUsers.map(u => (
                  <tr key={u.id} className="hover:bg-gray-800/40 transition-colors">
                    <td className="px-3 py-3">
                      <button onClick={() => toggleSelect(u.id)} className="text-gray-400 hover:text-white">
                        {selectedIds.has(u.id) ? <CheckSquare className="w-4 h-4 text-blue-400" /> : <Square className="w-4 h-4" />}
                      </button>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-2">
                        <div className="w-8 h-8 rounded-full bg-gray-700 flex items-center justify-center text-xs font-bold text-white flex-shrink-0">
                          {u.userName?.charAt(0).toUpperCase() || '?'}
                        </div>
                        <div className="min-w-0">
                          <p className="text-white font-medium truncate max-w-[120px]">{u.userName}</p>
                          <span className={`px-1.5 py-0.5 rounded text-[10px] font-medium ${getRoleBadge(u.role)}`}>{u.role}</span>
                        </div>
                      </div>
                    </td>
                    <td className="px-3 py-3"><code className="text-xs text-gray-400 font-mono">{fmtId(u.id)}</code></td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-1.5">
                        <Activity className="w-3.5 h-3.5 text-blue-400" />
                        <span className="text-white text-xs font-mono">
                          {u.activeConnections || 0}/{u.maxConnections || 0}
                        </span>
                      </div>
                    </td>
                    <td className="px-3 py-3"><span className="text-gray-300 text-xs">{u.planName || '—'}</span></td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-1.5">
                        <Server className="w-3.5 h-3.5 text-emerald-400 flex-shrink-0" />
                        <span className="text-gray-300 text-xs truncate max-w-[100px]">
                          {u.connectedNodes?.length > 0 ? u.connectedNodes.map(getNodeLabel).join(', ') : '—'}
                        </span>
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      <div className="flex items-center gap-1.5">
                        <span className={`w-2 h-2 rounded-full ${u.isActive ? 'bg-green-400' : 'bg-red-400'}`} />
                        <span className={`text-[11px] ${u.isActive ? 'text-green-400' : 'text-red-400'}`}>
                          {u.isActive ? 'Activo' : 'Inactivo'}{u.isTrial ? ' (Trial)' : ''}
                        </span>
                      </div>
                    </td>
                    <td className="px-3 py-3">
                      {u.serviceExpiry ? (
                        <span className="text-gray-300 text-xs">{new Date(u.serviceExpiry).toLocaleDateString()}</span>
                      ) : <span className="text-gray-500 text-xs">—</span>}
                    </td>
                    <td className="px-3 py-3">
                      <DesktopActions user={u} onEdit={u => { setEditUser(u); setEditForm({ userName: u.userName, password: '', nodeId: '' }); setError('') }}
                        onRenew={u => { setRenewUser(u); setRenewForm({ planId: '', durationHours: 720 }); setError('') }}
                        onAddConnection={handleAddConnection} onToggleSuspend={handleToggleSuspend}
                        onDelete={u => setConfirmDelete(u)} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {/* Mobile Cards */}
          <div className="md:hidden space-y-3">
            {filteredUsers.map(u => (
              <div key={u.id} className="bg-gray-800/50 border border-gray-700 rounded-xl p-3 space-y-3">
                {/* Row 1: Avatar + Name + Role */}
                <div className="flex items-center gap-3">
                  <button onClick={() => toggleSelect(u.id)} className="text-gray-400 hover:text-white flex-shrink-0">
                    {selectedIds.has(u.id) ? <CheckSquare className="w-4 h-4 text-blue-400" /> : <Square className="w-4 h-4" />}
                  </button>
                  <div className="w-9 h-9 rounded-full bg-gray-700 flex items-center justify-center text-sm font-bold text-white flex-shrink-0">
                    {u.userName?.charAt(0).toUpperCase() || '?'}
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-white font-medium truncate">{u.userName}</p>
                    <div className="flex items-center gap-2 mt-0.5">
                      <span className={`px-1.5 py-0.5 rounded text-[10px] font-medium ${getRoleBadge(u.role)}`}>{u.role}</span>
                      <code className="text-[10px] text-gray-500 font-mono">{fmtId(u.id)}</code>
                    </div>
                  </div>
                  <span className={`w-2 h-2 rounded-full ${u.isActive ? 'bg-green-400' : 'bg-red-400'} flex-shrink-0`} />
                </div>

                {/* Row 2: Stats */}
                <div className="grid grid-cols-3 gap-2 text-center text-xs">
                  <div className="bg-gray-700/50 rounded-lg p-2">
                    <Activity className="w-3.5 h-3.5 text-blue-400 mx-auto mb-1" />
                    <span className="text-white font-mono">{u.activeConnections || 0}/{u.maxConnections || 0}</span>
                  </div>
                  <div className="bg-gray-700/50 rounded-lg p-2">
                    <Server className="w-3.5 h-3.5 text-emerald-400 mx-auto mb-1" />
                    <span className="text-gray-300 truncate block">
                      {u.connectedNodes?.length > 0 ? getNodeLabel(u.connectedNodes[0]) : '—'}
                    </span>
                  </div>
                  <div className="bg-gray-700/50 rounded-lg p-2">
                    <Clock className="w-3.5 h-3.5 text-yellow-400 mx-auto mb-1" />
                    <span className="text-gray-300">
                      {u.serviceExpiry ? new Date(u.serviceExpiry).toLocaleDateString() : '—'}
                    </span>
                  </div>
                </div>

                {/* Row 3: Plan + Status */}
                <div className="flex items-center justify-between text-xs">
                  <span className="text-gray-400">
                    Plan: <span className="text-gray-200">{u.planName || 'Sin Plan'}</span>
                    {u.isTrial && <span className="text-purple-400 ml-1">(Trial)</span>}
                  </span>
                  <span className={`${u.isActive ? 'text-green-400' : 'text-red-400'}`}>
                    {u.isActive ? 'Activo' : 'Inactivo'}
                  </span>
                </div>

                {/* Row 4: Acciones */}
                <MobileActions user={u} onEdit={u => { setEditUser(u); setEditForm({ userName: u.userName, password: '', nodeId: '' }); setError('') }}
                  onRenew={u => { setRenewUser(u); setRenewForm({ planId: '', durationHours: 720 }); setError('') }}
                  onAddConnection={handleAddConnection} onToggleSuspend={handleToggleSuspend}
                  onDelete={u => setConfirmDelete(u)} />
              </div>
            ))}
          </div>
        </>
      )}

      {/* ===== MODALS ===== */}
      {[showCreate, editUser, renewUser, confirmDelete].some(Boolean) && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50 p-4" onClick={e => {
          if (e.target === e.currentTarget) { setShowCreate(false); setEditUser(null); setRenewUser(null); setConfirmDelete(null) }
        }}>
          <div className="bg-gray-800 rounded-2xl p-5 w-full max-w-sm border border-gray-700" onClick={e => e.stopPropagation()}>
            {/* Create Modal */}
            {showCreate && (
              <>
                <div className="flex items-center justify-between mb-5">
                  <h2 className="text-lg font-semibold text-white">Nuevo Usuario</h2>
                  <button onClick={() => setShowCreate(false)} className="text-gray-400 hover:text-white"><X className="w-5 h-5" /></button>
                </div>
                {error && <p className="mb-3 p-2 bg-red-500/10 border border-red-500/30 rounded text-red-300 text-xs">{error}</p>}
                <form onSubmit={handleCreate} className="space-y-3">
                  <InputField label="Usuario" value={form.userName} onChange={v => setForm({ ...form, userName: v })} />
                  <InputField label="Contraseña" value={form.password} onChange={v => setForm({ ...form, password: v })} type="password" />
                  <SelectField label="Plan" value={form.planId} onChange={v => setForm({ ...form, planId: v })} options={plans.map(p => ({ value: p.id, label: `${p.name} (${p.creditCost || p.cost} créditos)` }))} placeholder="Sin plan" />
                  <SelectField label="Nodo VPS" value={form.nodeId} onChange={v => setForm({ ...form, nodeId: v })} options={nodes.map(n => ({ value: n.id, label: n.label || n.ip }))} placeholder="Sin nodo" />
                  <button type="submit" disabled={creating} className="w-full py-2.5 bg-gradient-to-r from-blue-600 to-cyan-600 text-white rounded-lg hover:from-blue-500 hover:to-cyan-500 disabled:opacity-50 text-sm flex items-center justify-center gap-2">
                    {creating ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserPlus className="w-4 h-4" />}
                    {creating ? 'Creando…' : 'Crear Usuario'}
                  </button>
                </form>
              </>
            )}

            {/* Edit Modal */}
            {editUser && (
              <>
                <div className="flex items-center justify-between mb-5">
                  <h2 className="text-lg font-semibold text-white">Editar {editUser.userName}</h2>
                  <button onClick={() => setEditUser(null)} className="text-gray-400 hover:text-white"><X className="w-5 h-5" /></button>
                </div>
                {error && <p className="mb-3 p-2 bg-red-500/10 border border-red-500/30 rounded text-red-300 text-xs">{error}</p>}
                <form onSubmit={handleEdit} className="space-y-3">
                  <InputField label="Usuario" value={editForm.userName} onChange={v => setEditForm({ ...editForm, userName: v })} />
                  <InputField label="Nueva contraseña (opcional)" value={editForm.password} onChange={v => setEditForm({ ...editForm, password: v })} type="password" />
                  <SelectField label="Nodo VPS" value={editForm.nodeId} onChange={v => setEditForm({ ...editForm, nodeId: v })} options={nodes.map(n => ({ value: n.id, label: n.label || n.ip }))} placeholder="Sin cambio" />
                  <button type="submit" disabled={creating} className="w-full py-2.5 bg-gradient-to-r from-yellow-500 to-orange-500 text-white rounded-lg hover:from-yellow-400 hover:to-orange-400 disabled:opacity-50 text-sm flex items-center justify-center gap-2">
                    {creating ? <Loader2 className="w-4 h-4 animate-spin" /> : <Edit3 className="w-4 h-4" />}
                    {creating ? 'Guardando…' : 'Guardar Cambios'}
                  </button>
                </form>
              </>
            )}

            {/* Renew Modal */}
            {renewUser && (
              <>
                <div className="flex items-center justify-between mb-5">
                  <h2 className="text-lg font-semibold text-white">Renovar Plan - {renewUser.userName}</h2>
                  <button onClick={() => setRenewUser(null)} className="text-gray-400 hover:text-white"><X className="w-5 h-5" /></button>
                </div>
                {error && <p className="mb-3 p-2 bg-red-500/10 border border-red-500/30 rounded text-red-300 text-xs">{error}</p>}
                <form onSubmit={handleRenew} className="space-y-3">
                  <SelectField label="Plan" value={renewForm.planId} onChange={v => setRenewForm({ ...renewForm, planId: v })}
                    options={plans.filter(p => !p.isTrial).map(p => ({ value: p.id, label: `${p.name} (${p.creditCost || p.cost} créditos - ${p.durationHours}h)` }))} placeholder="Seleccionar plan…" />
                  <InputField label="Duración (horas)" value={renewForm.durationHours} onChange={v => setRenewForm({ ...renewForm, durationHours: v })} type="number" />
                  <button type="submit" disabled={creating} className="w-full py-2.5 bg-gradient-to-r from-green-500 to-emerald-500 text-white rounded-lg hover:from-green-400 hover:to-emerald-400 disabled:opacity-50 text-sm flex items-center justify-center gap-2">
                    {creating ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
                    {creating ? 'Renovando…' : 'Renovar Plan'}
                  </button>
                </form>
              </>
            )}

            {/* Delete Confirm */}
            {confirmDelete && (
              <>
                <div className="flex items-center justify-between mb-5">
                  <h2 className="text-lg font-semibold text-red-400">Eliminar Usuario</h2>
                  <button onClick={() => setConfirmDelete(null)} className="text-gray-400 hover:text-white"><X className="w-5 h-5" /></button>
                </div>
                <p className="text-gray-300 text-sm mb-6">
                  ¿Estás seguro de eliminar a <strong>{confirmDelete.userName}</strong>? Se eliminarán también sus sub-usuarios y accesos. Esta acción no se puede deshacer.
                </p>
                <div className="flex gap-3">
                  <button onClick={() => setConfirmDelete(null)} className="flex-1 py-2.5 bg-gray-700 text-white rounded-lg hover:bg-gray-600 text-sm">Cancelar</button>
                  <button onClick={() => handleDelete(confirmDelete.id)} className="flex-1 py-2.5 bg-red-600 text-white rounded-lg hover:bg-red-500 text-sm flex items-center justify-center gap-2">
                    <Trash2 className="w-4 h-4" /> Eliminar
                  </button>
                </div>
              </>
            )}
          </div>
        </div>
      )}
    </div>
  )
}

/* ===== Helper Components ===== */

function InputField({ label, value, onChange, type = 'text' }) {
  return (
    <div>
      <label className="block text-xs text-gray-400 mb-1">{label}</label>
      <input type={type} value={value} onChange={e => onChange(e.target.value)}
        className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:border-blue-500 text-sm" required={type !== 'password'} />
    </div>
  )
}

function SelectField({ label, value, onChange, options, placeholder }) {
  return (
    <div>
      <label className="block text-xs text-gray-400 mb-1">{label}</label>
      <select value={value} onChange={e => onChange(e.target.value)}
        className="w-full px-3 py-2 bg-gray-700 border border-gray-600 rounded-lg text-white focus:outline-none focus:border-blue-500 text-sm">
        <option value="">{placeholder}</option>
        {options.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
      </select>
    </div>
  )
}

function DesktopActions({ user, onEdit, onRenew, onAddConnection, onToggleSuspend, onDelete }) {
  return (
    <div className="flex items-center justify-end gap-1">
      <ActionBtn icon={<Edit3 className="w-3.5 h-3.5" />} label="Editar" onClick={() => onEdit(user)} color="blue" />
      <ActionBtn icon={<RefreshCw className="w-3.5 h-3.5" />} label="Renovar" onClick={() => onRenew(user)} color="green" />
      <ActionBtn icon={<Plus className="w-3.5 h-3.5" />} label="+1 Conexión" onClick={() => onAddConnection(user.id)} color="cyan" />
      <ActionBtn icon={<ToggleLeft className="w-3.5 h-3.5" />} label={user.isActive ? 'Suspender' : 'Activar'} onClick={() => onToggleSuspend(user.id)} color="yellow" />
      <ActionBtn icon={<Trash2 className="w-3.5 h-3.5" />} label="Eliminar" onClick={() => onDelete(user)} color="red" />
    </div>
  )
}

function MobileActions({ user, onEdit, onRenew, onAddConnection, onToggleSuspend, onDelete }) {
  return (
    <div className="grid grid-cols-5 gap-1.5">
      <ActionBtn icon={<Edit3 className="w-3.5 h-3.5" />} label="" onClick={() => onEdit(user)} color="blue" />
      <ActionBtn icon={<RefreshCw className="w-3.5 h-3.5" />} label="" onClick={() => onRenew(user)} color="green" />
      <ActionBtn icon={<Plus className="w-3.5 h-3.5" />} label="" onClick={() => onAddConnection(user.id)} color="cyan" />
      <ActionBtn icon={<ToggleLeft className="w-3.5 h-3.5" />} label="" onClick={() => onToggleSuspend(user.id)} color="yellow" />
      <ActionBtn icon={<Trash2 className="w-3.5 h-3.5" />} label="" onClick={() => onDelete(user)} color="red" />
    </div>
  )
}

function ActionBtn({ icon, label, onClick, color }) {
  const colors = {
    blue: 'bg-blue-600/20 hover:bg-blue-600/40 text-blue-300',
    green: 'bg-green-600/20 hover:bg-green-600/40 text-green-300',
    cyan: 'bg-cyan-600/20 hover:bg-cyan-600/40 text-cyan-300',
    yellow: 'bg-yellow-600/20 hover:bg-yellow-600/40 text-yellow-300',
    red: 'bg-red-600/20 hover:bg-red-600/40 text-red-300',
  }
  return (
    <button onClick={onClick}
      className={`${colors[color] || colors.blue} px-2 py-1.5 rounded-lg transition-colors text-xs flex items-center justify-center gap-1`}
      title={label}>
      {icon}
      {label && <span className="hidden sm:inline">{label}</span>}
    </button>
  )
}