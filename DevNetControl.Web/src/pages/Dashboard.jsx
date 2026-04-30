import { useEffect, useState } from 'react'
import { useAuthStore } from '../store/authStore'
import api from '../lib/api'
import { Wallet, Users, Server, ArrowUpRight, ArrowDownRight, TrendingUp, Loader2 } from 'lucide-react'

export default function Dashboard() {
  const { user } = useAuthStore()
  const [stats, setStats] = useState(null)
  const [recentTransactions, setRecentTransactions] = useState([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    async function fetchData() {
      try {
        const [balanceRes, historyRes, nodesRes, subusersRes] = await Promise.all([
          api.get('/credit/balance'),
          api.get('/credit/history'),
          api.get('/vpsnode'),
          api.get('/user/my-subusers'),
        ])

        setStats({
          balance: balanceRes.data.balance,
          nodesCount: nodesRes.data.length,
          subusersCount: subusersRes.data.length,
        })
        setRecentTransactions(historyRes.data.slice(0, 5))
      } catch (err) {
        console.error('Error fetching dashboard data:', err)
      } finally {
        setLoading(false)
      }
    }

    fetchData()
  }, [])

  if (loading) {
    return (
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin text-primary-600" />
      </div>
    )
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-xl font-bold text-gray-900 dark:text-white">Hola, {user?.userName}</h2>
        <p className="text-gray-500 dark:text-gray-400 text-sm mt-1">
          {user?.role === 'Admin' ? 'Panel de administracion' : user?.role === 'SuperAdmin' ? 'Panel Super Admin' : `Rol: ${user?.role}`}
        </p>
      </div>

      {/* Stats Cards */}
      <div className="grid grid-cols-2 gap-3">
        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-100 dark:border-gray-700 transition-colors">
          <div className="flex items-center gap-2 text-primary-600 dark:text-primary-400 mb-2">
            <Wallet className="w-5 h-5" />
            <span className="text-sm font-medium">Creditos</span>
          </div>
          <p className="text-2xl font-bold text-gray-900 dark:text-white">{stats?.balance?.toLocaleString() ?? 0}</p>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-100 dark:border-gray-700 transition-colors">
          <div className="flex items-center gap-2 text-green-600 dark:text-green-400 mb-2">
            <Server className="w-5 h-5" />
            <span className="text-sm font-medium">Nodos</span>
          </div>
          <p className="text-2xl font-bold text-gray-900 dark:text-white">{stats?.nodesCount ?? 0}</p>
        </div>

        <div className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-100 dark:border-gray-700 transition-colors col-span-2 sm:col-span-1">
          <div className="flex items-center gap-2 text-purple-600 dark:text-purple-400 mb-2">
            <Users className="w-5 h-5" />
            <span className="text-sm font-medium">Sub-usuarios</span>
          </div>
          <p className="text-2xl font-bold text-gray-900 dark:text-white">{stats?.subusersCount ?? 0}</p>
        </div>
      </div>

      {/* Recent Transactions */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 transition-colors">
        <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <TrendingUp className="w-4 h-4" />
            Ultimas transacciones
          </h3>
        </div>

        {recentTransactions.length === 0 ? (
          <div className="px-4 py-8 text-center text-gray-400 text-sm">
            No hay transacciones aun
          </div>
        ) : (
          <div className="divide-y divide-gray-100 dark:divide-gray-700">
            {recentTransactions.map((tx) => (
              <div key={tx.id} className="px-4 py-3 flex items-center justify-between">
                <div className="flex items-center gap-3">
                  <div className={`w-8 h-8 rounded-full flex items-center justify-center ${
                    tx.direction === 'Received' ? 'bg-green-100 dark:bg-green-900/30' : 'bg-red-100 dark:bg-red-900/30'
                  }`}>
                    {tx.direction === 'Received' ? (
                      <ArrowDownRight className="w-4 h-4 text-green-600 dark:text-green-400" />
                    ) : (
                      <ArrowUpRight className="w-4 h-4 text-red-600 dark:text-red-400" />
                    )}
                  </div>
                  <div>
                    <p className="text-sm font-medium text-gray-900 dark:text-white">
                      {tx.direction === 'Received' ? 'Recibido de' : 'Enviado a'} {tx.direction === 'Received' ? tx.fromUserName : tx.toUserName}
                    </p>
                    <p className="text-xs text-gray-400">
                      {new Date(tx.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                    </p>
                  </div>
                </div>
                <span className={`font-semibold text-sm ${
                  tx.direction === 'Received' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400'
                }`}>
                  {tx.direction === 'Received' ? '+' : '-'}{tx.amount.toLocaleString()}
                </span>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  )
}
