<script setup>
import { onMounted, ref } from 'vue'
import BaseLayout from './components/BaseLayout.vue'
import LoginPage from '../pages/login.vue'
import DashboardPage from '../pages/dashboard.vue'
import RequestsPage from '../pages/requests.vue'
import TimeAdjustmentsPage from '../pages/time-adjustments.vue'
import ApplicantsPage from '../pages/applicants.vue'
import SchoolsPage from '../pages/Schools.vue'
import AccountPage from '../pages/account.vue'
import IdGeneratePage from '../pages/idgenerate.vue'
import LogsPage from '../pages/logs.vue'
import ModulePage from '../pages/ModulePage.vue'
import AttendancePage from '../pages/attendance.vue'
import ForbiddenPage from '../pages/forbidden.vue'
import InternDashboardPage from '../pages/intern-dashboard.vue'
import InternDtrPage from '../pages/intern-dtr.vue'
import InternAttendancePage from '../pages/intern-attendance.vue'
import InternDocumentsPage from '../pages/intern-documents.vue'
import CocGeneratePage from '../pages/coc-generate.vue'
import DocumentsHistoryPage from '../pages/DocumentsHistory.vue'
import DtrMonitoringPage from '../pages/dtr.vue'
import { clearSession, isHRAdmin, isIntern } from './services/api'

/* Lahat ng page na kayang buksan ng app. Kailangan ding nakasaad
   sa assets/json/menu.json at ALLOWED_PAGES (BaseLayout.vue). */
const APP_PAGES = [
  'requests.html',
  'time-adjustments.html',
  'applicants.html',
  'dashboard.html',
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
  'account.html',
  'contact.html',
  'intern-dtr.html',
  'intern-attendance.html',
  'intern-documents.html'
]

const pageComponents = {
  'login.html': LoginPage,
  'dashboard.html': DashboardPage,
  'requests.html': RequestsPage,
  'time-adjustments.html': TimeAdjustmentsPage,
  'applicants.html': ApplicantsPage,
  'schools.html': SchoolsPage,
  'account.html': AccountPage,
  'internlist.html': ModulePage,
  'forms.html': ModulePage,
  'dtr.html': DtrMonitoringPage,
  'attendance.html': AttendancePage,
  'logs.html': LogsPage,
  'idgenerate.html': IdGeneratePage,
  'coc-generate.html': CocGeneratePage,
  'documents-history.html': DocumentsHistoryPage,
  'utility.html': ModulePage,
  'signatories.html': ModulePage,
  'contact.html': ModulePage,
  'intern-dtr.html': InternDtrPage,
  'intern-attendance.html': InternAttendancePage,
  'intern-documents.html': InternDocumentsPage,
  'forbidden.html': ForbiddenPage
}

const INTERN_PAGES = [
  'dashboard.html',
  'account.html',
  'intern-dtr.html',
  'intern-attendance.html',
  'intern-documents.html'
]

function isLoggedIn() {
  return Boolean(localStorage.getItem('authToken'))
}

/* Friendly deep links (hal. "/requests/documents-history") papunta sa
   flat na page name na ginagamit ng buong app. */
const PAGE_ALIASES = {
  'documents-history': 'documents-history.html'
}

/* Ang lahat ng current view ay para sa HR Admin lamang. */
function resolvePage() {
  const rawPage = window.location.pathname.split('/').pop()?.toLowerCase() || ''
  const page = PAGE_ALIASES[rawPage] || rawPage

  if (page === 'login' || page === 'login.html' || page === 'index.html' || page === '') {
    return 'login.html'
  }

  // Walang session? Balik sa login kahit may direktang URL.
  if (!isLoggedIn()) {
    return 'login.html'
  }

  if (isIntern()) {
    return INTERN_PAGES.includes(page) ? page : 'forbidden.html'
  }

  if (!isHRAdmin()) {
    return 'forbidden.html'
  }

  if (page in pageComponents) {
    return page
  }

  return 'dashboard.html'
}

const currentPage = ref(resolvePage())

function changePage(page) {
  // "Logout" ay nagta-tawag ng nito.
  if (page === 'login.html') {
    clearSession()
  }

  currentPage.value = page === 'login.html' ? resolvePage() : resolvePage()
}

onMounted(() => {
  window.addEventListener('popstate', () => {
    currentPage.value = resolvePage()
  })
})
</script>

<template>
  <component v-if="currentPage === 'login.html'" :is="pageComponents[currentPage]" />
  <BaseLayout v-else :current-page="currentPage" @navigate="changePage">
    <component
      :is="currentPage === 'dashboard.html' && isIntern() ? InternDashboardPage : pageComponents[currentPage]"
      :module-page="currentPage"
    />
  </BaseLayout>
</template>
