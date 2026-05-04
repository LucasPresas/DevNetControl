import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('user')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// ===== Activity Logs =====
export const getActivities = (params = {}) => api.get('/activity', { params })
export const getRecentActivities = (limit = 10) => api.get('/activity/recent', { params: { limit } })
export const getActivityDetail = (id) => api.get(`/activity/${id}`)
export const getActivitiesByUser = (userId, params = {}) => api.get(`/activity/user/${userId}`, { params })
export const getActivityStats = () => api.get('/activity/stats')
export const getCreditsSummary = (params = {}) => api.get('/activity/credits/summary', { params })

// ===== User Operations (Admin/SubReseller) =====
export const addConnection = (userId, data) => api.post(`/user/${userId}/add-connection`, data)
export const renewPlan = (userId, data) => api.post(`/user/${userId}/renew-plan`, data)
export const updateUserBasic = (userId, data) => api.put(`/admin/users/${userId}/update-basic`, data)

// ===== Credits Summary (from AuditController) =====
export const getCreditHistorySummary = () => api.get('/audit/history/summary')

// ===== Plans =====
export const getPlans = () => api.get('/plan')

export default api
