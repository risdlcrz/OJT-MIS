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
  getAll: async () => {
    const response = await apiClient.get('/applicants')
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/applicants/${id}`)
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/applicants', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/applicants/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/applicants/${id}`)
    return response.data
  }
}

// Programs API
export const programsAPI = {
  getAll: async () => {
    const response = await apiClient.get('/programs', { timeout: 0 })
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/programs/${id}`, { timeout: 0 })
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/programs', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/programs/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/programs/${id}`)
    return response.data
  }
}

// Schools API
export const schoolsAPI = {
  getAll: async () => {
    const response = await apiClient.get('/schools')
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/schools/${id}`)
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/schools', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/schools/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/schools/${id}`)
    return response.data
  }
}

// Signatories API
export const signatoriesAPI = {
  getAll: async () => {
    const response = await apiClient.get('/signatories')
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/signatories/${id}`)
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/signatories', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/signatories/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/signatories/${id}`)
    return response.data
  }
}

// Requests API
export const requestsAPI = {
  getAll: async () => {
    const response = await apiClient.get('/requests')
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/requests/${id}`)
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/requests', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/requests/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/requests/${id}`)
    return response.data
  }
}

// Interns API
export const internsAPI = {
  getAll: async () => {
    const response = await apiClient.get('/interns')
    return response.data
  },
  getById: async (id) => {
    const response = await apiClient.get(`/interns/${id}`)
    return response.data
  },
  create: async (data) => {
    const response = await apiClient.post('/interns', data)
    return response.data
  },
  update: async (id, data) => {
    const response = await apiClient.put(`/interns/${id}`, data)
    return response.data
  },
  delete: async (id) => {
    const response = await apiClient.delete(`/interns/${id}`)
    return response.data
  }
}

// Dashboard API
export const dashboardAPI = {
  getStats: async () => {
    const response = await apiClient.get('/dashboard/stats')
    return response.data
  }
}

export default apiClient
