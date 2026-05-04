import { useEffect, useState } from 'react'
import { useAuthStore } from '../store/authStore'
import api from '../lib/api'
import {
  Users, Server, Wallet, Activity, ArrowUpRight, ArrowDownRight,
  Loader2, Clock, UserCheck, AlertTriangle
} from 'lucide-react'

export default function Dashboard() {
  const { user } = useAuthStore()
  const [stats, setStats] = useState(null)
  const [nodes, setNodes] = useState([])
  const [subusers, setSubusers] = useState([])
  const [recentTransactions, setRecentTransactions] = useState([])
  const [recentLogs, setRecentLogs] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function fetchData() {
      try {
        const [balanceRes, historyRes, nodesRes, subusersRes, logsRes] = await Promise.all([
          api.get('/credit/balance'),
          api.get('/credit/history'),
          api.get('/vpsnode'),
          api.get('/user/my-subusers'),
          api.get('/activity/recent'),
        ])

        const activeUsers = subusersRes.data.filter(u => u.serviceExpiry && new Date(u.serviceExpiry) > new Date()).length
        const expiredUsers = subusersRes.data.filter(u => u.serviceExpiry && new Date(u.serviceExpiry) <= new Date()).length
        const trialUsers = subusersRes.data.filter(u => u.isTrial).length

        setStats({
          balance: balanceRes.data.balance,
          totalUsers: subusersRes.data.length,
          activeUsers,
          expiredUsers,
          trialUsers,
          nodesCount: nodesRes.data.length,
        })
        setSubusers(subusersRes.data)
        setNodes(nodesRes.data)
        setRecentTransactions(historyRes.data.slice(0, 8))
        setRecentLogs(logsRes.data.slice(0, 10))
      } catch (err) {
        console.error('Error:', err)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center py-20">
        <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
      </div>
    )
  }

  const roleLabels = { 0: 'SuperAdmin', 1: 'Admin', 2: 'Reseller', 3: 'SubReseller', 4: 'Customer' }

  return (
    <div className="space-y-6 max-w-7xl mx-auto">
      {/* Header */}
      <div>
        <h1 className="text-xl font-bold text-[var(--text-primary)]">Panel de Control</h1>
        <p className="text-sm text-[var(--text-muted)] mt-0.5">
          {user?.role === 'Admin' ? 'Gestion de tu tenant' : user?.role === 'SuperAdmin' ? 'Gestion global de la plataforma' : `Rol: ${roleLabels[user?.role] || user?.role}`}
        </p>
      </div>

      {/* Balance Warning */}
      {stats?.balance <= 0 && (
        <div className="flex items-center gap-3 p-4 bg-red-500/10 border border-red-500/30 rounded-lg">
          <AlertTriangle className="w-5 h-5 text-red-500 shrink-0" />
          <div>
            <p className="text-sm font-semibold text-red-400">Saldo Agotado</p>
            <p className="text-xs text-red-300/70">No tenes creditos para crear usuarios o servicios. Contacta a tu administrador.</p>
          </div>
        </div>
      )}

      {/* Stat Cards */}
      <div className="grid grid-cols-2 lg:grid-cols-4 gap-3">
        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Balance</span>
            <div className="p-2 rounded-lg bg-yellow-500/10">
              <Wallet className="w-4 h-4 text-yellow-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.balance?.toLocaleString() ?? 0}</p>
          <p className="text-xs text-[var(--text-muted)] mt-1">creditos disponibles</p>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Usuarios</span>
            <div className="p-2 rounded-lg bg-blue-500/10">
              <Users className="w-4 h-4 text-blue-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.totalUsers ?? 0}</p>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-xs text-green-400 flex items-center gap-1">
              <UserCheck className="w-3 h-3" /> {stats?.activeUsers ?? 0} activos
            </span>
            <span className="text-xs text-red-400">{stats?.expiredUsers ?? 0} vencidos</span>
          </div>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Nodos</span>
            <div className="p-2 rounded-lg bg-purple-500/10">
              <Server className="w-4 h-4 text-purple-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.nodesCount ?? 0}</p>
          <p className="text-xs text-[var(--text-muted)] mt-1">servidores conectados</p>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Pruebas</span>
            <div className="p-2 rounded-lg bg-amber-500/10">
              <Clock className="w-4 h-4 text-amber-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.trialUsers ?? 0}</p>
          <p className="text-xs text-[var(--text-muted)] mt-1">usuarios de prueba</p>
        </div>
      </div>

      {/* Two column layout */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
        {/* Actividades Recientes */}
        <div className="card">
          <div className="px-4 py-3 border-b border-[var(--border-color)] flex items-center justify-between">
            <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
              <Activity className="w-4 h-4 text-blue-500" />
              Actividades Recientes
            </h3>
            <button 
              onClick={() => window.location.href = '/logs'}
              className="text-xs text-blue-400 hover:text-blue-300 flex items-center gap-1"
            >
              Ver todas →
            </button>
          </div>
          <div className="divide-y divide-[var(--border-color)]">
            {recentLogs.length === 0 ? (
              <div className="px-4 py-8 text-center text-[var(--text-muted)] text-sm">
                Sin actividad registrada
              </div>
            ) : (
              recentLogs.map((activity) => (
                <div key={activity.id} className="px-4 py-2.5 flex items-start gap-3">
                  <div className={`w-2 h-2 mt-1.5 rounded-full shrink-0 ${
                    activity.actionType?.includes('Login') ? 'bg-blue-500' :
                    activity.actionType?.includes('Created') ? 'bg-green-500' :
                    activity.actionType?.includes('Credits') ? 'bg-yellow-500' :
                    activity.actionType?.includes('Deleted') ? 'bg-red-500' :
                    'bg-gray-500'
                  }`} />
                  <div className="flex-1 min-w-0">
                    <p className="text-sm text-[var(--text-primary)]">
                      <span className="font-semibold">{activity.actorUserName}</span>
                      {activity.actionType === 'Login' && ' inició sesión'}
                      {activity.actionType === 'Logout' && ' cerró sesión'}
                      {activity.actionType === 'UserCreated' && ' creó el usuario '}
                      {activity.actionType === 'UserUpdated' && ' editó al usuario '}
                      {activity.actionType === 'UserSuspended' && (activity.description?.includes('activado') ? ' activó a ' : ' suspendió a ')}
                      {activity.actionType === 'CreditsLoaded' && ' cargó créditos a '}
                      {activity.actionType === 'CreditsConsumed' && ' consumió créditos de '}
                      {activity.actionType === 'CreditsTransferred' && ' transfirió créditos a '}
                      {activity.actionType === 'PlanAssigned' && ' asignó plan a '}
                      {activity.actionType === 'PlanChanged' && ' cambió plan a '}
                      {activity.actionType === 'ServiceExtended' && ' extendió servicio a '}
                      {activity.actionType === 'ResellerCreated' && ' creó el reseller '}
                      {activity.actionType === 'SubResellerCreated' && ' creó el sub-reseller '}
                      {activity.actionType === 'BulkOperation' && ' realizó operación masiva'}
                      {activity.targetUserName && (
                        <span className="font-semibold text-blue-400">{activity.targetUserName}</span>
                      )}
                      {activity.planName && (
                        <span> con el plan <span className="font-semibold text-purple-400">{activity.planName}</span></span>
                      )}
                      {activity.creditsConsumed > 0 && (
                        <span>, gastó <span className="text-red-400 font-semibold">{activity.creditsConsumed}</span> crédito(s)</span>
                      )}
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
              ))
            )}
          </div>
        </div>

        {/* Recent Transactions (legacy) */}
        <div className="card">
          <div className="px-4 py-3 border-b border-[var(--border-color)]">
            <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
              <Activity className="w-4 h-4 text-green-500" />
              Transacciones Recientes
            </h3>
          </div>
          <div className="divide-y divide-[var(--border-color)]">
            {recentTransactions.length === 0 ? (
              <div className="px-4 py-8 text-center text-[var(--text-muted)] text-sm">
                Sin transacciones
              </div>
            ) : (
              recentTransactions.map((tx) => (
                <div key={tx.id} className="px-4 py-2.5 flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <div className={`w-2 h-2 rounded-full ${tx.direction === 'Received' ? 'bg-green-500' : 'bg-red-500'}`} />
                    <div>
                      <p className="text-sm text-[var(--text-primary)]">
                        {tx.direction === 'Received' ? tx.fromUserName : tx.toUserName}
                      </p>
                      <p className="text-xs text-[var(--text-muted)]">
                        {new Date(tx.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                      </p>
                    </div>
                  </div>
                  <span className={`text-sm font-semibold flex items-center gap-1 ${
                    tx.direction === 'Received' ? 'text-green-400' : 'text-red-400'
                  }`}>
                    {tx.direction === 'Received' ? <ArrowDownRight className="w-3.5 h-3.5" /> : <ArrowUpRight className="w-3.5 h-3.5" />}
                    {tx.amount.toLocaleString()}
                  </span>
                </div>
              ))
            )}
          </div>
        </div>
      </div>

      {/* My Users Overview */}
      {subusers && subusers.length > 0 && (
        <div className="card">
          <div className="px-4 py-3 border-b border-[var(--border-color)]">
            <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
              <Users className="w-4 h-4 text-blue-500" />
              Mis Usuarios
            </h3>
          </div>
          <div className="divide-y divide-[var(--border-color)]">
            {subusers.slice(0, 8).map((u) => (
              <div key={u.id} className="px-4 py-2.5 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <span className={`w-2 h-2 rounded-full ${
                    u.isTrial ? 'bg-yellow-500' :
                    (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'bg-green-500' : 'bg-red-500'
                  }`} />
                  <div>
                    <p className="text-sm text-[var(--text-primary)]">{u.userName}</p>
                    <div className="flex items-center gap-2 text-xs text-[var(--text-muted)]">
                      <span>{u.resellerName || 'N/A'}</span>
                      <span>|</span>
                      <span>{(u.plan?.maxConnections || 0) + (u.additionalConnections || 0)} conexiones</span>
                    </div>
                  </div>
                </div>
                <span className={`badge text-xs ${
                  u.isTrial ? 'badge-warning' :
                  (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'badge-success' : 'badge-danger'
                }`}>
                  {u.isTrial ? 'Prueba' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'Activo' : 'Vencido'}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Nodes Overview */}
      {nodes.length > 0 && (
        <div className="card">
          <div className="px-4 py-3 border-b border-[var(--border-color)]">
            <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
              <Server className="w-4 h-4 text-purple-500" />
              Nodos VPS
            </h3>
          </div>
          <div className="divide-y divide-[var(--border-color)]">
            {nodes.map((node) => (
              <div key={node.id} className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <span className="status-dot online" />
                  <div>
                    <p className="text-sm font-medium text-[var(--text-primary)]">{node.label}</p>
                    <p className="text-xs text-[var(--text-muted)] font-mono">{node.ip}:{node.sshPort}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
