import axios from 'axios'
import { go } from '@/utils/navigation'

/**
 * Base URL for the OJTMISApi backend.
 *
 * The default is relative ('/api') because the Vite dev server (vite.config.js)
 * forwards it to the .NET backend. So there is no hard-coded port
 * and no CORS. Set VITE_API_URL if you need an
 * absolute URL (for example, in production).
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

// Modules the notification bell watches. After a successful change
// here, the bell refreshes right away (it does not wait for the poll).
const NOTIFICATION_MODULES = ['/schools', '/signatories', '/applicants', '/interns', '/internrequests', '/account/profile']

// Normalise API failures so callers can always read a human-readable message.
apiClient.interceptors.response.use(
  (response) => {
    const method = (response.config.method || '').toLowerCase()
    const url = response.config.url || ''
    const isMonitored = NOTIFICATION_MODULES.some((path) => url.startsWith(path))

    if (isMonitored && ['post', 'put', 'patch', 'delete'].includes(method)) {
      window.dispatchEvent(new Event('ojt:notifications-changed'))
    }

    return response
  },
  (error) => {
    if (error.response?.status === 401) {
      clearSession()
      go('/login.html')
    }

    const data = error.response?.data
    let message = error.message || 'Request failed.'

    if (data) {
      if (typeof data === 'string') {
        message = data
      } else if (data.message) {
        message = data.message
      } else if (data.errors) {
        // ASP.NET Core ValidationProblemDetails shape. Show the specific
        // errors (for example, "Full Name is required.") before the
        // general title "One or more validation errors occurred."
        message = Object.values(data.errors).flat().join(' ') || data.title || message
      } else if (data.title) {
        message = data.title
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
 * Session helpers. The token and profile are kept in localStorage
 * so the axios interceptor above can read them.
 */
export const authAPI = {
  login: async (email, password) => {
    const { data } = await apiClient.post('/auth/login', { email, password })
    saveSession(data)
    return data.profile
  },
  /* No session is returned: the email must be verified and HR must approve first. */
  register: async (payload) => (await apiClient.post('/auth/register', payload)).data,
  confirmEmail: async (payload) => (await apiClient.post('/auth/confirm-email', payload)).data,
  resendConfirmation: async (email) => (await apiClient.post('/auth/resend-confirmation', { email })).data,
  me: async () => {
    const { data } = await apiClient.get('/auth/me')
    saveProfile(data)
    return data
  },
  logout: () => clearSession(),
  forgotPassword: async (email) => (await apiClient.post('/auth/forgot-password', { email })).data,
  resetPassword: async (payload) => (await apiClient.post('/auth/reset-password', payload)).data
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

/** The current profile from localStorage. */
export function currentUser() {
  try {
    return JSON.parse(localStorage.getItem('ojtUser') || 'null')
  } catch {
    return null
  }
}

/** True when someone is signed in and their role is HR Admin. */
export function isHRAdmin() {
  return (currentUser()?.roles || []).includes('HRAdmin')
}

/** True when the role is Intern and not HR Admin. */
export function isIntern() {
  const roles = currentUser()?.roles || []
  return roles.includes('Intern') && !roles.includes('HRAdmin')
}

/** True for department supervisors / signatories who are not HR Admin. */
export function isSupervisor() {
  const roles = currentUser()?.roles || []
  return roles.includes('Supervisor') && !roles.includes('HRAdmin')
}

export const schoolsAPI = crud('schools')
export const internsAPI = {
  ...crud('interns'),
  /* Current ID code (null until an ID is generated). Reading never changes it. */
  attendanceQr: async (id) => (await apiClient.get(`/interns/${id}/attendance-qr`)).data,
  /* Issues a new unique code for a new ID; the previous card stops working. */
  issueAttendanceQr: async (id) => (await apiClient.post(`/interns/${id}/attendance-qr`)).data
}
export const applicantsAPI = {
  ...crud('applicants'),
  /* Preview the start and end date before saving. */
  estimateDates: async (payload) => (await apiClient.post('/applicants/estimate-dates', payload)).data,
  /* Hire the applicant: copies the profile to an Intern and works out the dates. */
  hire: async (id, payload) => (await apiClient.post(`/applicants/${id}/hire`, payload)).data
}
export const internRequestsAPI = {
  ...crud('internrequests'),
  /* Only the requests that still have open slots. */
  available: async () => (await apiClient.get('/internrequests/available')).data,
  /* Change only the status (e.g. open a supervisor's Pending request). */
  setStatus: async (request, status) =>
    (await apiClient.put(`/internrequests/${request.id}`, { ...request, status })).data
}

/**
 * Supervisor / signatory portal. Every endpoint is scoped server-side to the
 * department of the signed-in supervisor.
 */
export const supervisorAPI = {
  department: async () => (await apiClient.get('/supervisor/department')).data,
  interns: async (params = {}) => (await apiClient.get('/supervisor/interns', { params })).data,
  dtr: async (params = {}) => (await apiClient.get('/supervisor/dtr', { params })).data,
  timeAdjustments: async (status = 'Pending') =>
    (await apiClient.get('/supervisor/time-adjustments', { params: { status } })).data,
  reviewTimeAdjustment: async (id, decision) =>
    (await apiClient.post(`/supervisor/time-adjustments/${id}/review`, { decision })).data,
  applicants: async (params = {}) => (await apiClient.get('/supervisor/applicants', { params })).data,
  orientations: async (params = {}) => (await apiClient.get('/supervisor/orientations', { params })).data,
  slots: async () => (await apiClient.get('/supervisor/slots')).data,
  requestSlots: async (payload) => (await apiClient.post('/supervisor/slots', payload)).data,
  updateSlots: async (id, payload) => (await apiClient.put(`/supervisor/slots/${id}`, payload)).data
}
export const programsAPI = crud('programs')
export const signatoriesAPI = crud('signatories')
export const dtrAPI = crud('dtr')
export const internPortalAPI = {
  getPortal: async () => (await apiClient.get('/intern/portal')).data,
  logTime: async (payload) => (await apiClient.post('/Attendance/LogTime', { payload })).data,
  scanDtr: async (payload) => (await apiClient.post('/Attendance/LogTime', { payload })).data,
  requestDocument: async (documentType, notes = '') =>
    (await apiClient.post('/intern/documents', { documentType, notes })).data,
  requestAdjustment: async ({ attendanceLogId, requestedTime, targetField, reason }) =>
    (await apiClient.post('/Attendance/RequestAdjustment', {
      attendanceLogId,
      requestedTime,
      targetField,
      reason
    })).data
}
export const notificationsAPI = {
  getAll: async (take = 20) => (await apiClient.get('/notifications', { params: { take } })).data,
  markRead: async (id) => (await apiClient.patch(`/notifications/${id}/read`)).data,
  markAllRead: async () => (await apiClient.patch('/notifications/read-all')).data,
  clear: async () => (await apiClient.delete('/notifications')).data
}

/** Kasalukuyang account: profile at change-password. */
export const accountAPI = {
  getProfile: async () => (await apiClient.get('/account/profile')).data,
  updateProfile: async (payload) => (await apiClient.put('/account/profile', payload)).data,
  /* The server signs out other devices and returns a fresh token for this one. */
  changePassword: async (payload) => {
    const data = (await apiClient.post('/account/change-password', payload)).data
    if (data?.token) {
      localStorage.setItem('authToken', data.token)
      localStorage.setItem('ojtExpiresAt', data.expiresAt)
    }
    return data
  }
}

/** HR Admin only: list and manage user accounts. */
export const adminUsersAPI = {
  getAll: async (role = '') =>
    (await apiClient.get('/admin/users', { params: role ? { role } : {} })).data,
  create: async (payload) => (await apiClient.post('/admin/users', payload)).data,
  update: async (id, payload) => (await apiClient.put(`/admin/users/${id}`, payload)).data,
  sendPasswordReset: async (id) => (await apiClient.post(`/admin/users/${id}/password-reset`)).data,
  approve: async (id) => (await apiClient.post(`/admin/users/${id}/approve`)).data,
  availablePeople: async () => (await apiClient.get('/admin/users/available-people')).data
}

/** HR Admin only: review attendance time adjustment requests. */
export const hrAttendanceAPI = {
  getPending: async () => (await apiClient.get('/HR/AttendanceRequests/Pending')).data,
  approve: async (id) => (await apiClient.post(`/HR/AttendanceRequests/Approve/${id}`)).data,
  reject: async (id) => (await apiClient.post(`/HR/AttendanceRequests/Reject/${id}`)).data,
  eligibleForCertificate: async () => (await apiClient.get('/HR/Certificates/Eligible')).data,
  logGeneratedDocument: async (payload) => (await apiClient.post('/HR/Documents/Generated', payload)).data,
  /* Permanent issuance history (ID cards and certificates) per intern. */
  getGeneratedDocuments: async (internId = null) =>
    (await apiClient.get('/HR/Documents/Generated', { params: internId ? { internId } : {} })).data
}

/**
 * HR Admin only: renders the Certificate of Completion and emails the PDF to
 * the intern's registered address. Rendering plus delivery can take longer than
 * a normal API call, so this request gets a longer timeout.
 */
export const certificateAPI = {
  generateAndSend: async (internId, payload = {}) =>
    (await apiClient.post(`/Certificate/GenerateAndSend/${internId}`, payload, { timeout: 60000 })).data
}

/** HR Admin only: audit trail of every user's actions. */
export const logsAPI = {
  getAll: async ({ category = 'all', search = '', take = 500 } = {}) =>
    (await apiClient.get('/logs', {
      params: { category, search, take }
    })).data
}

export default apiClient
