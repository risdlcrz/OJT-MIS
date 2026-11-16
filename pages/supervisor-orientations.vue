<template>
  <section class="content">
    <div class="ojt-stat-grid">
      <div class="ojt-stat">
        <span class="ojt-stat-label">Upcoming orientations</span>
        <span class="ojt-stat-value">{{ upcomingOrientations.length }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Awaiting confirmation</span>
        <span class="ojt-stat-value">{{ unconfirmedCount }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Starting in 30 days</span>
        <span class="ojt-stat-value">{{ startingSoonCount }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Incoming applicants</span>
        <span class="ojt-stat-value">{{ incoming.length }}</span>
      </div>
    </div>

    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Orientation &amp; Start Dates</h3>
          <p class="ojt-table-subtitle">
            Track who is joining <strong>{{ department || 'your department' }}</strong> and when, to plan office capacity.
          </p>
        </div>
        <div class="ojt-table-tools">
          <div class="ojt-segment" role="tablist" aria-label="View">
            <button type="button" role="tab" :class="{ active: view === 'schedule' }" :aria-selected="view === 'schedule'" @click="view = 'schedule'">
              Orientation Schedule
            </button>
            <button type="button" role="tab" :class="{ active: view === 'incoming' }" :aria-selected="view === 'incoming'" @click="view = 'incoming'">
              Incoming Roster
            </button>
          </div>
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="load">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
        </div>
      </div>

      <div v-if="scopeError || error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ scopeError || error }}</span>
        <button v-if="!scopeError" type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="search" type="search" class="form-control form-control-sm"
                 placeholder="Search name, school, program" aria-label="Search" />
        </div>
        <div v-if="view === 'schedule'" class="form-check form-switch mb-0">
          <input id="upcomingOnly" v-model="upcomingOnly" class="form-check-input" type="checkbox" role="switch" />
          <label for="upcomingOnly" class="form-check-label small">Upcoming only</label>
        </div>
        <span class="ojt-toolbar-count">{{ view === 'schedule' ? scheduleRows.length : incomingRows.length }} applicant(s)</span>
      </div>

      <!-- Orientation schedule -->
      <div v-if="view === 'schedule'" class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th class="col-date">Date</th>
              <th>Time</th>
              <th>Applicant</th>
              <th>Program</th>
              <th>Venue</th>
              <th>Attendance</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="6" :loading="isLoading" :empty="!isLoading && scheduleRows.length === 0"
                        icon="far fa-calendar" title="No orientations scheduled"
                        :text="upcomingOnly ? 'Turn off “Upcoming only” to see past sessions.' : 'HR has not scheduled an orientation for your department yet.'" />
            <template v-if="!isLoading">
              <tr v-for="row in scheduleRows" :key="row.id">
                <td class="col-date">
                  <span class="ojt-cell-primary">{{ formatDisplayDate(row.date) }}</span>
                  <span class="ojt-cell-secondary">{{ relativeDay(row.date) }}</span>
                </td>
                <td>
                  <span v-if="row.time">{{ formatClock(row.time) }}</span>
                  <span v-else class="ojt-na">Time TBA</span>
                </td>
                <td>
                  <span class="ojt-cell-primary">{{ row.fullName }}</span>
                  <span class="ojt-cell-secondary">{{ row.schoolName || 'School not set' }}</span>
                </td>
                <td>
                  <span v-if="row.program">{{ row.program }}</span>
                  <span v-else class="ojt-na">Not set</span>
                </td>
                <td>
                  <span v-if="row.office">{{ row.office }}</span>
                  <span v-else class="ojt-na">Venue TBA</span>
                </td>
                <td>
                  <StatusPill :status="row.confirmed ? 'Confirmed' : 'Awaiting confirmation'" />
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>

      <!-- Incoming roster -->
      <div v-else class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Applicant</th>
              <th>School &amp; Program</th>
              <th>Stage</th>
              <th class="col-date">Orientation</th>
              <th class="col-date">Official Start</th>
              <th class="col-date">Expected End</th>
              <th class="col-num">Req. Hours</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="7" :loading="isLoading" :empty="!isLoading && incomingRows.length === 0"
                        icon="fas fa-user-plus" title="No incoming interns"
                        text="Accepted applicants and newly hired interns who have not started yet will appear here." />
            <template v-if="!isLoading">
              <tr v-for="a in incomingRows" :key="a.id">
                <td>
                  <span class="ojt-cell-primary">{{ a.fullName }}</span>
                  <span class="ojt-cell-secondary">{{ a.email || 'No email on file' }}</span>
                </td>
                <td>
                  <span v-if="a.schoolName" class="d-block">{{ a.schoolName }}</span>
                  <span v-else class="ojt-na d-block">School not set</span>
                  <span class="ojt-cell-secondary">{{ a.program || 'Program not set' }}</span>
                </td>
                <td><StatusPill :status="a.hired ? 'Hired' : 'Accepted'" :label="a.hired ? 'Hired' : 'Accepted'" :tone="a.hired ? 'success' : 'info'" /></td>
                <td class="col-date">
                  <span v-if="a.orientation?.date">{{ formatDisplayDate(a.orientation.date) }}</span>
                  <span v-else class="ojt-na">Not scheduled</span>
                </td>
                <td class="col-date">
                  <template v-if="a.startDate">
                    <span class="ojt-cell-primary">{{ formatDisplayDate(a.startDate) }}</span>
                    <span class="ojt-cell-secondary">{{ relativeDay(a.startDate) }}</span>
                  </template>
                  <span v-else class="ojt-na">To be set</span>
                </td>
                <td class="col-date">
                  <span v-if="a.endDate">{{ formatDisplayDate(a.endDate) }}</span>
                  <span v-else class="ojt-na">To be set</span>
                </td>
                <td class="col-num">{{ a.requiredHours || 0 }}</td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { getApiErrorMessage, supervisorAPI } from '@/services/api'
import { formatClock, formatDisplayDate } from '@/utils/datetime'
import { daysUntil, relativeDay, useDepartment } from '@/utils/supervisor'

const { department, scopeError, loadDepartment } = useDepartment()

const view = ref(window.location.hash === '#incoming' ? 'incoming' : 'schedule')
const orientations = ref([])
const applicants = ref([])
const search = ref('')
const upcomingOnly = ref(true)
const isLoading = ref(true)
const error = ref('')

function matches(row) {
  const term = search.value.trim().toLowerCase()
  if (!term) return true
  return [row.fullName, row.schoolName, row.program, row.email]
    .some((v) => String(v || '').toLowerCase().includes(term))
}

const upcomingOrientations = computed(() => orientations.value.filter((o) => (daysUntil(o.date) ?? -1) >= 0))
const unconfirmedCount = computed(() => upcomingOrientations.value.filter((o) => !o.confirmed).length)

const scheduleRows = computed(() =>
  (upcomingOnly.value ? upcomingOrientations.value : orientations.value).filter(matches))

/* Incoming: accepted applicants, plus hired ones whose start date has not
   passed yet (or is still unset). Rejected applicants are excluded. */
const incoming = computed(() => applicants.value.filter((a) => {
  if (a.rejected) return false
  if (a.hired) return !a.startDate || (daysUntil(a.startDate) ?? -1) >= 0
  return a.accepted
}))

const startingSoonCount = computed(() => incoming.value.filter((a) => {
  const d = daysUntil(a.startDate)
  return d !== null && d >= 0 && d <= 30
}).length)

const incomingRows = computed(() =>
  incoming.value.filter(matches).sort((a, b) =>
    String(a.startDate || '9999').localeCompare(String(b.startDate || '9999'))))

async function load() {
  isLoading.value = true
  error.value = ''
  await loadDepartment()
  if (scopeError.value) {
    isLoading.value = false
    return
  }
  try {
    const [o, a] = await Promise.all([supervisorAPI.orientations(), supervisorAPI.applicants()])
    orientations.value = Array.isArray(o) ? o : []
    applicants.value = Array.isArray(a) ? a : []
  } catch (e) {
    orientations.value = []
    applicants.value = []
    error.value = getApiErrorMessage(e, 'Unable to load orientation schedules.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>

