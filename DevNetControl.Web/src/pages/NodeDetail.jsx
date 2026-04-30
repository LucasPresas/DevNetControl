import { useEffect, useState } from 'react'
import { useParams, Link } from 'react-router-dom'
import api from '../lib/api'
import { Server, Loader2, AlertCircle, Check, X, Terminal, Activity, ArrowLeft } from 'lucide-react'

export default function NodeDetail() {
  const { id } = useParams()
  const [node, setNode] = useState(null)
  const [metrics, setMetrics] = useState(null)
  const [command, setCommand] = useState('')
  const [output, setOutput] = useState('')
  const [loading, setLoading] = useState(true)
  const [actionLoading, setActionLoading] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchNode()
  }, [id])

  async function fetchNode() {
    try {
      const { data } = await api.get(`/vpsnode/${id}`)
      setNode(data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleTestConnection() {
    setActionLoading(true)
    setMessage(null)
    try {
      const { data } = await api.post(`/vpsnode/${id}/test-connection`)
      setMessage({ type: 'success', text: data.message })
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error de conexion' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleFetchMetrics() {
    setActionLoading(true)
    setMetrics(null)
    try {
      const { data } = await api.get(`/vpsnode/${id}/metrics`)
      setMetrics(data.metrics)
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error obteniendo metricas' })
    } finally {
      setActionLoading(false)
    }
  }

  async function handleExecuteCommand(e) {
    e.preventDefault()
    if (!command.trim()) return

    setActionLoading(true)
    setOutput('')
    try {
      const { data } = await api.post(`/vpsnode/${id}/execute`, { command })
      setOutput(data.output || data.error || 'Sin salida')
    } catch (err) {
      setOutput(`Error: ${err.response?.data?.message || 'Error ejecutando comando'}`)
    } finally {
      setActionLoading(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
      </div>
    )
  }

  if (!node) {
    return (
      <div className="card flex flex-col items-center justify-center py-16">
        <AlertCircle className="w-12 h-12 text-red-400 mx-auto mb-3" />
        <p className="text-[var(--text-muted)]">Nodo no encontrado</p>
        <Link to="/nodes" className="btn btn-primary mt-4">
          <ArrowLeft className="w-4 h-4" />
          Volver a nodos
        </Link>
      </div>
    )
  }

  return (
    <div className="max-w-7xl mx-auto space-y-4">
      {/* Header */}
      <div className="flex items-center gap-3">
        <Link to="/nodes" className="p-2 rounded-lg hover:bg-[var(--bg-hover)] text-[var(--text-secondary)]">
          <ArrowLeft className="w-5 h-5" />
        </Link>
        <div className="w-10 h-10 bg-blue-500/10 rounded-lg flex items-center justify-center">
          <Server className="w-5 h-5 text-blue-500" />
        </div>
        <div>
          <h1 className="text-lg font-bold text-[var(--text-primary)]">{node.label}</h1>
          <p className="text-sm text-[var(--text-muted)] font-mono">{node.ip}:{node.sshPort}</p>
        </div>
      </div>

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-lg text-sm ${
          message.type === 'success' ? 'bg-green-500/10 border border-green-500/30 text-green-400' : 'bg-red-500/10 border border-red-500/30 text-red-400'
        }`}>
          {message.type === 'success' ? <Check className="w-4 h-4" /> : <X className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Actions */}
      <div className="grid grid-cols-2 gap-2">
        <button onClick={handleTestConnection} disabled={actionLoading} className="btn btn-secondary py-3">
          <Activity className="w-4 h-4" />
          Test Conexion
        </button>
        <button onClick={handleFetchMetrics} disabled={actionLoading} className="btn btn-secondary py-3">
          <Activity className="w-4 h-4" />
          Metricas
        </button>
      </div>

      {/* Metrics */}
      {metrics && (
        <div className="card p-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4 flex items-center gap-2">
            <Activity className="w-4 h-4 text-blue-500" />
            Metricas del servidor
          </h3>
          <div className="grid grid-cols-2 gap-3">
            {Object.entries(metrics).map(([key, value]) => (
              <div key={key} className="p-3 rounded-lg bg-[var(--bg-primary)] border border-[var(--border-color)]">
                <p className="text-xs text-[var(--text-muted)] uppercase tracking-wide">{key}</p>
                <p className="font-semibold text-[var(--text-primary)] text-sm mt-0.5">{value}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Command Execution */}
      <div className="card">
        <div className="px-4 py-3 border-b border-[var(--border-color)]">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
            <Terminal className="w-4 h-4 text-green-500" />
            Ejecutar comando
          </h3>
        </div>

        <form onSubmit={handleExecuteCommand} className="p-4 space-y-3">
          <div className="flex gap-2">
            <input
              type="text"
              value={command}
              onChange={(e) => setCommand(e.target.value)}
              className="input font-mono text-sm"
              placeholder="uptime"
              required
            />
            <button type="submit" disabled={actionLoading} className="btn btn-primary">
              {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Ejecutar'}
            </button>
          </div>

          {output && (
            <div className="bg-[var(--bg-primary)] border border-[var(--border-color)] rounded-lg p-3 font-mono text-sm text-green-400 whitespace-pre-wrap max-h-48 overflow-y-auto">
              {output}
            </div>
          )}
        </form>
      </div>
    </div>
  )
}
