<script setup>
import { onMounted, ref } from 'vue'
import BaseLayout from './components/BaseLayout.vue'
import LoginPage from '../pages/login.vue'
import DashboardPage from '../pages/dashboard.vue'
import RequestsPage from '../pages/requests.vue'
import ApplicantsPage from '../pages/applicants.vue'
import SchoolsPage from '../pages/Schools.vue'
import ModulePage from '../pages/ModulePage.vue'

/* Lahat ng page na kayang buksan ng app. Kailangan ding nakasaad
   sa assets/json/menu.json at ALLOWED_PAGES (BaseLayout.vue). */
const APP_PAGES = [
  'requests.html',
  'applicants.html',
  'dashboard.html',
  'schools.html',
  'internlist.html',
  'forms.html',
  'dtr.html',
  'idgenerate.html',
  'utility.html',
  'programs.html',
  'signatories.html'
]

const pageComponents = {
  'login.html': LoginPage,
  'dashboard.html': DashboardPage,
  'requests.html': RequestsPage,
  'applicants.html': ApplicantsPage,
  'schools.html': SchoolsPage,
  'internlist.html': ModulePage,
  'forms.html': ModulePage,
  'dtr.html': ModulePage,
  'idgenerate.html': ModulePage,
  'utility.html': ModulePage,
  'programs.html': ModulePage,
  'signatories.html': ModulePage
}

function isLoggedIn() {
  return Boolean(localStorage.getItem('authToken'))
}

function getCurrentPage() {
  const page = window.location.pathname.split('/').pop()?.toLowerCase() || ''

  if (page === 'login' || page === 'login.html' || page === 'index.html' || page === '') {
    return 'login.html'
  }

  // Walang session? Balik sa login kahit may direktang URL.
  if (!isLoggedIn()) {
    return 'login.html'
  }

  if (page in pageComponents) {
    return page
  }

  // Naka-login pero hindi kilalang page: huwag na mag-login ulit.
  return 'dashboard.html'
}

const currentPage = ref(getCurrentPage())

function changePage(page) {
  // "Logout" ay nagta-tawag ng nito.
  if (page === 'login.html') {
    localStorage.removeItem('authToken')
    localStorage.removeItem('ojtUser')
  }

  currentPage.value = page
}

onMounted(() => {
  window.addEventListener('popstate', () => {
    currentPage.value = getCurrentPage()
  })
})
</script>

<template>
  <component v-if="currentPage === 'login.html'" :is="pageComponents[currentPage]" />
  <BaseLayout v-else :current-page="currentPage" @navigate="changePage">
    <component :is="pageComponents[currentPage]" />
  </BaseLayout>
</template>
