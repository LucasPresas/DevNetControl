import { useEffect, useState } from 'react'
import { useParams } from 'react-router-dom'
import api from '../lib/api'
import { Server, Loader2, AlertCircle, CheckCircle, Terminal, Activity } from 'lucide-react'

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
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin text-primary-600" />
      </div>
    )
  }

  if (!node) {
    return (
      <div className="text-center py-12">
        <AlertCircle className="w-12 h-12 text-red-400 mx-auto mb-3" />
        <p className="text-gray-500 dark:text-gray-400">Nodo no encontrado</p>
      </div>
    )
  }

  return (
    <div className="space-y-4">
      {/* Header */}
      <div className="flex items-center gap-3">
        <div className="w-12 h-12 bg-primary-100 dark:bg-primary-900/30 rounded-xl flex items-center justify-center">
          <Server className="w-6 h-6 text-primary-600 dark:text-primary-400" />
        </div>
        <div>
          <h2 className="text-xl font-bold text-gray-900 dark:text-white">{node.label}</h2>
          <p className="text-sm text-gray-400 font-mono">{node.ip}:{node.sshPort}</p>
        </div>
      </div>

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm ${
          message.type === 'success' ? 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-400'
        }`}>
          {message.type === 'success' ? <CheckCircle className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* Actions */}
      <div className="grid grid-cols-2 gap-2">
        <button
          onClick={handleTestConnection}
          disabled={actionLoading}
          className="flex items-center justify-center gap-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 py-3 rounded-xl font-medium hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50 text-gray-900 dark:text-white"
        >
          <Activity className="w-4 h-4" />
          Test conexion
        </button>
        <button
          onClick={handleFetchMetrics}
          disabled={actionLoading}
          className="flex items-center justify-center gap-2 bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 py-3 rounded-xl font-medium hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors disabled:opacity-50 text-gray-900 dark:text-white"
        >
          <Activity className="w-4 h-4" />
          Metricas
        </button>
      </div>

      {/* Metrics */}
      {metrics && (
        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-100 dark:border-gray-700 space-y-3 transition-colors">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <Activity className="w-4 h-4" />
            Metricas del servidor
          </h3>
          <div className="grid grid-cols-2 gap-3">
            {Object.entries(metrics).map(([key, value]) => (
              <div key={key} className="bg-gray-50 dark:bg-gray-700 rounded-lg p-3">
                <p className="text-xs text-gray-400 uppercase tracking-wide">{key}</p>
                <p className="font-semibold text-gray-900 dark:text-white text-sm mt-0.5">{value}</p>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Command Execution */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 transition-colors">
        <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <Terminal className="w-4 h-4" />
            Ejecutar comando
          </h3>
        </div>

        <form onSubmit={handleExecuteCommand} className="p-4 space-y-3">
          <div className="flex gap-2">
            <input
              type="text"
              value={command}
              onChange={(e) => setCommand(e.target.value)}
              className="flex-1 px-3 py-2.5 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none font-mono text-sm"
              placeholder="uptime"
              required
            />
            <button
              type="submit"
              disabled={actionLoading}
              className="bg-primary-600 text-white px-4 py-2.5 rounded-lg font-medium hover:bg-primary-700 disabled:bg-primary-400 transition-colors flex items-center gap-2"
            >
              {actionLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : 'Ejecutar'}
            </button>
          </div>

          {output && (
            <div className="bg-gray-900 text-green-400 rounded-lg p-3 font-mono text-sm whitespace-pre-wrap max-h-48 overflow-y-auto">
              {output}
            </div>
          )}
        </form>
      </div>
    </div>
  )
}
