import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import api from '../lib/api'
import { Server, Plus, Loader2, Check, X, Wifi, Terminal, Settings, Trash2 } from 'lucide-react'
import { bulkDeleteNodes } from '../lib/api'

export default function Nodes() {
  const [nodes, setNodes] = useState([])
  const [loading, setLoading] = useState(true)
  const [showCreate, setShowCreate] = useState(false)
  const [form, setForm] = useState({ label: '', ip: '', sshPort: '22', password: '' })
  const [submitting, setSubmitting] = useState(false)
  const [message, setMessage] = useState(null)
  const [testingNode, setTestingNode] = useState(null)
  const [selectedIds, setSelectedIds] = useState([])

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
        label: form.label,
        ip: form.ip,
        sshPort: parseInt(form.sshPort),
        password: form.password,
        creditCost: 0,
      })
      setMessage({ type: 'success', text: data.message })
      setForm({ label: '', ip: '', sshPort: '22', password: '' })
      setShowCreate(false)
      fetchNodes()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al crear nodo' })
    } finally {
      setSubmitting(false)
    }
  }

  async function handleDelete(id) {
    if (!confirm('Eliminar este nodo?')) return
    try {
      await api.delete(`/vpsnode/${id}`)
      setMessage({ type: 'success', text: 'Nodo eliminado' })
      fetchNodes()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al eliminar' })
    }
  }

  async function testConnection(id) {
    setTestingNode(id)
    try {
      const { data } = await api.post(`/vpsnode/${id}/test-connection`)
      setMessage({ type: 'success', text: data.message })
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error de conexion' })
    } finally {
      setTestingNode(null)
    }
  }

  function toggleSelect(id) {
    setSelectedIds(prev => prev.includes(id) ? prev.filter(i => i !== id) : [...prev, id])
  }

  function toggleSelectAll() {
    if (selectedIds.length === nodes.length) {
      setSelectedIds([])
    } else {
      setSelectedIds(nodes.map(n => n.id))
    }
  }

  async function handleBulkDelete() {
    if (selectedIds.length === 0) return

    const confirmDelete = confirm(`Eliminar ${selectedIds.length} nodos? Esta accion no se puede deshacer.`)
    if (!confirmDelete) return

    try {
      const { data } = await bulkDeleteNodes(selectedIds)
      setMessage({ type: 'success', text: data.message })
      setSelectedIds([])
      fetchNodes()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error al eliminar' })
    }
  }

  return (
    <div className="max-w-7xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Nodos VPS</h1>
          <p className="text-sm text-[var(--text-muted)]">{nodes.length} servidores registrados</p>
        </div>
        <button onClick={() => setShowCreate(!showCreate)} className="btn btn-primary">
          <Plus className="w-4 h-4" />
          Nuevo Nodo
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

      {showCreate && (
        <form onSubmit={handleSubmit} className="card p-4 mb-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4">Agregar Nodo VPS</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Etiqueta</label>
              <input type="text" value={form.label} onChange={(e) => setForm({ ...form, label: e.target.value })} className="input" placeholder="VPS-USA-01" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">IP</label>
              <input type="text" value={form.ip} onChange={(e) => setForm({ ...form, ip: e.target.value })} className="input" placeholder="192.168.1.1" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Puerto SSH</label>
              <input type="number" value={form.sshPort} onChange={(e) => setForm({ ...form, sshPort: e.target.value })} className="input" min="1" max="65535" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Password Root</label>
              <input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} className="input" placeholder="Contraseña del servidor" required />
            </div>
          </div>
          <div className="flex gap-2 mt-4">
            <button type="submit" disabled={submitting} className="btn btn-primary">
              {submitting ? <Loader2 className="w-4 h-4 animate-spin" /> : <Check className="w-4 h-4" />}
              Agregar
            </button>
            <button type="button" onClick={() => setShowCreate(false)} className="btn btn-secondary">Cancelar</button>
          </div>
        </form>
      )}

      {/* Bulk Actions */}
      {selectedIds.length > 0 && (
        <div className="flex items-center gap-2 mb-4">
          <span className="flex items-center text-sm text-[var(--text-secondary)] mr-1">
            {selectedIds.length} seleccionados
          </span>
          <button onClick={handleBulkDelete} className="btn btn-secondary text-sm text-red-400 border-red-500/30 hover:bg-red-500/10">
            <Trash2 className="w-4 h-4" />
            Eliminar
          </button>
        </div>
      )}

      {loading ? (
        <div className="flex items-center justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-blue-500" /></div>
      ) : nodes.length === 0 ? (
        <div className="card flex flex-col items-center justify-center py-16">
          <Server className="w-12 h-12 text-[var(--text-muted)] mb-3" />
          <p className="text-[var(--text-muted)]">No hay nodos registrados</p>
        </div>
      ) : (
        <div className="table-container overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th className="w-10">
                  <input
                    type="checkbox"
                    checked={selectedIds.length === nodes.length && nodes.length > 0}
                    onChange={toggleSelectAll}
                    className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                  />
                </th>
                <th>Nodo</th>
                <th>Direccion</th>
                <th>Propietario</th>
                <th>Estado</th>
                <th>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {nodes.map(n => (
                <tr key={n.id}>
                  <td>
                    <input
                      type="checkbox"
                      checked={selectedIds.includes(n.id)}
                      onChange={() => toggleSelect(n.id)}
                      className="w-4 h-4 rounded border-[var(--border-color)] bg-[var(--bg-primary)] text-blue-500 focus:ring-blue-500"
                    />
                  </td>
                  <td>
                    <div className="flex items-center gap-2">
                      <span className="status-dot online" />
                      <div>
                        <p className="font-medium text-[var(--text-primary)]">{n.label}</p>
                        <p className="text-xs text-[var(--text-muted)] font-mono">{n.ip}:{n.sshPort}</p>
                      </div>
                    </div>
                  </td>
                  <td className="font-mono text-sm text-[var(--text-secondary)]">{n.ip}:{n.sshPort}</td>
                  <td className="text-[var(--text-secondary)]">{n.ownerId ? 'Asignado' : '-'}</td>
                  <td>
                    <span className="badge badge-success">Online</span>
                  </td>
                  <td>
                    <div className="flex items-center gap-1">
                      <button onClick={() => testConnection(n.id)} disabled={testingNode === n.id} className="btn btn-sm btn-secondary">
                        {testingNode === n.id ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Wifi className="w-3.5 h-3.5" />}
                        Test
                      </button>
                      <Link to={`/nodes/${n.id}`} className="btn btn-sm btn-secondary">
                        <Terminal className="w-3.5 h-3.5" />
                        Gestion
                      </Link>
                      <button onClick={() => handleDelete(n.id)} className="btn btn-sm btn-danger">
                        <X className="w-3.5 h-3.5" />
                      </button>
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
