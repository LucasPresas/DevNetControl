import { useState, useEffect } from 'react'
import { X, Check, Loader2, Calendar } from 'lucide-react'
import { renewPlan, getPlans } from '../../lib/api'

export default function RenewPlanModal({ show, onClose, user, onSuccess }) {
  const [plans, setPlans] = useState([])
  const [selectedPlanId, setSelectedPlanId] = useState('')
  const [durationHours, setDurationHours] = useState('')
  const [loading, setLoading] = useState(false)
  const [loadingPlans, setLoadingPlans] = useState(true)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    if (show) {
      fetchPlans()
      setMessage(null)
      setSelectedPlanId('')
      setDurationHours('')
    }
  }, [show])

  async function fetchPlans() {
    setLoadingPlans(true)
    try {
      const { data } = await getPlans()
      setPlans(data || [])
    } catch (err) {
      console.error('Error fetching plans:', err)
    } finally {
      setLoadingPlans(false)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setMessage(null)

    if (!selectedPlanId || selectedPlanId.trim() === '') {
      setMessage({ type: 'error', text: 'Debe seleccionar un plan válido' })
      setLoading(false)
      return
    }

    try {
      const payload = { 
        planId: selectedPlanId,
        durationHours: durationHours && parseInt(durationHours) > 0 ? parseInt(durationHours) : 0
      }

      const { data } = await renewPlan(user.id, payload)
      setMessage({ type: 'success', text: data.message || 'Plan renovado' })
      setTimeout(() => {
        onSuccess && onSuccess()
        onClose()
      }, 1000)
    } catch (err) {
      const errorMsg = err.response?.data?.message || err.response?.data || 'Error al renovar plan'
      setMessage({ type: 'error', text: errorMsg })
    } finally {
      setLoading(false)
    }
  }

  if (!show) return null

  const selectedPlan = plans.find(p => p.id === selectedPlanId)
  const creditCost = selectedPlan?.creditCost || 0

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="card p-6 max-w-md w-full">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-bold text-[var(--text-primary)]">Renovar Plan</h3>
          <button onClick={onClose} className="text-[var(--text-muted)] hover:text-[var(--text-primary)]">
            <X className="w-5 h-5" />
          </button>
        </div>

        <p className="text-sm text-[var(--text-secondary)] mb-4">
          Usuario: <span className="font-semibold">{user?.userName}</span>
        </p>

        {message && (
          <div className={`flex items-center gap-2 px-4 py-3 rounded-lg mb-4 text-sm ${
            message.type === 'success' ? 'bg-green-500/10 border border-green-500/30 text-green-400' : 'bg-red-500/10 border border-red-500/30 text-red-400'
          }`}>
            {message.type === 'success' ? <Check className="w-4 h-4" /> : <X className="w-4 h-4" />}
            {message.text}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Seleccionar Plan</label>
            {loadingPlans ? (
              <div className="flex items-center gap-2 text-sm text-[var(--text-muted)]">
                <Loader2 className="w-4 h-4 animate-spin" /> Cargando...
              </div>
            ) : (
              <select
                value={selectedPlanId}
                onChange={(e) => setSelectedPlanId(e.target.value)}
                className="input"
                required
              >
                <option value="">Seleccionar plan...</option>
                {plans.map(p => (
                  <option key={p.id} value={p.id}>
                    {p.name} - {p.creditCost} créditos - {p.durationHours}h
                  </option>
                ))}
              </select>
            )}
          </div>

          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Duración (horas, opcional)</label>
            <input
              type="number"
              value={durationHours}
              onChange={(e) => setDurationHours(e.target.value)}
              className="input"
              min="1"
              placeholder={`Por defecto: ${selectedPlan?.durationHours || 0} horas`}
            />
            <p className="text-xs text-[var(--text-muted)] mt-1">Dejar vacío para usar la duración del plan</p>
          </div>

          {selectedPlanId && (
            <div className="p-3 bg-yellow-500/10 border border-yellow-500/30 rounded-lg">
              <p className="text-sm text-yellow-400 flex items-center gap-1">
                <Calendar className="w-3.5 h-3.5" />
                Costo: <span className="font-semibold">{creditCost} créditos</span>
              </p>
              <p className="text-xs text-yellow-300/70 mt-1">
                Expira: {selectedPlan ? new Date(Date.now() + ((durationHours || selectedPlan.durationHours) * 3600000)).toLocaleString('es-AR') : ''}
              </p>
            </div>
          )}

          <div className="flex gap-2">
            <button type="submit" disabled={loading || !selectedPlanId} className="btn btn-primary flex-1">
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              Confirmar Renovación
            </button>
            <button type="button" onClick={onClose} className="btn btn-secondary">
              Cancelar
            </button>
          </div>
        </form>
      </div>
    </div>
  )
}
