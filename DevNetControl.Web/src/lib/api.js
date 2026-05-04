import axios from 'axios'

const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
})

// ===== DEBUG: Request Interceptor =====
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  
  // LOG: Show request details
  console.group(`🚀 API REQUEST: ${config.method.toUpperCase()} ${config.url}`)
  console.log('📦 Payload:', config.data || 'N/A')
  console.log('🔑 Auth:', token ? 'Bearer token present' : 'No auth')
  console.groupEnd()
  
  return config
})

// ===== DEBUG: Response Interceptor =====
api.interceptors.response.use(
  (response) => {
    console.group(`✅ API RESPONSE: ${response.status} ${response.config.method.toUpperCase()} ${response.config.url}`)
    console.log('📋 Data:', response.data)
    console.groupEnd()
    return response
  },
  (error) => {
    console.group(`❌ API ERROR: ${error.response?.status || 'Network Error'} ${error.config?.method?.toUpperCase()} ${error.config?.url}`)
    console.error('⚠️ Error Details:', {
      status: error.response?.status,
      statusText: error.response?.statusText,
      data: error.response?.data,
      config: {
        method: error.config?.method,
        url: error.config?.url,
        data: error.config?.data
      }
    })
    console.groupEnd()
    
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

// ===== Bulk Operations =====
export const bulkDeleteResellers = (ids) => api.post('/user/bulk/delete', { UserIds: ids })
export const bulkToggleSuspendResellers = (ids) => api.post('/user/bulk/toggle-suspend', { UserIds: ids })
export const bulkDeleteNodes = (ids) => api.post('/vpsnode/bulk-delete', { nodeIds: ids })
export const bulkDeletePlans = (ids) => api.post('/plan/bulk-delete', { planIds: ids })

export default api
