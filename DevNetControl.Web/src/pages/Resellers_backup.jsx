import { useEffect, useState } from 'react'
import api from '../lib/api'
import {
  UserPlus, Loader2, Search, Check, X, Users as UsersIcon,
  Trash2, Edit3, CreditCard, FileText, Copy, Shield, ShieldOff, Server,
  ChevronLeft, ChevronRight
} from 'lucide-react'
import { bulkDeleteResellers, bulkToggleSuspendResellers } from '../lib/api'

export default function Resellers() {
  const [resellers, setResellers] = useState([])
  const [plans, setPlans] = useState([])
  const [availableNodes, setAvailableNodes] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState({ userName: '', password: '', isSubReseller: false, planIds: [], initialCredits: 0, nodeIds: [] })
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)
  const [search, setSearch] = useState('')
  const [filter, setFilter] = useState('all')
  const [editingReseller, setEditingReseller] = useState(null)
  const [showEditModal, setShowEditModal] = useState(false)
  const [showCreditsModal, setShowCreditsModal] = useState(false)
  const [showPlansModal, setShowPlansModal] = useState(false)
  const [showNodesModal, setShowNodesModal] = useState(false)
  const [selectedReseller, setSelectedReseller] = useState(null)
  const [creditAmount, setCreditAmount] = useState(0)
  const [selectedPlanIds, setSelectedPlanIds] = useState([])
  const [selectedNodeIds, setSelectedNodeIds] = useState([])
  const [actionLoading, setActionLoading] = useState(false)
  const [selectedIds, setSelectedIds] = useState([])
  const [page, setPage] = useState(1)
  const pageSize = 10

  useEffect(() => {
    fetchData()
  }, [])

  async function fetchData() {
    try {
      const [resellersRes, plansRes, nodesRes] = await Promise.all([
        api.get('/user/my-resellers'),
        api.get('/plan'),
        api.get('/nodeaccess/my-nodes'),
      ])
      setResellers(resellersRes.data)
      setPlans(plansRes.data.filter(p => p.isActive))
      setAvailableNodes(nodesRes.data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleSubmitCreate(e) {
    e.preventDefault()
    setSubmitting(true)
    setMessage(null)

    try {
      const { data } = await api.post('/user/create-reseller', {
        userName: form.userName,
        password: form.password,
        isSubReseller: form.isSubReseller,
        planIds: form.planIds.length > 0 ? form.planIds : null,
        initialCredits: parseFloat(form.initialCredits) || 0,
        nodeIds: form.nodeIds.length > 0 ? form.nodeIds : null,
      })
      setMessage({ type: 'success', text: data.message })
      setForm({ userName: '', password: '', isSubReseller: false, planIds: [], initialCredits: 0, nodeIds: [] })
      setShowCreate(false)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al crear reseller' })
    } finally {
      setSubmitting(false)
    }
  }

  async function handleToggleSuspend(id) {
    setActionLoading(true)
    try {
      const { data } = await api.post(`/user/${id}/toggle-suspend`)
      setMessage({ type: 'success', text: data.message })
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleDelete(id, userName) {
    if (!confirm(`Eliminar reseller "${userName}" y todos sus usuarios? Esta accion no se puede deshacer.`)) return
    setActionLoading(true)
    try {
      const { data } = await api.delete(`/user/${id}`)
      setMessage({ type: 'success', text: data.message })
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al eliminar' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleLoadCredits() {
    setActionLoading(true)
    try {
      const { data } = await api.post(`/user/${selectedReseller.id}/load-credits`, { amount: parseFloat(creditAmount) })
      setMessage({ type: 'success', text: data.message })
      setShowCreditsModal(false)
      setSelectedReseller(null)
      setCreditAmount(0)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al cargar creditos' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleUpdatePlans() {
    setActionLoading(true)
    try {
      const { data } = await api.post(`/plan/user/${selectedReseller.id}/plans`, { planIds: selectedPlanIds })
      setMessage({ type: 'success', text: data.message })
      setShowPlansModal(false)
      setSelectedReseller(null)
      setSelectedPlanIds([])
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al actualizar planes' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleUpdateNodes() {
    setActionLoading(true)
    try {
      const { data } = await api.post(`/user/${selectedReseller.id}/nodes`, { nodeIds: selectedNodeIds })
      setMessage({ type: 'success', text: data.message })
      setShowNodesModal(false)
      setSelectedReseller(null)
      setSelectedNodeIds([])
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al actualizar nodos' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleUpdateReseller(e) {
    e.preventDefault()
    setActionLoading(true)
    try {
      const updates = {}
      if (editingReseller.userName !== selectedReseller.userName) updates.userName = editingReseller.userName
      if (editingReseller.password) updates.password = editingReseller.password

      if (Object.keys(updates).length > 0) {
        await api.put(`/user/${selectedReseller.id}`, updates)
      }
      setMessage({ type: 'success', text: 'Reseller actualizado' })
      setShowEditModal(false)
      setSelectedReseller(null)
      setEditingReseller(null)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al actualizar' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleBulkDelete() {
    if (selectedIds.length === 0) return
    if (!confirm(`Eliminar ${selectedIds.length} resellers? Esta accion no se puede deshacer.`)) return
    setActionLoading(true)
    setMessage(null)
    try {
      const { data } = await bulkDeleteResellers(selectedIds)
      setMessage({ type: 'success', text: data.message })
      setSelectedIds([])
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al eliminar' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleBulkToggleSuspend() {
    if (selectedIds.length === 0) return
    setActionLoading(true)
    setMessage(null)
    try {
      const { data } = await bulkToggleSuspendResellers(selectedIds)
      setMessage({ type: 'success', text: data.message })
      setSelectedIds([])
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al actualizar' })
    } finally {
      setActionLoading(false)
    }
  }

  function copyToClipboard(reseller) {
    const nodes = (reseller.nodes || []).join(', ') || 'Sin nodos'
    const duration = reseller.durationHours
      ? reseller.durationHours >= 24 && reseller.durationHours % 24 === 0
        ? `${reseller.durationHours / 24} dias`
        : `${reseller.durationHours} horas`
      : '-'
    const text = `*DevNetControl - Datos de acceso*\n\nUsuario: ${reseller.userName}\nPassword: (se envio por separado)\nPlan: ${reseller.planName || 'Sin plan'}\nDuracion: ${duration}\nCreditos: ${reseller.credits?.toLocaleString() ?? 0}\nNodos: ${nodes}`
    navigator.clipboard.writeText(text)
    setMessage({ type: 'success', text: 'Datos copiados al portapapeles' })
    setTimeout(() => setMessage(null), 3000)
  }

  function toggleNodeId(id) {
    setForm(prev => ({
      ...prev,
      nodeIds: prev.nodeIds.includes(id) ? prev.nodeIds.filter(n => n !== id) : [...prev.nodeIds, id]
    }))
  }

  function togglePlanId(id) {
    setForm(prev => ({
      ...prev,
      planIds: prev.planIds.includes(id) ? prev.planIds.filter(n => n !== id) : [...prev.planIds, id]
    }))
  }

  function toggleSelectedNodeId(id) {
    setSelectedNodeIds(prev => prev.includes(id) ? prev.filter(n => n !== id) : [...prev, id])
  }

  function toggleSelectedPlanId(id) {
    setSelectedPlanIds(prev => prev.includes(id) ? prev.filter(n => n !== id) : [...prev, id])
  }

  function toggleSelect(id) {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id])
  }

  function toggleSelectAll() {
    if (selectedIds.length === filteredResellers.length) {
      setSelectedIds([])
    } else {
      setSelectedIds(filteredResellers.map(r => r.id))
    }
  }

  const filteredResellers = resellers.filter(r => {
    if (search && !r.userName.toLowerCase().includes(search.toLowerCase())) return false
    if (filter === 'active') return r.isActive
    if (filter === 'suspended') return !r.isActive
    return true
  })

  const totalPages = Math.ceil(filteredResellers.length / pageSize)
  const paginatedResellers = filteredResellers.slice((page - 1) * pageSize, page * pageSize)

  function openCreditsModal(r) { setSelectedReseller(r); setCreditAmount(0); setShowCreditsModal(true) }
  function openPlansModal(r) {
    setSelectedReseller(r)
    api.get(`/plan/user/${r.id}/plans`).then(res => {
      setSelectedPlanIds(res.data.map(p => p.id))
    }).catch(() => setSelectedPlanIds([]))
    setShowPlansModal(true)
  }
  function openNodesModal(r) {
    setSelectedReseller(r)
    api.get(`/nodeaccess/user/${r.id}`).then(res => {
      setSelectedNodeIds(res.data.map(n => n.nodeId))
    }).catch(() => setSelectedNodeIds([]))
    setShowNodesModal(true)
  }
  function openEditModal(r) { setSelectedReseller(r); setEditingReseller({ userName: r.userName, password: '' }); setShowEditModal(true) }

  return (
    <div className="max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Resellers</h1>
          <p className="text-sm text-[var(--text-muted)]">{resellers.length} resellers registrados</p>
        </div>
        <button onClick={() => setShowCreate(!showCreate)} className="btn btn-primary">
          <UserPlus className="w-4 h-4" />
          Nuevo Reseller
        </button>
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

      {/* Create Form */}
      {showCreate && (
        <form onSubmit={handleSubmitCreate} className="card p-4 mb-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4">Crear Nuevo Reseller</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Username</label>
              <input type="text" value={form.userName} onChange={(e) => setForm({ ...form, userName: e.target.value })} className="input" placeholder="reseller123" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Password</label>
              <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} className="input" placeholder="Minimo 6 caracteres" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Tipo</label>
              <select value={form.isSubReseller ? 'sub' : 'reseller'} onChange={(e) => setForm({ ...form, isSubReseller: e.target.value === 'sub' })} className="input">
                <option value="reseller">Reseller</option>
                <option value="sub">Sub-Reseller</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Creditos iniciales</label>
              <input type="number" value={form.initialCredits} onChange={(e) => setForm({ ...form, initialCredits: e.target.value })} className="input" min="0" step="0.01" />
            </div>
          </div>

          {/* Plan selection */}
          <div className="mt-4">
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-2">Planes disponibles</label>
            <div className="flex flex-wrap gap-2">
              {plans.length === 0 && <p className="text-sm text-[var(--text-muted)]">No hay planes disponibles</p>}
              {plans.map(p => (
                <button
                  key={p.id}
                  type="button"
                  onClick={() => togglePlanId(p.id)}
                  className={`px-3 py-1.5 rounded-md text-sm border transition-colors ${
                    form.planIds.includes(p.id)
                      ? 'bg-blue-500/20 border-blue-500/50 text-blue-400'
                      : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-secondary)] hover:border-[var(--text-muted)]'
                  }`}
                >
                  {p.name} {p.creditCost === 0 && <span className="text-yellow-400">(Gratis)</span>}
                </button>
              ))}
            </div>
          </div>

          {/* Node selection */}
          <div className="mt-4">
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-2">Nodos con acceso</label>
            <div className="flex flex-wrap gap-2">
              {availableNodes.length === 0 && <p className="text-sm text-[var(--text-muted)]">No hay nodos disponibles</p>}
              {availableNodes.map(n => (
                <button
                  key={n.id}
                  type="button"
                  onClick={() => toggleNodeId(n.id)}
                  className={`px-3 py-1.5 rounded-md text-sm border transition-colors ${
                    form.nodeIds.includes(n.id)
                      ? 'bg-blue-500/20 border-blue-500/50 text-blue-400'
                      : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-secondary)] hover:border-[var(--text-muted)]'
                  }`}
                >
                  <Server className="w-3 h-3 inline mr-1" />
                  {n.label}
                </button>
              ))}
            </div>
          </div>

          <div className="flex gap-2 mt-4">
            <button type="submit" disabled={submitting} className="btn btn-primary">
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              Crear Reseller
            </button>
            <button type="button" onClick={() => setShowCreate(false)} className="btn btn-secondary">Cancelar</button>
          </div>
        </form>
      )}

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
          <input type="text" value={search} onChange={(e) => setSearch(e.target.value)} className="input pl-10" placeholder="Buscar reseller..." />
        </div>
        <div className="flex gap-1.5">
          {[
            { key: 'all', label: 'Todos' },
            { key: 'active', label: 'Activos' },
            { key: 'suspended', label: 'Suspendidos' },
          ].map(f => (
            <button key={f.key} onClick={() => { setFilter(f.key); setPage(1); }} className={`btn btn-sm ${filter === f.key ? 'btn-primary' : 'btn-secondary'}`}>
              {f.label}
            </button>
          ))}
        </div>
      </div>

      {/* Bulk Actions */}
      {selectedIds.length > 0 && (
        <div className="flex items-center gap-2 mb-4">
          <span className="flex items-center text-sm text-[var(--text-secondary)] mr-1">
            {selectedIds.length} seleccionados
          </span>
          <button onClick={() => handleBulkToggleSuspend()} className="btn btn-secondary text-sm">
            {filteredResellers.find(r => selectedIds.includes(r.id))?.isActive ? 'Suspender' : 'Activar'}
          </button>
          <button onClick={handleBulkDelete} className="btn btn-secondary text-sm text-red-400 border-red-500/30 hover:bg-red-500/10">
            <Trash2 className="w-4 h-4" />
            Eliminar
          </button>
        </div>
      )}

      {/* Table */}
      {loading ? (
        <div className="flex items-center justify-center py-16">
          <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
        </div>
      ) : paginatedResellers.length === 0 ? (
        <div className="card flex flex-col items-center justify-center py-16">
          <UsersIcon className="w-12 h-12 text-[var(--text-muted)] mb-3" />
          <p className="text-[var(--text-muted)]">No se encontraron resellers</p>
        </div>
      ) : (
        <div className="table-container overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th className="w-10">
                  <input
                    type="checkbox"
                    checked={selectedIds.length === filteredResellers.length && filteredResellers.length > 0}
                    onChange={toggleSelectAll}
                    className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                  />
                </th>
                <th>Reseller</th>
                <th>Tipo</th>
                <th>Planes</th>
                <th>Creditos</th>
                <th>Nodos</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {paginatedResellers.map(r => (
                <tr key={r.id}>
                  <td>
                    <input
                      type="checkbox"
                      checked={selectedIds.includes(r.id)}
                      onChange={() => toggleSelect(r.id)}
                      className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                    />
                  </td>
                  <td>
                    <div className="flex items-center gap-2">
                      <span className={`status-dot ${r.isActive ? 'online' : 'offline'}`} />
                      <div>
                        <p className="font-medium text-[var(--text-primary)]">{r.userName}</p>
                      </div>
                    </div>
                  </td>
                  <td>
                    <span className={`badge ${r.role === 'SubReseller' ? 'badge-info' : 'badge-warning'}`}>
                      {r.role === 'SubReseller' ? 'Sub-Reseller' : 'Reseller'}
                    </span>
                  </td>
                  <td>
                    {(r.plans || []).length > 0
                      ? (
                        <div className="flex flex-wrap gap-1 max-w-48">
                          {r.plans.map((p, i) => (
                            <span key={i} className={`badge text-xs ${p.isTrial ? 'badge-warning' : 'badge-success'}`}>{p.name}</span>
                          ))}
                        </div>
                      )
                      : <span className="text-[var(--text-muted)] text-xs">Sin planes</span>
                    }
                  </td>
                  <td className="font-semibold text-[var(--text-primary)]">{r.credits?.toLocaleString() ?? 0}</td>
                  <td>
                    {(r.nodes || []).length > 0
                      ? (
                        <div className="flex flex-wrap gap-1 max-w-48">
                          {r.nodes.map((n, i) => (
                            <span key={i} className="badge badge-info text-xs">{n}</span>
                          ))}
                        </div>
                      )
                      : <span className="text-[var(--text-muted)] text-xs">Sin nodos</span>
                    }
                  </td>
                  <td>
                    {r.isActive
                      ? <span className="badge badge-success">Activo</span>
                      : <span className="badge badge-danger">Suspendido</span>
                    }
                  </td>
                  <td>
                    <div className="flex items-center gap-1 flex-wrap">
                      <button onClick={() => copyToClipboard(r)} title="Copiar datos" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-blue-400 transition-colors">
                        <Copy className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openEditModal(r)} title="Editar" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-green-400 transition-colors">
                        <Edit3 className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openCreditsModal(r)} title="Cargar creditos" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-yellow-400 transition-colors">
                        <CreditCard className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openPlansModal(r)} title="Gestionar planes" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-purple-400 transition-colors">
                        <FileText className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => openNodesModal(r)} title="Gestionar nodos" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-cyan-400 transition-colors">
                        <Server className="w-3.5 h-3.5" />
                      </button>
                      <button onClick={() => handleToggleSuspend(r.id)} disabled={actionLoading} title={r.isActive ? 'Suspender' : 'Activar'} className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-orange-400 transition-colors">
                        {r.isActive ? <ShieldOff className="w-3.5 h-3.5" /> : <Shield className="w-3.5 h-3.5" />}
                      </button>
                      <button onClick={() => handleDelete(r.id, r.userName)} disabled={actionLoading} title="Eliminar" className="p-1.5 rounded-md hover:bg-[var(--bg-hover)] text-[var(--text-secondary)] hover:text-red-400 transition-colors">
                        <Trash2 className="w-3.5 h-3.5" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="flex items-center justify-between mt-4 px-2">
          <p className="text-sm text-[var(--text-muted)]">
            Mostrando {(page - 1) * pageSize + 1}-{Math.min(page * pageSize, filteredResellers.length)} de {filteredResellers.length}
          </p>
          <div className="flex gap-1">
            <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1} className="btn btn-secondary btn-sm">
              <ChevronLeft className="w-4 h-4" />
            </button>
            {Array.from({ length: totalPages }, (_, i) => i + 1).map(p => (
              <button key={p} onClick={() => setPage(p)} className={`btn btn-sm ${page === p ? 'btn-primary' : 'btn-secondary'}`}>
                {p}
              </button>
            ))}
            <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages} className="btn btn-secondary btn-sm">
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      )}

      {/* Edit Modal */}
      {showEditModal && selectedReseller && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
          <div className="card p-6 max-w-sm w-full">
            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Editar Reseller</h3>
            <form onSubmit={handleUpdateReseller} className="space-y-3">
              <div>
                <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Username</label>
                <input type="text" value={editingReseller?.userName || ''} onChange={(e) => setEditingReseller({ ...editingReseller, userName: e.target.value })} className="input" required />
              </div>
              <div>
                <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Nueva password (dejar vacio para no cambiar)</label>
                <input type="password" value={editingReseller?.password || ''} onChange={(e) => setEditingReseller({ ...editingReseller, password: e.target.value })} className="input" placeholder="Nueva password" />
              </div>
              <div className="flex gap-2 mt-4">
                <button type="submit" disabled={actionLoading} className="btn btn-primary flex-1">
                  {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
                  Guardar
                </button>
                <button type="button" onClick={() => { setShowEditModal(false); setSelectedReseller(null) }} className="btn btn-secondary">Cancelar</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Credits Modal */}
      {showCreditsModal && selectedReseller && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
          <div className="card p-6 max-w-sm w-full">
            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Cargar Creditos</h3>
            <p className="text-sm text-[var(--text-secondary)] mb-3">Reseller: <span className="font-medium text-[var(--text-primary)]">{selectedReseller.userName}</span></p>
            <p className="text-sm text-[var(--text-secondary)] mb-3">Saldo actual: <span className="font-semibold text-yellow-400">{selectedReseller.credits?.toLocaleString() ?? 0}</span></p>
            <div className="mb-4">
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Cantidad a cargar</label>
              <input type="number" value={creditAmount} onChange={(e) => setCreditAmount(e.target.value)} className="input" min="0" step="0.01" placeholder="0" />
            </div>
            <div className="flex gap-2">
              <button onClick={handleLoadCredits} disabled={actionLoading || !creditAmount} className="btn btn-primary flex-1">
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <CreditCard className="w-4 h-4" />}
                Confirmar
              </button>
              <button onClick={() => { setShowCreditsModal(false); setSelectedReseller(null) }} className="btn btn-secondary">Cancelar</button>
            </div>
          </div>
        </div>
      )}

      {/* Plans Modal */}
      {showPlansModal && selectedReseller && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
          <div className="card p-6 max-w-md w-full">
            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Gestionar Planes</h3>
            <p className="text-sm text-[var(--text-secondary)] mb-3">Reseller: <span className="font-medium text-[var(--text-primary)]">{selectedReseller.userName}</span></p>
            <div className="mb-4">
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-2">Seleccionar planes disponibles</label>
              <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto">
                {plans.length === 0 && <p className="text-sm text-[var(--text-muted)]">No hay planes disponibles</p>}
                {plans.map(p => (
                  <button
                    key={p.id}
                    type="button"
                    onClick={() => toggleSelectedPlanId(p.id)}
                    className={`px-3 py-1.5 rounded-md text-sm border transition-colors ${
                      selectedPlanIds.includes(p.id)
                        ? 'bg-blue-500/20 border-blue-500/50 text-blue-400'
                        : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-secondary)] hover:border-[var(--text-muted)]'
                    }`}
                  >
                    {p.name} {p.creditCost === 0 && <span className="text-yellow-400">(Gratis)</span>}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex gap-2">
              <button onClick={handleUpdatePlans} disabled={actionLoading} className="btn btn-primary flex-1">
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <FileText className="w-4 h-4" />}
                Guardar
              </button>
              <button onClick={() => { setShowPlansModal(false); setSelectedReseller(null) }} className="btn btn-secondary">Cancelar</button>
            </div>
          </div>
        </div>
      )}

      {/* Nodes Modal */}
      {showNodesModal && selectedReseller && (
        <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
          <div className="card p-6 max-w-md w-full">
            <h3 className="text-lg font-bold text-[var(--text-primary)] mb-4">Gestionar Nodos</h3>
            <p className="text-sm text-[var(--text-secondary)] mb-3">Reseller: <span className="font-medium text-[var(--text-primary)]">{selectedReseller.userName}</span></p>
            <div className="mb-4">
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-2">Seleccionar nodos con acceso</label>
              <div className="flex flex-wrap gap-2 max-h-48 overflow-y-auto">
                {availableNodes.length === 0 && <p className="text-sm text-[var(--text-muted)]">No hay nodos disponibles</p>}
                {availableNodes.map(n => (
                  <button
                    key={n.id}
                    type="button"
                    onClick={() => toggleSelectedNodeId(n.id)}
                    className={`px-3 py-1.5 rounded-md text-sm border transition-colors ${
                      selectedNodeIds.includes(n.id)
                        ? 'bg-blue-500/20 border-blue-500/50 text-blue-400'
                        : 'bg-[var(--bg-primary)] border-[var(--border-color)] text-[var(--text-secondary)] hover:border-[var(--text-muted)]'
                    }`}
                  >
                    <Server className="w-3 h-3 inline mr-1" />
                    {n.label}
                  </button>
                ))}
              </div>
            </div>
            <div className="flex gap-2">
              <button onClick={handleUpdateNodes} disabled={actionLoading} className="btn btn-primary flex-1">
                {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Server className="w-4 h-4" />}
                Guardar
              </button>
              <button onClick={() => { setShowNodesModal(false); setSelectedReseller(null) }} className="btn btn-secondary">Cancelar</button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
