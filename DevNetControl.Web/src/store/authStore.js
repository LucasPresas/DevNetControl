import { create } from 'zustand'
import api from '../lib/api'

export const useAuthStore = create((set) => ({
  user: JSON.parse(localStorage.getItem('user') || 'null'),
  token: localStorage.getItem('token'),
  isAuthenticated: !!localStorage.getItem('token'),
  isLoading: false,
  error: null,

  login: async (userName, password) => {
    set({ isLoading: true, error: null })
    try {
      const { data } = await api.post('/auth/login', { userName, password })
      localStorage.setItem('token', data.token)
      localStorage.setItem('user', JSON.stringify({ userName: data.user, role: data.role }))
      set({ user: { userName: data.user, role: data.role }, token: data.token, isAuthenticated: true, isLoading: false })
      return { success: true }
    } catch (err) {
      const message = err.response?.data?.message || 'Error al iniciar sesión'
      set({ error: message, isLoading: false })
      return { success: false, message }
    }
  },

  logout: () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    set({ user: null, token: null, isAuthenticated: false, error: null })
  },

  setUser: (user) => set({ user }),
}))
