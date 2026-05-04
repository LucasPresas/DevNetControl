import { useState, useEffect } from 'react'
import { X, Check, Loader2 } from 'lucide-react'
import { updateUserBasic } from '../../lib/api'

export default function EditUserModal({ show, onClose, user, onSuccess }) {
  const [form, setForm] = useState({
    userName: '',
    password: '',
    parentId: '',
    maxConnections: '',
  })
  const [loading, setLoading] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    if (show && user) {
      setForm({
        userName: user.userName || '',
        password: '',
        parentId: user.parentId || '',
        maxConnections: user.additionalConnections || 0,
      })
      setMessage(null)
    }
  }, [show, user])

  async function handleSubmit(e) {
    e.preventDefault()
    setLoading(true)
    setMessage(null)

    const data = {}
    if (form.userName && form.userName !== user.userName) data.userName = form.userName
    if (form.password) data.password = form.password
    if (form.parentId && form.parentId !== user.parentId) data.parentId = form.parentId
    if (form.maxConnections !== '' && parseInt(form.maxConnections) !== user.additionalConnections) {
      data.maxConnections = parseInt(form.maxConnections)
    }

    if (Object.keys(data).length === 0) {
      setMessage({ type: 'error', text: 'No hay cambios para guardar' })
      setLoading(false)
      return
    }

    try {
      const { data: response } = await updateUserBasic(user.id, data)
      setMessage({ type: 'success', text: response.message || 'Usuario actualizado' })
      setTimeout(() => {
        onSuccess && onSuccess()
        onClose()
      }, 1000)
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al actualizar usuario' })
    } finally {
      setLoading(false)
    }
  }

  if (!show) return null

  return (
    <div className="fixed inset-0 bg-black/60 flex items-center justify-center z-50 p-4">
      <div className="card p-6 max-w-md w-full">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-bold text-[var(--text-primary)]">Editar Usuario</h3>
          <button onClick={onClose} className="text-[var(--text-muted)] hover:text-[var(--text-primary)]">
            <X className="w-5 h-5" />
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

        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Nombre de Usuario</label>
            <input
              type="text"
              value={form.userName}
              onChange={(e) => setForm({ ...form, userName: e.target.value })}
              className="input"
              placeholder="Dejar vacío para no cambiar"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Nueva Contraseña</label>
            <input
              type="password"
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              className="input"
              placeholder="Dejar vacío para no cambiar (mínimo 6 caracteres)"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">ID de Reseller (Padre)</label>
            <input
              type="text"
              value={form.parentId}
              onChange={(e) => setForm({ ...form, parentId: e.target.value })}
              className="input"
              placeholder="Dejar vacío para no cambiar (GUID válido)"
            />
          </div>

          <div>
            <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Conexiones Adicionales</label>
            <input
              type="number"
              value={form.maxConnections}
              onChange={(e) => setForm({ ...form, maxConnections: e.target.value })}
              className="input"
              min="0"
              placeholder="0"
            />
            <p className="text-xs text-[var(--text-muted)] mt-1">Actual: {user.additionalConnections || 0}</p>
          </div>

          <div className="flex gap-2">
            <button type="submit" disabled={loading} className="btn btn-primary flex-1">
              {loading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              Guardar
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
