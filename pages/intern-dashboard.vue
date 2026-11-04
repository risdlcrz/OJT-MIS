<template>
  <section class="content">
    <div v-if="isLoading" class="text-center my-5">
      <div class="spinner-border text-primary" role="status"></div>
    </div>
    <div v-else-if="error" class="alert alert-danger m-3">
      {{ error }}
      <button type="button" class="btn btn-sm btn-outline-danger ms-2" @click="load">Retry</button>
    </div>
    <div v-else>
      <h3 class="mb-3">Welcome, {{ intern.fullName }}</h3>
      <div class="row g-3 mb-3">
        <div class="col-md-4">
          <div class="card shadow-sm h-100">
            <div class="card-body">
              <div class="text-muted small">Hours rendered</div>
              <div class="fs-3 fw-semibold">{{ hoursRendered }} / {{ intern.requiredHours || 0 }}</div>
              <div class="text-muted small">{{ remainingHours }} hours remaining</div>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="card shadow-sm h-100">
            <div class="card-body">
              <div class="text-muted small">Today</div>
              <div class="fw-semibold">{{ todayLabel }}</div>
              <div class="text-muted small">{{ intern.applicantOffice || intern.program || '—' }}</div>
            </div>
          </div>
        </div>
        <div class="col-md-4">
          <div class="card shadow-sm h-100">
            <div class="card-body">
              <div class="text-muted small">Internship</div>
              <div class="fw-semibold">{{ formatDate(intern.startDate) }} – {{ formatDate(intern.endDate) }}</div>
              <div class="text-muted small">{{ intern.schoolName || intern.email }}</div>
            </div>
          </div>
        </div>
      </div>
      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-primary" @click="go('intern-dtr.html')">Scan ID for DTR</button>
        <button type="button" class="btn btn-outline-secondary" @click="go('intern-attendance.html')">View attendance</button>
        <button type="button" class="btn btn-outline-secondary" @click="go('intern-documents.html')">Request documents</button>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const intern = ref({})
const hoursRendered = ref(0)
const remainingHours = ref(0)
const today = ref(null)
const isLoading = ref(true)
const error = ref('')

const todayLabel = computed(() => {
  if (!today.value) return 'No time in yet'
  const inn = formatTime(today.value.timeIn)
  const out = today.value.timeOut ? formatTime(today.value.timeOut) : 'still in'
  return `${inn} – ${out}`
})

function go(page) {
  window.history.pushState({}, '', `/${page}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : '—'
}

function formatTime(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : '—'
}

async function load() {
  isLoading.value = true
  error.value = ''
  try {
    const data = await internPortalAPI.getPortal()
    intern.value = data.intern || {}
    hoursRendered.value = data.hoursRendered || 0
    remainingHours.value = data.remainingHours || 0
    today.value = data.today
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load intern dashboard.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
