import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../lib/api'
import { Server, Plus, Loader2, AlertCircle, CheckCircle, Wifi, WifiOff } from 'lucide-react'

export default function Nodes() {
  const [nodes, setNodes] = useState([])
  const [loading, setLoading] = useState(true)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState({ ip: '', sshPort: '22', label: '', password: '' })
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchNodes()
  }, [])

  async function fetchNodes() {
    try {
      const { data } = await api.get('/vpsnode')
      setNodes(data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleSubmit(e) {
    e.preventDefault()
    setSubmitting(true)
    setMessage(null)

    try {
      const { data } = await api.post('/vpsnode', {
        ip: form.ip,
        sshPort: parseInt(form.sshPort),
        label: form.label,
        password: form.password,
        creditCost: 0,
      })
      setMessage({ type: 'success', text: data.message })
      setForm({ ip: '', sshPort: '22', label: '', password: '' })
      setShowForm(false)
      fetchNodes()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al crear nodo' })
    } finally {
      setSubmitting(false)
    }
  }

  async function handleTest(id) {
    try {
      const { data } = await api.post(`/vpsnode/${id}/test-connection`)
      setMessage({ type: 'success', text: data.message })
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error de conexión' })
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin text-primary-600" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h2 className="text-xl font-bold text-gray-900">Nodos VPS</h2>
        <button
          onClick={() => setShowForm(!showForm)}
          className="flex items-center gap-2 bg-primary-600 text-white px-3 py-2 rounded-lg text-sm font-medium hover:bg-primary-700 transition-colors"
        >
          <Plus className="w-4 h-4" />
          <span className="hidden sm:inline">Agregar</span>
        </button>
      </div>

      {/* Create Form */}
      {showForm && (
        <form onSubmit={handleSubmit} className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 space-y-3">
          <h3 className="font-semibold text-gray-900">Nuevo nodo</h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Etiqueta</label>
            <input
              type="text"
              value={form.label}
              onChange={(e) => setForm({ ...form, label: e.target.value })}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
              placeholder="VPS-Principal"
              required
            />
          </div>

          <div className="grid grid-cols-3 gap-2">
            <div className="col-span-2">
              <label className="block text-sm font-medium text-gray-700 mb-1">IP</label>
              <input
                type="text"
                value={form.ip}
                onChange={(e) => setForm({ ...form, ip: e.target.value })}
                className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
                placeholder="192.168.1.1"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Puerto</label>
              <input
                type="number"
                value={form.sshPort}
                onChange={(e) => setForm({ ...form, sshPort: e.target.value })}
                className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
                placeholder="22"
                required
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Contraseña SSH</label>
            <input
              type="password"
              value={form.password}
              onChange={(e) => setForm({ ...form, password: e.target.value })}
              className="w-full px-3 py-2.5 border border-gray-300 rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
              placeholder="Contraseña root"
              required
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={submitting}
              className="flex-1 bg-primary-600 text-white py-2.5 rounded-lg font-medium hover:bg-primary-700 disabled:bg-primary-400 transition-colors flex items-center justify-center gap-2"
            >
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <CheckCircle className="w-4 h-4" />}
              Crear
            </button>
            <button
              type="button"
              onClick={() => { setShowForm(false); setMessage(null) }}
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

      {/* Nodes List */}
      {nodes.length === 0 ? (
        <div className="bg-white rounded-xl p-8 text-center shadow-sm border border-gray-100">
          <Server className="w-12 h-12 text-gray-300 mx-auto mb-3" />
          <p className="text-gray-500">No tenés nodos configurados</p>
        </div>
      ) : (
        <div className="grid gap-3">
          {nodes.map((node) => (
            <Link
              key={node.id}
              to={`/nodes/${node.id}`}
              className="bg-white rounded-xl p-4 shadow-sm border border-gray-100 hover:border-primary-200 hover:shadow-md transition-all block"
            >
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className="w-10 h-10 bg-primary-100 rounded-lg flex items-center justify-center">
                    <Server className="w-5 h-5 text-primary-600" />
                  </div>
                  <div>
                    <p className="font-medium text-gray-900">{node.label}</p>
                    <p className="text-xs text-gray-400 font-mono">{node.ip}:{node.sshPort}</p>
                  </div>
                </div>
                <button
                  onClick={(e) => { e.preventDefault(); handleTest(node.id) }}
                  className="p-2 text-gray-400 hover:text-primary-600 hover:bg-primary-50 rounded-lg transition-colors"
                  aria-label="Test conexión"
                >
                  <Wifi className="w-4 h-4" />
                </button>
              </div>
            </Link>
          ))}
        </div>
      )}
    </div>
  )
}
