<script setup>
import { onMounted, ref } from 'vue'
import BaseLayout from './components/BaseLayout.vue'
import LoginPage from '../pages/login.vue'
import DashboardPage from '../pages/dashboard.vue'
import RequestsPage from '../pages/requests.vue'
import ApplicantsPage from '../pages/applicants.vue'

function getCurrentPage() {
  const page = window.location.pathname.split('/').pop()?.toLowerCase()
  if (page === 'login' || page === 'login.html') return 'login.html'
  return ['requests.html', 'applicants.html', 'dashboard.html'].includes(page)
    ? page
    : 'login.html'
}

const currentPage = ref(getCurrentPage())
const pageComponents = {
  'login.html': LoginPage,
  'dashboard.html': DashboardPage,
  'requests.html': RequestsPage,
  'applicants.html': ApplicantsPage
}

function changePage(page) {
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
