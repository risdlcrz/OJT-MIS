import { createRouter, createWebHistory } from 'vue-router'
import { clearSession, isHRAdmin } from '@/services/api'
import { canOpen } from '@/utils/access'
import { pageOf } from '@/utils/navigation'

/*
 * Every page keeps its existing "*.html" address, so emailed links
 * (login.html?mode=reset / ?mode=confirm), bookmarks and notification deep
 * links keep working. Pages load lazily: each one is downloaded the first
 * time it is opened.
 *
 * Who may open what is still decided by src/utils/access.js. A new page must
 * be added there and here.
 */

const ModulePage = () => import('../../pages/ModulePage.vue')

/* The dashboard is a different screen for each role (chosen per render). */
const RoleDashboard = () => import('../components/RoleDashboard.vue')

const pages = {
  'dashboard.html': RoleDashboard,
  'requests.html': () => import('../../pages/requests.vue'),
  'time-adjustments.html': () => import('../../pages/time-adjustments.vue'),
  'applicants.html': () => import('../../pages/applicants.vue'),
  'schools.html': () => import('../../pages/Schools.vue'),
  'account.html': () => import('../../pages/account.vue'),
  'dtr.html': () => import('../../pages/dtr.vue'),
  'attendance.html': () => import('../../pages/attendance.vue'),
  'logs.html': () => import('../../pages/logs.vue'),
  'idgenerate.html': () => import('../../pages/idgenerate.vue'),
  'coc-generate.html': () => import('../../pages/coc-generate.vue'),
  'documents-history.html': () => import('../../pages/documents-history.vue'),
  'intern-dtr.html': () => import('../../pages/intern-dtr.vue'),
  'intern-attendance.html': () => import('../../pages/intern-attendance.vue'),
  'intern-documents.html': () => import('../../pages/intern-documents.vue'),
  'supervisor-interns.html': () => import('../../pages/supervisor-interns.vue'),
  'supervisor-dtr.html': () => import('../../pages/supervisor-dtr.vue'),
  'supervisor-time-adjustments.html': () => import('../../pages/supervisor-time-adjustments.vue'),
  'supervisor-orientations.html': () => import('../../pages/supervisor-orientations.vue'),
  'supervisor-requests.html': () => import('../../pages/supervisor-requests.vue'),
  'forbidden.html': () => import('../../pages/forbidden.vue')
}

/* Pages served by the generic ModulePage, which needs to know which one. */
const modulePages = ['internlist.html', 'forms.html', 'utility.html', 'signatories.html', 'contact.html']

const routes = [
  { path: '/', redirect: (to) => ({ path: '/login.html', query: to.query }) },
  { path: '/index.html', redirect: (to) => ({ path: '/login.html', query: to.query }) },
  { path: '/login', redirect: (to) => ({ path: '/login.html', query: to.query }) },
  {
    path: '/login.html',
    name: 'login',
    component: () => import('../../pages/login.vue'),
    meta: { bare: true }
  },
  ...Object.entries(pages).map(([page, component]) => ({ path: `/${page}`, component })),
  ...modulePages.map((page) => ({
    path: `/${page}`,
    component: ModulePage,
    props: { modulePage: page }
  })),
  /* Unknown address: the guard decides between dashboard and forbidden. */
  { path: '/:unknown(.*)*', component: pages['forbidden.html'] }
]

const knownPages = new Set([...Object.keys(pages), ...modulePages])

/**
 * A stored token only counts while it has not expired (#17): an expired
 * session goes straight back to sign-in instead of failing request by request.
 */
export function hasValidSession() {
  if (!localStorage.getItem('authToken')) return false

  const expiresAt = Date.parse(localStorage.getItem('ojtExpiresAt') || '')
  if (Number.isFinite(expiresAt) && expiresAt <= Date.now()) {
    clearSession()
    return false
  }

  return true
}

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior: (to, from, saved) => saved || (to.path !== from.path ? { top: 0 } : false)
})

router.beforeEach((to) => {
  if (to.name === 'login') return true

  if (!hasValidSession()) {
    return { path: '/login.html' }
  }

  const page = pageOf(to.path)
  if (page === 'forbidden.html' || canOpen(page)) return true

  // HR typing an unknown address lands on the dashboard; every other case
  // (another role's page, or no recognized role) is forbidden.
  return isHRAdmin() && !knownPages.has(page)
    ? { path: '/dashboard.html' }
    : { path: '/forbidden.html' }
})

/*
 * Once a session has expired, the next navigation goes to sign-in. Check
 * every minute too, so an idle tab does not sit on a dead session.
 */
window.setInterval(() => {
  if (router.currentRoute.value.name !== 'login' && !hasValidSession()) {
    router.replace('/login.html')
  }
}, 60 * 1000)

export default router
