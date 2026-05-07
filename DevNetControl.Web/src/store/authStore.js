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
       const userData = {
         userName: data.user,
         role: data.role,
         userId: data.userId,
         credits: data.credits ?? 0,
       }
       localStorage.setItem('token', data.accessToken)
      localStorage.setItem('user', JSON.stringify(userData))
       set({ user: userData, token: data.accessToken, isAuthenticated: true, isLoading: false })
      return { success: true }
    } catch (err) {
      const message = err.response?.data?.message || 'Error al iniciar sesion'
      set({ error: message, isLoading: false })
      return { success: false, message }
    }
  },

  logout: () => {
    localStorage.removeItem('token')
    localStorage.removeItem('user')
    set({ user: null, token: null, isAuthenticated: false, error: null })
  },

  setUser: (user) => {
    localStorage.setItem('user', JSON.stringify(user))
    set({ user })
  },

  updateCredits: (credits) => {
    set((state) => {
      const updated = { ...state.user, credits }
      localStorage.setItem('user', JSON.stringify(updated))
      return { user: updated }
    })
  },
}))
