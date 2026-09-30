import axios from 'axios'

/**
 * Base URL for the OJTMISApi backend.
 * Override per environment with a VITE_API_URL entry in a .env file.
 */
const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5080/api'

/**
 * Shared axios instance used by every resource API below.
 * Keeps base URL, timeout and auth/error handling in one place.
 */
const apiClient = axios.create({
  baseURL: API_BASE_URL,
  timeout: 15000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Attach the bearer token to every outgoing request when one is available.
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('authToken')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Normalise API failures so callers can always read a human-readable message.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('authToken')
      window.location.href = '/login.html'
    }

    const data = error.response?.data
    let message = error.message || 'Request failed.'

    if (data) {
      if (typeof data === 'string') {
        message = data
      } else if (data.message) {
        message = data.message
      } else if (data.title) {
        message = data.title
      } else if (data.errors) {
        // ASP.NET Core ValidationProblemDetails shape.
        message = Object.values(data.errors).flat().join(' ') || message
      }
    }

    return Promise.reject(Object.assign(error, { userMessage: message }))
  }
)

/**
 * Extracts a readable message from anything thrown by the API layer.
 * @param {unknown} error
 * @param {string} fallback
 * @returns {string}
 */
export function getApiErrorMessage(error, fallback = 'Something went wrong. Please try again.') {
  if (error?.userMessage) return error.userMessage
  if (error?.response?.data?.message) return error.response.data.message
  if (error?.message) return error.message
  return fallback
}

/**
 * Builds a CRUD helper set for a lowercase REST resource.
 * @param {string} resource
 */
function crud(resource) {
  return {
    getAll: async () => (await apiClient.get(`/${resource}`)).data,
    getById: async (id) => (await apiClient.get(`/${resource}/${id}`)).data,
    create: async (data) => (await apiClient.post(`/${resource}`, data)).data,
    update: async (id, data) => (await apiClient.put(`/${resource}/${id}`, data)).data,
    delete: async (id) => (await apiClient.delete(`/${resource}/${id}`)).data
  }
}

export const schoolsAPI = crud('schools')
export const internsAPI = crud('interns')
export const programsAPI = crud('programs')
export const signatoriesAPI = crud('signatories')

export default apiClient
