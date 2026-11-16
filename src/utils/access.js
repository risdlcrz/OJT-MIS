import { isHRAdmin, isIntern, isSupervisor } from '@/services/api'

/*
 * Single source of truth for which pages each role may open. Used by the
 * router (App.vue), the sidebar, and notification deep links (BaseLayout.vue).
 * A new page must be added here and to pageComponents in App.vue.
 */

export const HR_PAGES = [
  'dashboard.html',
  'account.html',
  'requests.html',
  'time-adjustments.html',
  'applicants.html',
  'schools.html',
  'internlist.html',
  'forms.html',
  'coc-generate.html',
  'documents-history.html',
  'dtr.html',
  'attendance.html',
  'logs.html',
  'idgenerate.html',
  'utility.html',
  'signatories.html',
  'contact.html'
]

export const INTERN_PAGES = [
  'dashboard.html',
  'account.html',
  'intern-dtr.html',
  'intern-attendance.html',
  'intern-documents.html'
]

export const SUPERVISOR_PAGES = [
  'dashboard.html',
  'account.html',
  'supervisor-interns.html',
  'supervisor-dtr.html',
  'supervisor-time-adjustments.html',
  'supervisor-orientations.html',
  'supervisor-requests.html'
]

/** Every page the app can render after login, for any role. */
export const ALL_PAGES = [...new Set([...HR_PAGES, ...INTERN_PAGES, ...SUPERVISOR_PAGES])]

/** Pages the signed-in user's role may open. */
export function allowedPages() {
  if (isIntern()) return INTERN_PAGES
  if (isSupervisor()) return SUPERVISOR_PAGES
  if (isHRAdmin()) return HR_PAGES
  return []
}

/** True when the signed-in user may open the page ("requests.html"). */
export function canOpen(page) {
  return allowedPages().includes(String(page || '').toLowerCase())
}
