import { useEffect, useState } from 'react'
import api, { getCreditHistorySummary, getCreditsSummary } from '../lib/api'
import { Wallet, ArrowUpRight, ArrowDownRight, Send, Loader2, Check, X, RefreshCw, TrendingDown, TrendingUp, DollarSign } from 'lucide-react'

const TYPE_LABELS = {
  0: { label: 'Transferencia', color: 'text-[var(--text-secondary)]' },
  1: { label: 'Carga manual', color: 'text-green-400' },
  2: { label: 'Creacion usuario', color: 'text-blue-400' },
  3: { label: 'Extension servicio', color: 'text-purple-400' },
  4: { label: 'Usuario prueba', color: 'text-amber-400' },
  5: { label: 'Costo nodo', color: 'text-indigo-400' },
}

export default function Credits() {
  const [balance, setBalance] = useState(0)
  const [history, setHistory] = useState([])
  const [summary, setSummary] = useState(null)
  const [loading, setLoading] = useState(true)
  const [showTransfer, setShowTransfer] = useState(false)
  const [form, setForm] = useState({ toUserId: '', amount: '' })
  const [transferring, setTransferring] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchData()
  }, [])

  async function fetchData() {
    try {
      const [balanceRes, historyRes, summaryRes] = await Promise.all([
        api.get('/credit/balance'),
        api.get('/credit/history'),
        getCreditHistorySummary().catch(() => null),
      ])
      setBalance(balanceRes.data.balance)
      setHistory(historyRes.data)
      if (summaryRes?.data) setSummary(summaryRes.data)
    } catch (err) {
      console.error('Error:', err)
    } finally {
      setLoading(false)
    }
  }

  async function handleTransfer(e) {
    e.preventDefault()
    setTransferring(true)
    setMessage(null)

    try {
      const { data } = await api.post('/credit/transfer', {
        toUserId: form.toUserId,
        amount: parseFloat(form.amount),
      })
      setMessage({ type: 'success', text: data.message })
      setForm({ toUserId: '', amount: '' })
      setShowTransfer(false)
      fetchData()
    } catch (err) {
      setMessage({ type: 'error', text: err.response?.data?.message || 'Error en la transferencia' })
    } finally {
      setTransferring(false)
    }
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-16">
        <Loader2 className="w-8 h-8 animate-spin text-blue-500" />
      </div>
    )
  }

  return (
    <div className="max-w-7xl mx-auto space-y-4">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3">
        <div>
          <h1 className="text-xl font-bold text-[var(--text-primary)]">Creditos</h1>
          <p className="text-sm text-[var(--text-muted)]">Gestion de creditos y transferencias</p>
        </div>
        <button onClick={() => setShowTransfer(!showTransfer)} className="btn btn-primary">
          <Send className="w-4 h-4" />
          Transferir
        </button>
      </div>

      {/* Summary Cards */}
      {summary && (
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
          <div className="card p-4">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Total Consumido</span>
              <div className="p-2 rounded-lg bg-red-500/10">
                <TrendingDown className="w-4 h-4 text-red-500" />
              </div>
            </div>
            <p className="text-2xl font-bold text-red-400">{summary.totalConsumed?.toLocaleString() || 0}</p>
          </div>

          <div className="card p-4">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Total Agregado</span>
              <div className="p-2 rounded-lg bg-green-500/10">
                <TrendingUp className="w-4 h-4 text-green-500" />
              </div>
            </div>
            <p className="text-2xl font-bold text-green-400">{summary.totalAdded?.toLocaleString() || 0}</p>
          </div>

          <div className="card p-4">
            <div className="flex items-center justify-between mb-3">
              <span className="text-xs font-medium text-[var(--text-muted)] uppercase tracking-wider">Balance Neto</span>
              <div className="p-2 rounded-lg bg-yellow-500/10">
                <DollarSign className="w-4 h-4 text-yellow-500" />
              </div>
            </div>
            <p className={`text-2xl font-bold ${summary.netBalance >= 0 ? 'text-green-400' : 'text-red-400'}`}>
              {summary.netBalance?.toLocaleString() || 0}
            </p>
          </div>
        </div>
      )}

      {/* Balance Card */}
      <div className="card p-6 bg-gradient-to-br from-blue-600/20 to-blue-900/10 border-blue-500/30">
        <div className="flex items-center gap-2 mb-2">
          <Wallet className="w-5 h-5 text-blue-400" />
          <span className="text-sm text-[var(--text-secondary)]">Balance disponible</span>
        </div>
        <p className="text-3xl font-bold text-white">{balance.toLocaleString()}</p>
        {balance <= 0 && (
          <p className="text-sm text-red-400 mt-2">Saldo agotado - Contacta a tu administrador</p>
        )}
      </div>

      {/* Transfer Form */}
      {showTransfer && (
        <form onSubmit={handleTransfer} className="card p-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-4">Transferir Creditos</h3>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">ID Destinatario</label>
              <input type="text" value={form.toUserId} onChange={(e) => setForm({ ...form, toUserId: e.target.value })} className="input font-mono text-sm" placeholder="uuid-del-usuario" required />
            </div>
            <div>
              <label className="block text-xs font-medium text-[var(--text-secondary)] mb-1">Monto</label>
              <input type="number" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} className="input" placeholder="0" min="1" required />
            </div>
          </div>
          <div className="flex gap-2 mt-4">
            <button type="submit" disabled={transferring} className="btn btn-primary">
              {transferring ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
              Enviar
            </button>
            <button type="button" onClick={() => { setShowTransfer(false); setMessage(null) }} className="btn btn-secondary">Cancelar</button>
          </div>
        </form>
      )}

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-lg text-sm ${
          message.type === 'success' ? 'bg-green-500/10 border border-green-500/30 text-green-400' : 'bg-red-500/10 border border-red-500/30 text-red-400'
        }`}>
          {message.type === 'success' ? <Check className="w-4 h-4" /> : <X className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* History */}
      <div className="card">
        <div className="px-4 py-3 border-b border-[var(--border-color)] flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--text-primary)]">Historial</h3>
          <button onClick={fetchData} className="text-xs text-[var(--text-muted)] hover:text-[var(--text-primary)] flex items-center gap-1">
            <RefreshCw className="w-3 h-3" /> Actualizar
          </button>
        </div>

        {history.length === 0 ? (
          <div className="px-4 py-8 text-center text-[var(--text-muted)] text-sm">Sin transacciones</div>
        ) : (
          <div className="divide-y divide-[var(--border-color)]">
            {history.map((tx) => {
              const typeInfo = TYPE_LABELS[tx.type] || TYPE_LABELS[0]
              const sourceBefore = tx.sourceBalanceBefore ?? 0
              const sourceAfter = tx.sourceBalanceAfter ?? 0
              const targetBefore = tx.targetBalanceBefore ?? 0
              const targetAfter = tx.targetBalanceAfter ?? 0
              return (
                <div key={tx.id} className="px-4 py-2.5 flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className={`w-2 h-2 rounded-full ${tx.direction === 'Received' ? 'bg-green-500' : 'bg-red-500'}`} />
                    <div>
                      <p className="text-sm text-[var(--text-primary)]">
                        {tx.direction === 'Received' ? tx.fromUserName : tx.toUserName}
                      </p>
                      <div className="flex items-center gap-2">
                        <span className="text-xs text-[var(--text-muted)]">
                          {new Date(tx.timestamp).toLocaleDateString('es-AR', { day: '2-digit', month: 'short', hour: '2-digit', minute: '2-digit' })}
                        </span>
                        <span className={`text-xs ${typeInfo.color}`}>{typeInfo.label}</span>
                      </div>
                      {tx.note && (
                        <p className="text-xs text-[var(--text-muted)] mt-1 max-w-[300px] truncate">{tx.note}</p>
                      )}
                      <div className="flex items-center gap-3 mt-1 text-xs text-[var(--text-muted)]">
                        {tx.direction === 'Sent' && (
                          <span>De: {sourceBefore} → {sourceAfter}</span>
                        )}
                        {tx.direction === 'Received' && (
                          <span>Para: {targetBefore} → {targetAfter}</span>
                        )}
                      </div>
                    </div>
                  </div>
                  <span className={`text-sm font-semibold flex items-center gap-1 ${
                    tx.direction === 'Received' ? 'text-green-400' : 'text-red-400'
                  }`}>
                    {tx.direction === 'Received' ? <ArrowDownRight className="w-3.5 h-3.5" /> : <ArrowUpRight className="w-3.5 h-3.5" />}
                    {tx.amount.toLocaleString()}
                  </span>
                </div>
              )
            })}
          </div>
        )}
      </div>

      {/* Admin Summary */}
      {summary && (
        <div className="card p-4">
          <h3 className="text-sm font-semibold text-[var(--text-primary)] mb-3 flex items-center gap-2">
            <DollarSign className="w-4 h-4 text-yellow-500" />
            Resumen Detallado (Admin)
          </h3>
          <div className="grid grid-cols-2 sm:grid-cols-4 gap-3 text-sm">
            <div>
              <p className="text-xs text-[var(--text-muted)]">Total Transferencias</p>
              <p className="font-semibold text-[var(--text-primary)]">{summary.totalTransfers || 0}</p>
            </div>
            <div>
              <p className="text-xs text-[var(--text-muted)]">Compras de Plan</p>
              <p className="font-semibold text-[var(--text-primary)]">{summary.totalPlanPurchases || 0}</p>
            </div>
            <div>
              <p className="text-xs text-[var(--text-muted)]">Consumido</p>
              <p className="font-semibold text-red-400">{summary.totalConsumed?.toLocaleString() || 0}</p>
            </div>
            <div>
              <p className="text-xs text-[var(--text-muted)]">Agregado</p>
              <p className="font-semibold text-green-400">{summary.totalAdded?.toLocaleString() || 0}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
