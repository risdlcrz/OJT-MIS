import axios from 'axios'

/**
 * Base URL for the OJTMISApi backend.
 *
 * Default ay relative ('/api') dahil ang Vite dev server (vite.config.js)
 * ang nagpo-proxy patungo sa .NET backend. Kaya walang hard-coded na port
 * at walang CORS. Override gamit ang VITE_API_URL kung kailangan ng
 * absolute URL (hal. sa production).
 */
const API_BASE_URL = import.meta.env.VITE_API_URL || '/api'

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
      window.history.pushState({}, '', '/login.html')
      window.dispatchEvent(new PopStateEvent('popstate'))
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

/**
 * Session helpers. Ang token at profile ay ini-store sa localStorage
 * para mabasa ng axios interceptor sa itaas.
 */
export const authAPI = {
  login: async (email, password) => {
    const { data } = await apiClient.post('/auth/login', { email, password })
    saveSession(data)
    return data.profile
  },
  register: async (payload) => {
    const { data } = await apiClient.post('/auth/register', payload)
    saveSession(data)
    return data.profile
  },
  me: async () => {
    const { data } = await apiClient.get('/auth/me')
    saveProfile(data)
    return data
  },
  logout: () => clearSession()
}

export function saveProfile(profile) {
  localStorage.setItem('ojtUser', JSON.stringify(profile))
}

export function saveSession(data) {
  localStorage.setItem('authToken', data.token)
  localStorage.setItem('ojtExpiresAt', data.expiresAt)
  saveProfile(data.profile)
}

export function clearSession() {
  localStorage.removeItem('authToken')
  localStorage.removeItem('ojtUser')
  localStorage.removeItem('ojtExpiresAt')
}

/** Kasalukuyang profile mula sa localStorage. */
export function currentUser() {
  try {
    return JSON.parse(localStorage.getItem('ojtUser') || 'null')
  } catch {
    return null
  }
}

/** True kapag may naka-login at HR Admin ang role. */
export function isHRAdmin() {
  return (currentUser()?.roles || []).includes('HRAdmin')
}

export const schoolsAPI = crud('schools')
export const internsAPI = crud('interns')
export const applicantsAPI = {
  ...crud('applicants'),
  /* Preview ng start/end date bago i-save. */
  estimateDates: async (payload) => (await apiClient.post('/applicants/estimate-dates', payload)).data,
  /* I-hire ang applicant: kinopya ang profile sa Intern + kinakalkula ang dates. */
  hire: async (id, payload) => (await apiClient.post(`/applicants/${id}/hire`, payload)).data
}
export const internRequestsAPI = {
  ...crud('internrequests'),
  /* Ang mga request na may natitirang slot lamang. */
  available: async () => (await apiClient.get('/internrequests/available')).data
}
export const programsAPI = crud('programs')
export const signatoriesAPI = crud('signatories')

export default apiClient
