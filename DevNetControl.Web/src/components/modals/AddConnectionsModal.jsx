import { useState } from 'react'
import { X, Check, Loader2, PlusCircle } from 'lucide-react'
import { addConnection } from '../../lib/api'

export default function AddConnectionsModal({ show, onClose, user, onSuccess }) {
  const [connectionsToAdd, setConnectionsToAdd] = useState(1)
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState(null)

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setMessage(null)

    if (connectionsToAdd < 1) {
      setMessage({ type: 'error', text: 'Debe agregar al menos 1 conexión' })
      setLoading(false)
      return
    }

    try {
      const { data } = await addConnection(user.id, { connectionsToAdd })
      setMessage({ type: 'success', text: data.message || 'Conexiones agregadas' })
      setTimeout(() => {
        onSuccess && onSuccess()
        onClose()
      }, 1000)
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al agregar conexiones' })
    } finally {
      setLoading(false)
    }
  }

  if (!show) return null

  const creditCost = connectionsToAdd

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="card p-6 max-w-sm w-full">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-bold text-[var(--text-primary)]">Agregar Conexiones</h3>
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
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Cantidad de Conexiones</label>
            <input
              type="number"
              value={connectionsToAdd}
              onChange={(e) => setConnectionsToAdd(parseInt(e.target.value) || 1)}
              className="input"
              min="1"
              max="100"
              required
            />
            <p className="text-xs text-[var(--text-muted)] mt-1">
              Costo: <span className="text-yellow-400 font-semibold">{creditCost} crédito(s)</span>
            </p>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={loading} className="btn btn-primary flex-1">
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <PlusCircle className="w-4 h-4" />}
              Agregar
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
