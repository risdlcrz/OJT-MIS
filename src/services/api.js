import axios from 'axios'

// Get API base URL from environment or use default
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000/api'

// Create axios instance with default config
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Add request interceptor
apiClient.interceptors.request.use(
  (config) => {
    // Add auth token if available
    const token = localStorage.getItem('authToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Add response interceptor
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Handle common errors
    if (error.response?.status === 401) {
      // Unauthorized - redirect to login
      localStorage.removeItem('authToken')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// Applicants API
export const applicantsAPI = {
  getAll: () => apiClient.get('/applicants'),
  getById: (id) => apiClient.get(`/applicants/${id}`),
  create: (data) => apiClient.post('/applicants', data),
  update: (id, data) => apiClient.put(`/applicants/${id}`, data),
  delete: (id) => apiClient.delete(`/applicants/${id}`)
}

// Programs API
export const programsAPI = {
  getAll: () => apiClient.get('/programs'),
  getById: (id) => apiClient.get(`/programs/${id}`),
  create: (data) => apiClient.post('/programs', data),
  update: (id, data) => apiClient.put(`/programs/${id}`, data),
  delete: (id) => apiClient.delete(`/programs/${id}`)
}

// Schools API
export const schoolsAPI = {
  getAll: () => apiClient.get('/schools'),
  getById: (id) => apiClient.get(`/schools/${id}`),
  create: (data) => apiClient.post('/schools', data),
  update: (id, data) => apiClient.put(`/schools/${id}`, data),
  delete: (id) => apiClient.delete(`/schools/${id}`)
}

// Signatories API
export const signatoriesAPI = {
  getAll: () => apiClient.get('/signatories'),
  getById: (id) => apiClient.get(`/signatories/${id}`),
  create: (data) => apiClient.post('/signatories', data),
  update: (id, data) => apiClient.put(`/signatories/${id}`, data),
  delete: (id) => apiClient.delete(`/signatories/${id}`)
}

// Requests API
export const requestsAPI = {
  getAll: () => apiClient.get('/requests'),
  getById: (id) => apiClient.get(`/requests/${id}`),
  create: (data) => apiClient.post('/requests', data),
  update: (id, data) => apiClient.put(`/requests/${id}`, data),
  delete: (id) => apiClient.delete(`/requests/${id}`)
}

// Interns API
export const internsAPI = {
  getAll: () => apiClient.get('/interns'),
  getById: (id) => apiClient.get(`/interns/${id}`),
  create: (data) => apiClient.post('/interns', data),
  update: (id, data) => apiClient.put(`/interns/${id}`, data),
  delete: (id) => apiClient.delete(`/interns/${id}`)
}

// Dashboard API
export const dashboardAPI = {
  getStats: () => apiClient.get('/dashboard/stats')
}

export default apiClient
