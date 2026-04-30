import { useEffect, useState } from 'react'
import api from '../lib/api'
import { Wallet, ArrowUpRight, ArrowDownRight, Send, Clock, Loader2, AlertCircle, CheckCircle } from 'lucide-react'

export default function Credits() {
  const [balance, setBalance] = useState(0)
  const [history, setHistory] = useState([])
  const [loading, setLoading] = useState(true)
  const [showTransfer, setShowTransfer] = useState(false)
  const [transferForm, setTransferForm] = useState({ toUserId: '', amount: '' })
  const [transferring, setTransferring] = useState(false)
  const [message, setMessage] = useState(null)

  useEffect(() => {
    fetchData()
  }, [])

  async function fetchData() {
    try {
      const [balanceRes, historyRes] = await Promise.all([
        api.get('/credit/balance'),
        api.get('/credit/history'),
      ])
      setBalance(balanceRes.data.balance)
      setHistory(historyRes.data)
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
        toUserId: transferForm.toUserId,
        amount: parseFloat(transferForm.amount),
      })
      setMessage({ type: 'success', text: data.message })
      setTransferForm({ toUserId: '', amount: '' })
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
      <div className="flex items-center justify-center py-12">
        <Loader2 className="w-8 h-8 animate-spin text-primary-600" />
      </div>
    )
  }

  return (
    <div className="space-y-4">
      <h2 className="text-xl font-bold text-gray-900 dark:text-white">Creditos</h2>

      {/* Balance Card */}
      <div className="bg-gradient-to-br from-primary-600 to-primary-800 rounded-xl p-6 text-white shadow-lg">
        <div className="flex items-center gap-2 mb-2">
          <Wallet className="w-5 h-5 opacity-80" />
          <span className="text-sm opacity-80">Balance disponible</span>
        </div>
        <p className="text-4xl font-bold">{balance.toLocaleString()}</p>
      </div>

      {/* Transfer Button */}
      <button
        onClick={() => setShowTransfer(!showTransfer)}
        className="w-full flex items-center justify-center gap-2 bg-primary-600 text-white py-3 rounded-xl font-medium hover:bg-primary-700 transition-colors"
      >
        <Send className="w-4 h-4" />
        Transferir creditos
      </button>

      {/* Transfer Form */}
      {showTransfer && (
        <form onSubmit={handleTransfer} className="bg-white dark:bg-gray-800 rounded-xl p-4 shadow-sm border border-gray-100 dark:border-gray-700 space-y-3 transition-colors">
          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">ID del destinatario</label>
            <input
              type="text"
              value={transferForm.toUserId}
              onChange={(e) => setTransferForm({ ...transferForm, toUserId: e.target.value })}
              className="w-full px-3 py-2.5 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none font-mono text-sm"
              placeholder="uuid-del-usuario"
              required
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Monto</label>
            <input
              type="number"
              value={transferForm.amount}
              onChange={(e) => setTransferForm({ ...transferForm, amount: e.target.value })}
              className="w-full px-3 py-2.5 border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-700 text-gray-900 dark:text-white rounded-lg focus:ring-2 focus:ring-primary-500 focus:border-transparent outline-none"
              placeholder="0"
              min="1"
              step="1"
              required
            />
          </div>

          <div className="flex gap-2">
            <button
              type="submit"
              disabled={transferring}
              className="flex-1 bg-primary-600 text-white py-2.5 rounded-lg font-medium hover:bg-primary-700 disabled:bg-primary-400 transition-colors flex items-center justify-center gap-2"
            >
              {transferring ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
              Enviar
            </button>
            <button
              type="button"
              onClick={() => { setShowTransfer(false); setMessage(null) }}
              className="px-4 py-2.5 border border-gray-300 dark:border-gray-600 dark:text-gray-300 rounded-lg text-gray-600 hover:bg-gray-50 dark:hover:bg-gray-700 transition-colors"
            >
              Cancelar
            </button>
          </div>
        </form>
      )}

      {message && (
        <div className={`flex items-center gap-2 px-4 py-3 rounded-xl text-sm ${
          message.type === 'success' ? 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-400' : 'bg-red-50 dark:bg-red-900/30 text-red-700 dark:text-red-400'
        }`}>
          {message.type === 'success' ? <CheckCircle className="w-4 h-4" /> : <AlertCircle className="w-4 h-4" />}
          {message.text}
        </div>
      )}

      {/* History */}
      <div className="bg-white dark:bg-gray-800 rounded-xl shadow-sm border border-gray-100 dark:border-gray-700 transition-colors">
        <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
          <h3 className="font-semibold text-gray-900 dark:text-white flex items-center gap-2">
            <Clock className="w-4 h-4" />
            Historial
          </h3>
        </div>

        {history.length === 0 ? (
          <div className="px-4 py-8 text-center text-gray-400 text-sm">
            No hay transacciones aun
          </div>
        ) : (
          <div className="divide-y divide-gray-100 dark:divide-gray-700">
            {history.map((tx) => (
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
                      {tx.direction === 'Received' ? 'De' : 'A'} {tx.direction === 'Received' ? tx.fromUserName : tx.toUserName}
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
