import { useEffect, useState } from 'react'
import api from '../lib/api'
import { Shield, Users, PlusCircle, Loader2, AlertCircle, CheckCircle, Wallet, Edit3, Trash2 } from 'lucide-react'

export default function AdminPanel() {
  const [users, setUsers] = useState([])
  const [loading, setLoading] = useState(true)
  const [showAddCredits, setShowAddCredits] = useState(false)
  const [selectedUser, setSelectedUser] = useState(null)
  const [creditsAmount, setCreditsAmount] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchUsers()
  }, [])

  async function fetchUsers() {
    try {
      const { data } = await api.get('/admin/users')
      setUsers(data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleAddCredits(e) {
    e.preventDefault()
    if (!selectedUser || !creditsAmount) return

    setSubmitting(true)
    setMessage(null)

    try {
      const { data } = await api.post(`/admin/users/${selectedUser}/add-credits`, {
        amount: parseFloat(creditsAmount),
      })
      setMessage({ type: 'success', text: data.message })
      setCreditsAmount('')
      setShowAddCredits(false)
      fetchUsers()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error agregando créditos' })
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDeleteUser(id, userName) {
    if (!confirm(`¿Estás seguro de eliminar a "${userName}"?`)) return

    try {
      const { data } = await api.delete(`/admin/users/${id}`)
      setMessage({ type: 'success', text: data.message })
      fetchUsers()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error eliminando usuario' })
    }
  }

  const roleLabels = { 0: 'Admin', 1: 'Reseller', 2: 'SubReseller', 3: 'Customer' }
  const roleColors = {
    0: 'bg-red-100 text-red-700',
    1: 'bg-purple-100 text-purple-700',
    2: 'bg-blue-100 text-blue-700',
    3: 'bg-gray-100 text-gray-700',
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center gap-3">
        <div className="w-10 h-10 bg-red-100 rounded-xl flex items-center justify-center">
          <Shield className="w-5 h-5 text-red-600" />
        </div>
        <div>
          <h2 className="text-xl font-bold text-gray-900">Panel Admin</h2>
          <p className="text-sm text-gray-400">{users.length} usuarios registrados</p>
        </div>
      </div>

      {/* Add Credits Form */}
      {showAddCredits && (
        <form onSubmit={handleAddCredits} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 space-y-3">
          <h3 className="font-semibold text-gray-900 flex items-center gap-2">
            <PlusCircle className="w-4 h-4" />
            Agregar créditos
          </h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Usuario</label>
            <select
              value={selectedUser || ''}
              onChange={(e) => setSelectedUser(e.target.value)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
              required
            >
              <option value="">Seleccionar usuario...</option>
              {users.map((u) => (
                <option key={u.id} value={u.id}>{u.userName} ({roleLabels[u.role]})</option>
              ))}
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Monto</label>
            <input
              type="number"
              value={creditsAmount}
              onChange={(e) => setCreditsAmount(e.target.value)}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
              placeholder="0"
              min="1"
              required
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-primary-600 text-white py-2.5 rounded-lg font-medium hover:bg-primary-700 disabled:bg-primary-400 transition-colors flex items-center justify-center gap-2"
            >
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Wallet className="w-4 h-4" />}
              Agregar
            </button>
            <button
              type="button"
              onClick={() => { setShowAddCredits(false); setMessage(null) }}
              className="px-4 py-2.5 border border-gray-300 rounded-lg text-gray-600 hover:bg-gray-50 transition-colors"
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm ${
          message.type === 'success' ? 'bg-green-50 text-green-700' : 'bg-red-50 text-red-700'
        }`}>
          {message.type === 'success' ? <CheckCircle className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Quick Actions */}
      <div className="grid grid-cols-2 gap-2">
        <button
          onClick={() => setShowAddCredits(!showAddCredits)}
          className="flex items-center justify-center gap-2 bg-primary-50 text-primary-700 py-3 rounded-xl font-medium hover:bg-primary-100 transition-colors"
        >
          <PlusCircle className="w-4 h-4" />
          Agregar créditos
        </button>
      </div>

      {/* Users Table */}
      {loading ? (
        <div className="flex items-center justify-center py-12">
          <Loader2 className="w-8 h-8 animate-spin text-primary-600" />
        </div>
      ) : users.length === 0 ? (
        <div className="bg-white rounded-xl p-8 text-center shadow-sm border border-gray-100">
          <Users className="w-12 h-12 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500">No hay usuarios registrados</p>
        </div>
      ) : (
        <div className="bg-white rounded-xl shadow-sm border border-gray-100 divide-y divide-gray-100">
          {users.map((u) => (
            <div key={u.id} className="px-4 py-3 flex items-center justify-between">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 bg-primary-100 rounded-full flex items-center justify-center">
                  <span className="text-primary-700 font-semibold text-sm">{u.userName.charAt(0).toUpperCase()}</span>
                </div>
                <div>
                  <p className="font-medium text-gray-900">{u.userName}</p>
                  <span className={`inline-block text-xs px-2 py-0.5 rounded-full font-medium ${roleColors[u.role]}`}>
                    {roleLabels[u.role]}
                  </span>
                </div>
              </div>
              <div className="flex items-center gap-2">
                <div className="text-right">
                  <p className="font-semibold text-sm text-gray-900">{u.credits?.toLocaleString() ?? 0}</p>
                  <p className="text-xs text-gray-400">{u.subordinatesCount ?? 0} hijos</p>
                </div>
                {u.role !== 0 && (
                  <button
                    onClick={() => handleDeleteUser(u.id, u.userName)}
                    className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    aria-label="Eliminar usuario"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                )}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
