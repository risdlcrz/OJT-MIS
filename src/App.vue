<script setup>
import { onMounted, ref } from 'vue'
import BaseLayout from './components/BaseLayout.vue'
import DashboardPage from '../pages/dashboard.vue'
import RequestsPage from '../pages/requests.vue'
import ApplicantsPage from '../pages/applicants.vue'
import InternListPage from '../pages/InternList.vue'
import ScheduleOrientationPage from '../pages/ScheduleOrientation.vue'
import CocPage from '../pages/COC.vue'
import DtrPage from '../pages/DTR.vue'
import IdGeneratePage from '../pages/IDGenerate.vue'
import UtilityPage from '../pages/Utility.vue'
import ProgramsPage from '../pages/Programs.vue'
import SchoolsPage from '../pages/Schools.vue'
import SignatoriesPage from '../pages/Signatories.vue'
import LoginPage from '../pages/login.vue'

function getCurrentPage() {
  const page = window.location.pathname.split('/').pop()?.toLowerCase()
  if (page === 'forms.aspx') return 'coc.html'
  if (page === 'dtr.aspx') return 'dtr.html'
  if (page === 'idgenerate.aspx') return 'id-generate.html'
  if (page === 'utility.aspx') return 'utility.html'
  if (page === 'programslist.aspx') return 'programs.html'
  if (page === 'schools.aspx') return 'schools.html'
  if (page === 'signatories.aspx') return 'signatories.html'
  return ['login.html', 'requests.html', 'applicants.html', 'intern-list.html', 'schedule-orientation.html', 'coc.html', 'dtr.html', 'id-generate.html', 'utility.html', 'programs.html', 'schools.html', 'signatories.html'].includes(page) ? page : 'dashboard.html'
}

const currentPage = ref(getCurrentPage())
const pageComponents = {
  'login.html': LoginPage,
  'dashboard.html': DashboardPage,
  'requests.html': RequestsPage,
  'applicants.html': ApplicantsPage,
  'intern-list.html': InternListPage,
  'schedule-orientation.html': ScheduleOrientationPage,
  'coc.html': CocPage,
  'dtr.html': DtrPage,
  'id-generate.html': IdGeneratePage,
  'utility.html': UtilityPage,
  'programs.html': ProgramsPage,
  'schools.html': SchoolsPage,
  'signatories.html': SignatoriesPage
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
