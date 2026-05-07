import { useEffect, useState } from 'react'
import { useAuthStore } from '../store/authStore'
import api from '../lib/api'
import {
  Users, Server, Wallet, Activity, ArrowUpRight, ArrowDownRight,
  Loader2, Clock, UserCheck, AlertTriangle, Wifi, WifiOff, Clock3,
  UserX, Cpu, HardDrive, MemoryStick, Network
} from 'lucide-react'

export default function Dashboard() {
  const { user } = useAuthStore()
  const isAdmin = user?.role === 'Admin' || user?.role === 'SuperAdmin'
  const [stats, setStats] = useState(null)
  const [nodes, setNodes] = useState([])
  const [subusers, setSubusers] = useState([])
  const [recentTransactions, setRecentTransactions] = useState([])
  const [recentLogs, setRecentLogs] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function fetchData() {
      try {
        const endpoints = [
          api.get('/user/dashboard-stats'),
          api.get('/activity/recent'),
        ]
        if (isAdmin) {
          endpoints.push(api.get('/vpsnode/health'))
          endpoints.push(api.get('/credit/history'))
        }
        endpoints.push(api.get('/user/my-subusers'))

        const results = await Promise.all(endpoints)
        const statsRes = results[0]
        const logsRes = results[1]
        let nodesRes, historyRes, subusersRes

        if (isAdmin) {
          nodesRes = results[2]
          historyRes = results[3]
          subusersRes = results[4]
        } else {
          subusersRes = results[2]
        }

        setStats(statsRes.data)
        setSubusers(subusersRes.data)
        setRecentLogs(logsRes.data.slice(0, 10))

        if (isAdmin && nodesRes) setNodes(nodesRes.data)
        if (isAdmin && historyRes) setRecentTransactions(historyRes.data.slice(0, 8))
      } catch (err) {
        console.error('Error:', err)
      } finally {
        setLoading(false)
      }
    }
    fetchData()
  }, [isAdmin])

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
            <span className="text-xs text-red-400 flex items-center gap-1">
              <UserX className="w-3 h-3" /> {stats?.expiredUsers ?? 0} vencidos
            </span>
          </div>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Conexiones</span>
            <div className="p-2 rounded-lg bg-green-500/10">
              <Network className="w-4 h-4 text-green-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.onlineUsers ?? 0}</p>
          <div className="flex items-center gap-3 mt-1">
            <span className="text-xs text-green-400 flex items-center gap-1">
              <Wifi className="w-3 h-3" /> {stats?.onlineUsers ?? 0} online
            </span>
            <span className="text-xs text-[var(--text-muted)]">/ {stats?.totalConnections ?? 0} max</span>
          </div>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Vencen Pronto</span>
            <div className="p-2 rounded-lg bg-amber-500/10">
              <Clock3 className="w-4 h-4 text-amber-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.expiringSoonUsers ?? 0}</p>
          <p className="text-xs text-[var(--text-muted)] mt-1">en los proximos 3 dias</p>
        </div>

        <div className="card p-4">
          <div className="flex items-center justify-between mb-3">
            <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Resellers</span>
            <div className="p-2 rounded-lg bg-purple-500/10">
              <Server className="w-4 h-4 text-purple-500" />
            </div>
          </div>
          <p className="text-2xl font-bold text-[var(--text-primary)]">{stats?.resellerCount ?? 0}</p>
          <p className="text-xs text-[var(--text-muted)] mt-1">resellers registrados</p>
        </div>
      </div>

      {/* Expiring Soon Alert */}
      {stats?.expiringSoonUsers > 0 && (
        <div className="flex items-center gap-3 p-4 bg-amber-500/10 border border-amber-500/30 rounded-lg">
          <Clock3 className="w-5 h-5 text-amber-500 shrink-0" />
          <div>
            <p className="text-sm font-semibold text-amber-400">Usuarios por vencer</p>
            <p className="text-xs text-amber-300/70">{stats.expiringSoonUsers} usuario(s) vencen en los proximos 3 dias</p>
          </div>
        </div>
      )}

      {/* Nodes with load (Admin only) */}
      {isAdmin && nodes.length > 0 && (
        <div className="card">
          <div className="px-4 py-3 border-b border-[var(--border-color)]">
            <h3 className="text-sm font-semibold text-[var(--text-primary)] flex items-center gap-2">
              <Server className="w-4 h-4 text-purple-500" />
              Estado de Nodos
            </h3>
          </div>
          <div className="divide-y divide-[var(--border-color)]">
            {nodes.map((node) => (
              <div key={node.id} className="px-4 py-3">
                <div className="flex items-center justify-between mb-2">
                  <div className="flex items-center gap-3">
                    <span className={`status-dot ${node.isOnline ? 'online' : 'offline'}`} />
                    <div>
                      <p className="text-sm font-medium text-[var(--text-primary)]">{node.label}</p>
                      <p className="text-xs text-[var(--text-muted)] font-mono">{node.ip}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-4 text-xs">
                    <span className="text-[var(--text-secondary)] flex items-center gap-1">
                      <Users className="w-3 h-3" /> {node.userCount} usuarios
                    </span>
                    {node.lastHealthCheck && (
                      <span className="text-[var(--text-muted)]">
                        Ultimo check: {new Date(node.lastHealthCheck).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                      </span>
                    )}
                  </div>
                </div>
                {node.isOnline && node.metrics && (
                  <div className="grid grid-cols-3 gap-3 mt-2 ml-8">
                    <div>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs text-[var(--text-muted)] flex items-center gap-1">
                          <Cpu className="w-3 h-3" /> CPU
                        </span>
                        <span className={`text-xs font-semibold ${node.metrics.cpuPercent > 80 ? 'text-red-400' : node.metrics.cpuPercent > 50 ? 'text-amber-400' : 'text-green-400'}`}>{node.metrics.cpuPercent}%</span>
                      </div>
                      <div className="w-full bg-[var(--bg-primary)] rounded-full h-1.5">
                        <div className={`h-1.5 rounded-full ${node.metrics.cpuPercent > 80 ? 'bg-red-500' : node.metrics.cpuPercent > 50 ? 'bg-amber-500' : 'bg-green-500'}`} style={{ width: `${Math.min(node.metrics.cpuPercent, 100)}%` }} />
                      </div>
                    </div>
                    <div>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs text-[var(--text-muted)] flex items-center gap-1">
                          <MemoryStick className="w-3 h-3" /> RAM
                        </span>
                        <span className={`text-xs font-semibold ${node.metrics.ramPercent > 80 ? 'text-red-400' : node.metrics.ramPercent > 50 ? 'text-amber-400' : 'text-green-400'}`}>{node.metrics.ramPercent}%</span>
                      </div>
                      <div className="w-full bg-[var(--bg-primary)] rounded-full h-1.5">
                        <div className={`h-1.5 rounded-full ${node.metrics.ramPercent > 80 ? 'bg-red-500' : node.metrics.ramPercent > 50 ? 'bg-amber-500' : 'bg-green-500'}`} style={{ width: `${Math.min(node.metrics.ramPercent, 100)}%` }} />
                      </div>
                    </div>
                    <div>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs text-[var(--text-muted)] flex items-center gap-1">
                          <HardDrive className="w-3 h-3" /> Disco
                        </span>
                        <span className={`text-xs font-semibold ${node.metrics.diskPercent > 80 ? 'text-red-400' : node.metrics.diskPercent > 50 ? 'text-amber-400' : 'text-green-400'}`}>{node.metrics.diskPercent}%</span>
                      </div>
                      <div className="w-full bg-[var(--bg-primary)] rounded-full h-1.5">
                        <div className={`h-1.5 rounded-full ${node.metrics.diskPercent > 80 ? 'bg-red-500' : node.metrics.diskPercent > 50 ? 'bg-amber-500' : 'bg-green-500'}`} style={{ width: `${Math.min(node.metrics.diskPercent, 100)}%` }} />
                      </div>
                    </div>
                  </div>
                )}
                {!node.isOnline && (
                  <div className="ml-8 mt-1">
                    <span className="text-xs text-red-400 flex items-center gap-1">
                      <WifiOff className="w-3 h-3" /> Nodo desconectado
                    </span>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

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
                      {activity.actionType === 'Login' && ' inicio sesion'}
                      {activity.actionType === 'Logout' && ' cerro sesion'}
                      {activity.actionType === 'UserCreated' && ' creo el usuario '}
                      {activity.actionType === 'UserUpdated' && ' edito al usuario '}
                      {activity.actionType === 'UserSuspended' && (activity.description?.includes('activado') || activity.description?.includes('reactivado') ? ' activo a ' : ' suspendio a ')}
                      {activity.actionType === 'UserDeleted' && (activity.description?.includes('Sub-reseller') ? ' elimino el sub-reseller ' : ' elimino al usuario ')}
                      {activity.actionType === 'CreditsLoaded' && ' cargo creditos a '}
                      {activity.actionType === 'CreditsConsumed' && ' consumio creditos de '}
                      {activity.actionType === 'CreditsTransferred' && ' transfirio creditos a '}
                      {activity.actionType === 'PlanAssigned' && ' asigno plan a '}
                      {activity.actionType === 'PlanChanged' && ' cambio plan a '}
                      {activity.actionType === 'ServiceExtended' && ' extendio servicio a '}
                      {activity.actionType === 'ResellerCreated' && ' creo el reseller '}
                      {activity.actionType === 'SubResellerCreated' && ' creo el sub-reseller '}
                      {activity.actionType === 'BulkOperation' && ' realizo operacion masiva'}
                      {activity.targetUserName && (
                        <span className="font-semibold text-blue-400">{activity.targetUserName}</span>
                      )}
                      {activity.planName && (
                        <span> con el plan <span className="font-semibold text-purple-400">{activity.planName}</span></span>
                      )}
                    </p>
                    <p className="text-xs text-[var(--text-muted)] mt-0.5">
                      {new Date(activity.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                      {activity.actorRole && <span> · {activity.actorRole}</span>}
                    </p>
                    {activity.actionType === 'UserDeleted' && activity.details && (
                      <p className="text-xs text-amber-400/80 mt-0.5">
                        {(() => {
                          try {
                            const d = JSON.parse(activity.details)
                            return `Reembolso: ${d.refundedCredits} creditos · Hijos eliminados: ${d.deletedChildCount} · Saldo: ${d.actorBalanceBefore} → ${d.actorBalanceAfter}`
                          } catch { return '' }
                        })()}
                      </p>
                    )}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Right column */}
        {isAdmin ? (
          /* Recent Transactions (Admin) */
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
                    <span className={`text-sm font-semibold flex items-center gap-1 ${tx.direction === 'Received' ? 'text-green-400' : 'text-red-400'}`}>
                      {tx.direction === 'Received' ? <ArrowDownRight className="w-3.5 h-3.5" /> : <ArrowUpRight className="w-3.5 h-3.5" />}
                      {tx.amount.toLocaleString()}
                    </span>
                  </div>
                ))
              )}
            </div>
          </div>
        ) : (
          /* My Users Overview (Reseller) */
          subusers && subusers.length > 0 && (
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
                      <span className={`w-2 h-2 rounded-full ${u.isTrial ? 'bg-yellow-500' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'bg-green-500' : 'bg-red-500'}`} />
                      <div>
                        <p className="text-sm text-[var(--text-primary)]">{u.userName}</p>
                        <div className="flex items-center gap-2 text-xs text-[var(--text-muted)]">
                          <span>{u.resellerName || 'N/A'}</span>
                          <span>|</span>
                          <span>{(u.plan?.maxConnections || 0) + (u.additionalConnections || 0)} conexiones</span>
                        </div>
                      </div>
                    </div>
                    <span className={`badge text-xs ${u.isTrial ? 'badge-warning' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'badge-success' : 'badge-danger'}`}>
                      {u.isTrial ? 'Prueba' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'Activo' : 'Vencido'}
                    </span>
                  </div>
                ))}
              </div>
            </div>
          )
        )}
      </div>

      {/* My Users Overview (Admin - full width) */}
      {isAdmin && subusers && subusers.length > 0 && (
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
                  <span className={`w-2 h-2 rounded-full ${u.isTrial ? 'bg-yellow-500' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'bg-green-500' : 'bg-red-500'}`} />
                  <div>
                    <p className="text-sm text-[var(--text-primary)]">{u.userName}</p>
                    <div className="flex items-center gap-2 text-xs text-[var(--text-muted)]">
                      <span>{u.resellerName || 'N/A'}</span>
                      <span>|</span>
                      <span>{(u.plan?.maxConnections || 0) + (u.additionalConnections || 0)} conexiones</span>
                    </div>
                  </div>
                </div>
                <span className={`badge text-xs ${u.isTrial ? 'badge-warning' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'badge-success' : 'badge-danger'}`}>
                  {u.isTrial ? 'Prueba' : (u.serviceExpiry && new Date(u.serviceExpiry) > new Date()) ? 'Activo' : 'Vencido'}
                </span>
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  )
}
