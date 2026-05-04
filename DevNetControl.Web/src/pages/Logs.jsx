import { useEffect, useState } from 'react'
import api, { getActivities, getRecentActivities } from '../lib/api'
import { FileText, Loader2, Search, RefreshCw, Activity, List, Filter, User } from 'lucide-react'

const actionTypeLabels = {
  'Login': 'Login Exitoso',
  'Logout': 'Logout',
  'UserCreated': 'Usuario Creado',
  'UserUpdated': 'Usuario Editado',
  'UserSuspended': 'Usuario Suspendido',
  'CreditsLoaded': 'Créditos Cargados',
  'CreditsConsumed': 'Créditos Consumidos',
  'CreditsTransferred': 'Créditos Transferidos',
  'PlanAssigned': 'Plan Asignado',
  'PlanChanged': 'Plan Cambiado',
  'ServiceExtended': 'Servicio Extendido',
  'ResellerCreated': 'Reseller Creado',
  'SubResellerCreated': 'SubReseller Creado',
  'NodeAccessGranted': 'Acceso a Nodo Otorgado',
  'NodeAccessRevoked': 'Acceso a Nodo Revocado',
  'PlanAccessGranted': 'Acceso a Plan Otorgado',
  'PlanAccessRevoked': 'Acceso a Plan Revocado',
  'BulkOperation': 'Operación Masiva',
}

export default function Logs() {
  const [activeTab, setActiveTab] = useState('activity')
  const [activities, setActivities] = useState([])
  const [sessionLogs, setSessionLogs] = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch] = useState('')
  const [page, setPage] = useState(1)
  const [totalCount, setTotalCount] = useState(0)
  const [selectedActionType, setSelectedActionType] = useState('')
  const [selectedUserName, setSelectedUserName] = useState('')
  const pageSize = 20

  useEffect(() => {
    fetchData()
  }, [activeTab, page, selectedActionType, selectedUserName])

  async function fetchData() {
    setLoading(true)
    try {
      if (activeTab === 'activity') {
        const params = {
          page,
          pageSize,
          search: search || undefined,
          actionType: selectedActionType || undefined,
          actorUserName: selectedUserName || undefined,
        }
        const { data } = await getActivities(params)
        setActivities(data.data || [])
        setTotalCount(data.total || 0)
      } else {
        const { data } = await api.get(`/sessionlog?limit=100${search ? `&search=${encodeURIComponent(search)}` : ''}`)
        setSessionLogs(data)
      }
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  function handleSearch(e) {
    if (e.key === 'Enter') {
      setPage(1)
      fetchData()
    }
  }

  const totalPages = Math.ceil(totalCount / pageSize)

  function getActivityText(activity) {
    const actor = activity.actorUserName || 'Sistema'
    let text = actor
    
    if (activity.actionType === 'Login') text += ' inició sesión'
    else if (activity.actionType === 'Logout') text += ' cerró sesión'
    else if (activity.actionType === 'UserCreated') text += ' creó el usuario '
    else if (activity.actionType === 'UserUpdated') text += ' editó al usuario '
    else if (activity.actionType === 'UserSuspended') {
      const isActivate = activity.description?.includes('activado')
      text += isActivate ? ' activó a ' : ' suspendió a '
    }
    else if (activity.actionType === 'CreditsLoaded') text += ' cargó créditos a '
    else if (activity.actionType === 'CreditsConsumed') text += ' consumió créditos de '
    else if (activity.actionType === 'CreditsTransferred') text += ' transfirió créditos a '
    else if (activity.actionType === 'PlanAssigned') text += ' asignó plan a '
    else if (activity.actionType === 'PlanChanged') text += ' cambió plan a '
    else if (activity.actionType === 'ServiceExtended') text += ' extendió servicio a '
    else if (activity.actionType === 'ResellerCreated') text += ' creó el reseller '
    else if (activity.actionType === 'SubResellerCreated') text += ' creó el sub-reseller '
    else if (activity.actionType === 'BulkOperation') text += ' realizó operación masiva: '
    else text += ' realizó acción'

    if (activity.targetUserName) text += activity.targetUserName
    if (activity.planName) text += ' con el plan ' + activity.planName
    if (activity.creditsConsumed > 0) text += ', gastó ' + activity.creditsConsumed + ' crédito(s)'
    
    return text
  }

  return (
    <div className="max-w-7xl mx-auto">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-4">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Logs de Actividad</h1>
          <p className="text-sm text-[var(--text-muted)]">Historial de acciones y sesiones</p>
        </div>
        <button onClick={fetchData} className="btn btn-secondary">
          <RefreshCw className="w-4 h-4" />
          Actualizar
        </button>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 mb-4 border-b border-[var(--border-color)]">
        <button
          onClick={() => { setActiveTab('activity'); setPage(1) }}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'activity'
              ? 'border-blue-500 text-blue-400'
              : 'border-transparent text-[var(--text-muted)] hover:text-[var(--text-secondary)]'
          }`}
        >
          <Activity className="w-4 h-4" />
          Actividad del Sistema
        </button>
        <button
          onClick={() => { setActiveTab('sessions'); setPage(1) }}
          className={`flex items-center gap-2 px-4 py-2 text-sm font-medium border-b-2 transition-colors ${
            activeTab === 'sessions'
              ? 'border-blue-500 text-blue-400'
              : 'border-transparent text-[var(--text-muted)] hover:text-[var(--text-secondary)]'
          }`}
        >
          <List className="w-4 h-4" />
          Sesiones
        </button>
      </div>

      {/* Filters */}
      <div className="flex flex-col sm:flex-row gap-3 mb-4">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            onKeyDown={handleSearch}
            className="input pl-10"
            placeholder={activeTab === 'activity' ? 'Buscar por descripción, acción...' : 'Buscar por usuario, IP...'}
          />
        </div>
        {activeTab === 'activity' && (
          <>
            <div className="relative">
              <User className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--text-muted)]" />
              <input
                type="text"
                value={selectedUserName}
                onChange={(e) => { setSelectedUserName(e.target.value); setPage(1) }}
                onKeyDown={handleSearch}
                className="input pl-10 w-auto"
                placeholder="Filtrar por usuario..."
              />
            </div>
            <select
              value={selectedActionType}
              onChange={(e) => { setSelectedActionType(e.target.value); setPage(1) }}
              className="input w-auto"
            >
              <option value="">Todas las acciones</option>
              {Object.entries(actionTypeLabels).map(([key, label]) => (
                <option key={key} value={key}>{label}</option>
              ))}
            </select>
          </>
        )}
      </div>

      {loading ? (
        <div className="flex items-center justify-center py-16"><Loader2 className="w-8 h-8 animate-spin text-blue-500" /></div>
      ) : (
        <>
          {/* Activity Tab */}
          {activeTab === 'activity' && (
            activities.length === 0 ? (
              <div className="card flex flex-col items-center justify-center py-16">
                <Activity className="w-12 h-12 text-[var(--text-muted)] mb-3" />
                <p className="text-[var(--text-muted)]">No hay actividad registrada</p>
              </div>
            ) : (
              <div className="space-y-2">
                {activities.map(activity => (
                  <div key={activity.id} className="card p-3 flex items-start gap-3">
                    <div className={`w-2 h-2 mt-1.5 rounded-full shrink-0 ${
                      activity.actionType?.includes('Login') ? 'bg-blue-500' :
                      activity.actionType?.includes('Created') ? 'bg-green-500' :
                      activity.actionType?.includes('Credits') ? 'bg-yellow-500' :
                      activity.actionType?.includes('Deleted') ? 'bg-red-500' :
                      'bg-gray-500'
                    }`} />
                    <div className="flex-1 min-w-0">
                      <p className="text-sm text-[var(--text-primary)]">
                        {getActivityText(activity)}
                      </p>
                      <p className="text-xs text-[var(--text-muted)] mt-0.5">
                        {new Date(activity.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                        {activity.actorRole && <span> · {activity.actorRole}</span>}
                        {activity.creditsBalanceAfter !== undefined && (
                          <span> · Saldo: {activity.creditsBalanceAfter} créditos</span>
                        )}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            )
          )}

          {/* Sessions Tab */}
          {activeTab === 'sessions' && (
            sessionLogs.length === 0 ? (
              <div className="card flex flex-col items-center justify-center py-16">
                <FileText className="w-12 h-12 text-[var(--text-muted)] mb-3" />
                <p className="text-[var(--text-muted)]">No hay logs de sesión</p>
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
                      <th>Acción</th>
                      <th>Detalles</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sessionLogs.map(log => (
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
            )
          )}

          {/* Pagination */}
          {activeTab === 'activity' && totalPages > 1 && (
            <div className="flex items-center justify-between mt-4">
              <p className="text-sm text-[var(--text-muted)]">
                Página {page} de {totalPages} ({totalCount} registros)
              </p>
              <div className="flex gap-2">
                <button
                  onClick={() => setPage(p => Math.max(1, p - 1))}
                  disabled={page <= 1}
                  className="btn btn-sm btn-secondary"
                >
                  Anterior
                </button>
                <button
                  onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                  className="btn btn-sm btn-secondary"
                >
                  Siguiente
                </button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  )
}
