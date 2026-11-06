<template>
  <section class="content dashboard-page" :style="{ '--scroll': scrollY }">
    <div class="row">
      <div class="col-md-12">
        <div v-if="isLoading" class="text-center py-5">
          <div class="spinner-border text-warning" role="status"></div>
        </div>
        <div v-else-if="error" class="alert alert-danger m-3">
          {{ error }}
          <button type="button" class="btn btn-sm btn-outline-danger ms-2" @click="load">Retry</button>
        </div>
        <template v-else>
          <div class="ojt-hero">
            <div class="ojt-hero-inner">
              <p class="ojt-hero-kicker">Intern dashboard</p>
              <h1 class="ojt-hero-title ojt-hero-title-name">{{ intern.fullName || 'Intern' }}</h1>
              <div class="ojt-hero-actions">
                <a class="ojt-btn ojt-btn-primary" href="/intern-dtr.html" @click.prevent="go('intern-dtr.html')">
                  Scan QR for DTR
                </a>
                <a class="ojt-btn ojt-btn-ghost" href="/intern-attendance.html" @click.prevent="go('intern-attendance.html')">
                  View attendance
                </a>
                <a class="ojt-btn ojt-btn-ghost" href="/intern-documents.html" @click.prevent="go('intern-documents.html')">
                  Request documents
                </a>
              </div>
            </div>

            <button type="button" class="ojt-scroll-cue" aria-label="Scroll to status" @click="scrollToSummary">
              <span></span>
              <span></span>
            </button>
          </div>

          <section ref="summaryEl" class="ojt-summary">
            <h2 class="ojt-section-title">Status</h2>
            <div class="ojt-stats">
              <article class="stat-card stat-orange reveal reveal-in">
                <div>
                  <span class="stat-label">Total Hours Rendered</span>
                  <span class="stat-number">{{ hoursRendered }}</span>
                </div>
                <i class="fas fa-clock"></i>
              </article>

              <article class="stat-card stat-blue reveal reveal-in">
                <div>
                  <span class="stat-label">Remaining Hours</span>
                  <span class="stat-number">{{ remainingHours }}</span>
                </div>
                <i class="fas fa-hourglass-half"></i>
              </article>

              <article class="stat-card stat-purple reveal reveal-in">
                <div>
                  <span class="stat-label">Attendance Status</span>
                  <span class="stat-number stat-number-text">{{ attendanceStatus }}</span>
                </div>
                <i class="fas fa-user-check"></i>
              </article>
            </div>

            <div class="ojt-info-grid">
              <article class="ojt-info reveal reveal-in">
                <h3>Today</h3>
                <p class="ojt-info-main">{{ todayLabel }}</p>
                <p class="ojt-info-meta">{{ intern.applicantOffice || intern.program || 'Internship' }}</p>
              </article>
              <article class="ojt-info reveal reveal-in">
                <h3>Required hours</h3>
                <p class="ojt-info-main">{{ intern.requiredHours || 0 }} hours</p>
                <div class="progress mt-2" style="height: 8px;">
                  <div class="progress-bar" role="progressbar" :style="{ width: progress + '%' }"></div>
                </div>
              </article>
              <article class="ojt-info reveal reveal-in">
                <h3>Internship</h3>
                <p class="ojt-info-main">{{ formatDate(intern.startDate) }} – {{ formatDate(intern.endDate) }}</p>
                <p class="ojt-info-meta">{{ intern.schoolName || intern.email || '—' }}</p>
              </article>
            </div>
          </section>
        </template>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'
import '../assets/css/Dashboard.css'

const intern = ref({})
const hoursRendered = ref(0)
const remainingHours = ref(0)
const today = ref(null)
const clockedIn = ref(false)
const isLoading = ref(true)
const error = ref('')
const scrollY = ref(0)
const summaryEl = ref(null)
let raf = 0

const progress = computed(() => {
  const required = Number(intern.value.requiredHours) || 0
  if (required <= 0) return 0
  return Math.min(100, Math.round((Number(hoursRendered.value) / required) * 100))
})

const attendanceStatus = computed(() => {
  if (clockedIn.value) return 'Timed in'
  if (today.value?.status) return today.value.status
  return 'No time in'
})

function shiftSpan(shift) {
  if (!shift?.timeIn) return '—'
  return `${formatTime(shift.timeIn)}–${shift.timeOut ? formatTime(shift.timeOut) : 'in'}`
}

const todayLabel = computed(() => {
  if (!today.value) return 'No time in yet'
  return `AM ${shiftSpan(today.value.morning)} · PM ${shiftSpan(today.value.afternoon)}`
})

function onScroll() {
  if (raf) return
  raf = requestAnimationFrame(() => {
    scrollY.value = window.scrollY
    raf = 0
  })
}

function scrollToSummary() {
  summaryEl.value?.scrollIntoView({ behavior: 'smooth', block: 'start' })
}

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
    clockedIn.value = Boolean(data.clockedIn)
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load intern dashboard.')
  } finally {
    isLoading.value = false
  }
}

function onDtrUpdated(event) {
  const detail = event.detail || {}
  if (detail.today) today.value = detail.today
  if (detail.hoursRendered != null) hoursRendered.value = detail.hoursRendered
  if (detail.remainingHours != null) remainingHours.value = detail.remainingHours
  clockedIn.value = Boolean(detail.clockedIn)
}

onMounted(() => {
  load()
  window.addEventListener('scroll', onScroll, { passive: true })
  window.addEventListener('ojt:dtr-updated', onDtrUpdated)
})

onBeforeUnmount(() => {
  window.removeEventListener('scroll', onScroll)
  window.removeEventListener('ojt:dtr-updated', onDtrUpdated)
})
</script>

<style scoped>
.ojt-hero-kicker {
  margin: 0;
  color: #ffe7c2;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.18em;
  text-transform: uppercase;
}

.ojt-hero-title-name {
  font-size: clamp(2.4rem, 7vw, 5.5rem);
  max-width: 16ch;
}

.stat-number-text {
  font-size: 1.35rem;
  line-height: 1.2;
}
</style>
