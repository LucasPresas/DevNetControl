import { useEffect, useState } from 'react'
import api from '../lib/api'
import { FileText, Loader2, Search, RefreshCw } from 'lucide-react'

export default function Logs() {
  const [logs, setLogs] = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')

  useEffect(() => {
    fetchLogs()
  }, [])

  async function fetchLogs() {
    try {
      const { data } = await api.get(`/sessionlog?limit=100${search ? `&search=${encodeURIComponent(search)}` : ''}`)
      setLogs(data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="max-w-7xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Logs de Actividad</h1>
          <p className="text-sm text-[var(--text-muted)]">Ultimas sesiones y acciones</p>
        </div>
        <button onClick={fetchLogs} className="btn btn-secondary">
          <RefreshCw className="w-4 h-4" />
          Actualizar
        </button>
      </div>

      <div className="relative mb-4">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
        <input
          type="text"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => e.key === 'Enter' && fetchLogs()}
          className="input pl-10"
          placeholder="Buscar por usuario, IP, accion..."
        />
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-blue-500" /></div>
      ) : logs.length === 0 ? (
        <div className="card flex flex-col items-center justify-center py-16">
          <FileText className="w-12 h-12 text-[var(--text-muted)] mb-3" />
          <p className="text-[var(--text-muted)]">No hay logs registrados</p>
        </div>
      ) : (
        <div className="table-container overflow-x-auto">
          <table className="table">
            <thead>
              <tr>
                <th>Timestamp</th>
                <th>Usuario</th>
                <th>IP Cliente</th>
                <th>Nodo</th>
                <th>Accion</th>
                <th>Detalles</th>
              </tr>
            </thead>
            <tbody>
              {logs.map(log => (
                <tr key={log.id}>
                  <td className="text-[var(--text-muted)] whitespace-nowrap">
                    {new Date(log.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                  </td>
                  <td className="font-medium text-[var(--text-primary)]">{log.userName}</td>
                  <td className="font-mono text-xs text-[var(--text-secondary)]">{log.clientIp}</td>
                  <td className="font-mono text-xs text-[var(--text-secondary)]">{log.nodeIp}</td>
                  <td>
                    <span className="badge badge-info">{log.action}</span>
                  </td>
                  <td className="text-sm text-[var(--text-muted)] max-w-[200px] truncate">{log.details}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
