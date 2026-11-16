import { ref } from 'vue'
import { getApiErrorMessage, supervisorAPI } from '@/services/api'

/* The department rarely changes during a session, so every supervisor page
   shares one request. Keyed by token so a new login never sees the
   previous user's department. */
let departmentPromise = null
let departmentToken = null

/**
 * Department scope of the signed-in supervisor (from the linked HR
 * signatory record). `scopeError` is set when HR has not assigned a
 * department yet; pages show it instead of an empty table.
 */
export function useDepartment() {
  const department = ref('')
  const signatory = ref(null)
  const scopeError = ref('')

  async function loadDepartment() {
    scopeError.value = ''
    try {
      const token = localStorage.getItem('authToken')
      if (!departmentPromise || departmentToken !== token) {
        departmentToken = token
        departmentPromise = supervisorAPI.department()
      }
      const data = await departmentPromise
      department.value = data?.department || ''
      signatory.value = data?.signatory || null
    } catch (e) {
      departmentPromise = null
      scopeError.value = getApiErrorMessage(e, 'Unable to load your department.')
    }
  }

  return { department, signatory, scopeError, loadDepartment }
}

/** Navigate inside the single-page app without a full reload. */
export function go(path) {
  window.history.pushState({}, '', path.startsWith('/') ? path : `/${path}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

/** True when a value has something worth showing in a table cell. */
export function present(value) {
  return value !== null && value !== undefined && String(value).trim() !== ''
}

/** Whole days from today until the given date (negative when past). */
export function daysUntil(value) {
  if (!value) return null
  const text = String(value).slice(0, 10)
  const target = new Date(`${text}T00:00:00`)
  if (Number.isNaN(target.getTime())) return null
  const today = new Date()
  today.setHours(0, 0, 0, 0)
  return Math.round((target - today) / 86400000)
}

/** "Today", "Tomorrow", "In 5 days", "3 days ago". */
export function relativeDay(value) {
  const days = daysUntil(value)
  if (days === null) return ''
  if (days === 0) return 'Today'
  if (days === 1) return 'Tomorrow'
  if (days === -1) return 'Yesterday'
  return days > 0 ? `In ${days} days` : `${Math.abs(days)} days ago`
}

/** Label for an attendance adjustment target field. */
export function adjustmentFieldLabel(field) {
  return {
    AM_In: 'AM Time In',
    AM_Out: 'AM Time Out',
    PM_In: 'PM Time In',
    PM_Out: 'PM Time Out'
  }[field] || field || ''
}
