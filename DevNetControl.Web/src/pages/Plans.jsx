import { useEffect, useState } from 'react'
import api from '../lib/api'
import { FileText, Plus, Loader2, Check, X, Edit, ToggleRight, ToggleLeft, Trash2, Clock } from 'lucide-react'

export default function Plans() {
  const [plans, setPlans] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [editingPlan, setEditingPlan] = useState(null)
  const [form, setForm] = useState({ name: '', description: '', durationType: 'days', durationValue: 30, creditCost: 1, maxConnections: 1, maxDevices: 1 })
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchPlans()
  }, [])

  async function fetchPlans() {
    try {
      const { data } = await api.get('/plan')
      setPlans(data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  function getDurationHours() {
    const val = parseInt(form.durationValue) || 0
    return form.durationType === 'hours' ? val : val * 24
  }

  function formatDurationDisplay(hours) {
    if (!hours || hours <= 0) return '-'
    if (hours >= 24 && hours % 24 === 0) return `${hours / 24} dias`
    return `${hours} horas`
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setMessage(null)

    const durationHours = getDurationHours()
    if (durationHours <= 0) {
      setMessage({ type: 'error', text: 'La duracion debe ser mayor a cero' })
      setSubmitting(false)
      return
    }

    try {
      const payload = {
        name: form.name,
        description: form.description,
        durationHours,
        creditCost: parseFloat(form.creditCost),
        maxConnections: parseInt(form.maxConnections),
        maxDevices: parseInt(form.maxDevices),
      }

      if (editingPlan) {
        await api.put(`/plan/${editingPlan.id}`, payload)
        setMessage({ type: 'success', text: 'Plan actualizado' })
      } else {
        await api.post('/plan', payload)
        setMessage({ type: 'success', text: 'Plan creado' })
      }
      resetForm()
      fetchPlans()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error' })
    } finally {
      setSubmitting(false)
    }
  }

  async function handleToggle(id) {
    await api.post(`/plan/${id}/toggle`)
    fetchPlans()
  }

  async function handleDelete(id) {
    if (!confirm('Eliminar este plan?')) return
    try {
      await api.delete(`/plan/${id}`)
      setMessage({ type: 'success', text: 'Plan eliminado' })
      fetchPlans()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error' })
    }
  }

  function resetForm() {
    setForm({ name: '', description: '', durationType: 'days', durationValue: 30, creditCost: 1, maxConnections: 1, maxDevices: 1 })
    setShowCreate(false)
    setEditingPlan(null)
  }

  function startEdit(plan) {
    setEditingPlan(plan)
    const hours = plan.durationHours
    const isDays = hours >= 24 && hours % 24 === 0
    setForm({
      name: plan.name,
      description: plan.description,
      durationType: isDays ? 'days' : 'hours',
      durationValue: isDays ? hours / 24 : hours,
      creditCost: plan.creditCost,
      maxConnections: plan.maxConnections,
      maxDevices: plan.maxDevices,
    })
  }

  function applyPreset(type) {
    const presets = {
      trial1h: { durationType: 'hours', durationValue: 1, creditCost: 0 },
      trial2h: { durationType: 'hours', durationValue: 2, creditCost: 0 },
      trial6h: { durationType: 'hours', durationValue: 6, creditCost: 0 },
      trial24h: { durationType: 'hours', durationValue: 24, creditCost: 0 },
      plan1d: { durationType: 'days', durationValue: 1, creditCost: 1 },
      plan3d: { durationType: 'days', durationValue: 3, creditCost: 3 },
      plan7d: { durationType: 'days', durationValue: 7, creditCost: 5 },
      plan30d: { durationType: 'days', durationValue: 30, creditCost: 10 },
    }
    const preset = presets[type]
    if (preset) setForm(prev => ({ ...prev, ...preset }))
  }

  return (
    <div className="max-w-7xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Planes</h1>
          <p className="text-sm text-[var(--text-muted)]">{plans.length} planes configurados</p>
        </div>
        <button onClick={() => { setShowCreate(true); setEditingPlan(null); resetForm() }} className="btn btn-primary">
          <Plus className="w-4 h-4" />
          Nuevo Plan
        </button>
      </div>

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-lg mb-4 text-sm ${
          message.type === 'success' ? 'bg-green-500/10 border border-green-500/30 text-green-400' : 'bg-red-500/10 border border-red-500/30 text-red-400'
        }`}>
          {message.type === 'success' ? <Check className="w-4 h-4" /> : <X className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Create/Edit Form */}
      {(showCreate || editingPlan) && (
        <form onSubmit={handleSubmit} className="card p-4 mb-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4">{editingPlan ? 'Editar Plan' : 'Crear Plan'}</h3>

          {/* Quick presets */}
          <div className="mb-4">
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-2">Presets rapidos</label>
            <div className="flex flex-wrap gap-1.5">
              <button type="button" onClick={() => applyPreset('trial1h')} className="px-2 py-1 rounded bg-yellow-500/10 border border-yellow-500/30 text-yellow-400 text-xs hover:bg-yellow-500/20">Trial 1h</button>
              <button type="button" onClick={() => applyPreset('trial2h')} className="px-2 py-1 rounded bg-yellow-500/10 border border-yellow-500/30 text-yellow-400 text-xs hover:bg-yellow-500/20">Trial 2h</button>
              <button type="button" onClick={() => applyPreset('trial6h')} className="px-2 py-1 rounded bg-yellow-500/10 border border-yellow-500/30 text-yellow-400 text-xs hover:bg-yellow-500/20">Trial 6h</button>
              <button type="button" onClick={() => applyPreset('trial24h')} className="px-2 py-1 rounded bg-yellow-500/10 border border-yellow-500/30 text-yellow-400 text-xs hover:bg-yellow-500/20">Trial 24h</button>
              <button type="button" onClick={() => applyPreset('plan1d')} className="px-2 py-1 rounded bg-blue-500/10 border border-blue-500/30 text-blue-400 text-xs hover:bg-blue-500/20">1 dia</button>
              <button type="button" onClick={() => applyPreset('plan3d')} className="px-2 py-1 rounded bg-blue-500/10 border border-blue-500/30 text-blue-400 text-xs hover:bg-blue-500/20">3 dias</button>
              <button type="button" onClick={() => applyPreset('plan7d')} className="px-2 py-1 rounded bg-blue-500/10 border border-blue-500/30 text-blue-400 text-xs hover:bg-blue-500/20">7 dias</button>
              <button type="button" onClick={() => applyPreset('plan30d')} className="px-2 py-1 rounded bg-blue-500/10 border border-blue-500/30 text-blue-400 text-xs hover:bg-blue-500/20">30 dias</button>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Nombre</label>
              <input type="text" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} className="input" placeholder="Trial 2 Horas" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Descripcion</label>
              <input type="text" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} className="input" placeholder="Plan de prueba gratuito" />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Duracion</label>
              <div className="flex gap-2">
                <select value={form.durationType} onChange={(e) => setForm({ ...form, durationType: e.target.value })} className="input w-28">
                  <option value="hours">Horas</option>
                  <option value="days">Dias</option>
                </select>
                <input type="number" min="1" max={form.durationType === 'hours' ? 87600 : 3650} value={form.durationValue} onChange={(e) => setForm({ ...form, durationValue: e.target.value })} className="input flex-1" required />
              </div>
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">
                Costo Creditos
                {form.creditCost == 0 && <span className="ml-1 text-yellow-400">(Gratis/Trial)</span>}
              </label>
              <input type="number" min="0" step="0.01" value={form.creditCost} onChange={(e) => setForm({ ...form, creditCost: e.target.value })} className="input" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Conexiones Max</label>
              <input type="number" min="1" max="100" value={form.maxConnections} onChange={(e) => setForm({ ...form, maxConnections: e.target.value })} className="input" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Dispositivos Max</label>
              <input type="number" min="1" max="100" value={form.maxDevices} onChange={(e) => setForm({ ...form, maxDevices: e.target.value })} className="input" required />
            </div>
          </div>

          {getDurationHours() > 0 && (
            <p className="text-xs text-[var(--text-muted)] mt-3 flex items-center gap-1">
              <Clock className="w-3 h-3" />
              Duracion total: {formatDurationDisplay(getDurationHours())}
            </p>
          )}

          <div className="flex gap-2 mt-4">
            <button type="submit" disabled={submitting} className="btn btn-primary">
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              {editingPlan ? 'Actualizar' : 'Crear'}
            </button>
            <button type="button" onClick={resetForm} className="btn btn-secondary">Cancelar</button>
          </div>
        </form>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-blue-500" /></div>
      ) : plans.length === 0 ? (
        <div className="card flex flex-col items-center justify-center py-16">
          <FileText className="w-12 h-12 text-[var(--text-muted)] mb-3" />
          <p className="text-[var(--text-muted)]">No hay planes configurados</p>
        </div>
      ) : (
        <div className="table-container overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Duracion</th>
                <th>Costo</th>
                <th>Conexiones</th>
                <th>Dispositivos</th>
                <th>Usuarios</th>
                <th>Tipo</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {plans.map(p => (
                <tr key={p.id}>
                  <td>
                    <p className="font-medium text-[var(--text-primary)]">{p.name}</p>
                    {p.description && <p className="text-xs text-[var(--text-muted)]">{p.description}</p>}
                  </td>
                  <td className="text-[var(--text-secondary)]">{formatDurationDisplay(p.durationHours)}</td>
                  <td className={`font-semibold ${p.creditCost === 0 ? 'text-green-400' : 'text-yellow-400'}`}>
                    {p.creditCost === 0 ? 'Gratis' : p.creditCost}
                  </td>
                  <td>{p.maxConnections}</td>
                  <td>{p.maxDevices}</td>
                  <td>
                    <span className="badge badge-info">{p.usersCount}</span>
                  </td>
                  <td>
                    {p.isTrial
                      ? <span className="badge badge-warning">Trial</span>
                      : <span className="badge badge-success">Pago</span>
                    }
                  </td>
                  <td>
                    {p.isActive
                      ? <span className="badge badge-success">Activo</span>
                      : <span className="badge badge-danger">Inactivo</span>
                    }
                  </td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button onClick={() => startEdit(p)} className="p-1 text-[var(--text-secondary)] hover:text-blue-400" title="Editar"><Edit className="w-4 h-4" /></button>
                      <button onClick={() => handleToggle(p.id)} className="p-1 text-[var(--text-secondary)] hover:text-green-400" title="Toggle">
                        {p.isActive ? <ToggleRight className="w-4 h-4 text-green-400" /> : <ToggleLeft className="w-4 h-4 text-[var(--text-muted)]" />}
                      </button>
                      <button onClick={() => handleDelete(p.id)} className="p-1 text-[var(--text-secondary)] hover:text-red-400" title="Eliminar"><Trash2 className="w-4 h-4" /></button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
