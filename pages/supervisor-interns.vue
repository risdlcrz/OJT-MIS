<template>
  <section class="content">
    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Intern List</h3>
          <p class="ojt-table-subtitle">
            Interns assigned to <strong>{{ department || 'your department' }}</strong>. Select a row to open their Daily Time Record.
          </p>
        </div>
        <div class="ojt-table-tools">
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
                 placeholder="Search name, email, school" aria-label="Search interns" />
        </div>
        <div class="ojt-segment" role="tablist" aria-label="Filter by status">
          <button v-for="tab in tabs" :key="tab.value" type="button" role="tab"
                  :class="{ active: statusFilter === tab.value }" :aria-selected="statusFilter === tab.value"
                  @click="statusFilter = tab.value">
            {{ tab.label }}<span class="ojt-segment-count">{{ countFor(tab.value) }}</span>
          </button>
        </div>
        <span class="ojt-toolbar-count">{{ filtered.length }} of {{ interns.length }} intern(s)</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Intern</th>
              <th>School &amp; Program</th>
              <th>Hours Rendered</th>
              <th class="col-date">OJT Period</th>
              <th>Status</th>
              <th class="col-actions" aria-label="Open DTR"></th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="6" :loading="isLoading" :empty="!isLoading && filtered.length === 0"
                        icon="fas fa-user-graduate"
                        :title="interns.length ? 'No interns match your filters' : 'No interns assigned yet'"
                        :text="interns.length ? 'Try a different search or status.' : 'Interns hired for your department will appear here.'" />
            <template v-if="!isLoading">
              <tr v-for="intern in filtered" :key="intern.id" class="ojt-row-link" tabindex="0"
                  @click="openDtr(intern)" @keydown.enter="openDtr(intern)">
                <td>
                  <span class="ojt-cell-primary">{{ intern.fullName }}</span>
                  <span class="ojt-cell-secondary">{{ intern.email || 'No email on file' }}</span>
                </td>
                <td>
                  <span v-if="intern.schoolName" class="d-block">{{ intern.schoolName }}</span>
                  <span v-else class="ojt-na d-block">School not set</span>
                  <span class="ojt-cell-secondary">{{ intern.program || 'Program not set' }}</span>
                </td>
                <td>
                  <div class="ojt-progress" :title="`${intern.progressPercent}% complete`">
                    <div class="ojt-progress-track">
                      <div class="ojt-progress-fill" :style="{ width: `${intern.progressPercent || 0}%` }"></div>
                    </div>
                    <span class="ojt-progress-label">{{ hours(intern.hoursRendered) }} / {{ hours(intern.requiredHours) }}</span>
                  </div>
                </td>
                <td class="col-date">
                  <template v-if="intern.startDate || intern.endDate">
                    {{ dateOrBlank(intern.startDate) }} &ndash; {{ dateOrBlank(intern.endDate) }}
                  </template>
                  <span v-else class="ojt-na">Not scheduled</span>
                </td>
                <td><StatusPill :status="intern.status" /></td>
                <td class="col-actions">
                  <button type="button" class="ojt-icon-btn" title="View DTR" @click.stop="openDtr(intern)">
                    <i class="fas fa-chevron-right"></i>
                  </button>
                </td>
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
import { formatDisplayDate } from '@/utils/datetime'
import { go, useDepartment } from '@/utils/supervisor'

const { department, scopeError, loadDepartment } = useDepartment()

const interns = ref([])
const isLoading = ref(true)
const error = ref('')
const search = ref('')
const statusFilter = ref('all')

const tabs = [
  { value: 'all', label: 'All' },
  { value: 'Hired', label: 'Active' },
  { value: 'Completed', label: 'Completed' },
  { value: 'other', label: 'Other' }
]

function matchesStatus(intern, value) {
  if (value === 'all') return true
  if (value === 'other') return intern.status !== 'Hired' && intern.status !== 'Completed'
  return intern.status === value
}

function countFor(value) {
  return interns.value.filter((i) => matchesStatus(i, value)).length
}

const filtered = computed(() => {
  const term = search.value.trim().toLowerCase()
  return interns.value.filter((i) => {
    if (!matchesStatus(i, statusFilter.value)) return false
    if (!term) return true
    return [i.fullName, i.email, i.schoolName, i.program, i.applicantNo]
      .some((v) => String(v || '').toLowerCase().includes(term))
  })
})

function hours(value) {
  const n = Number(value)
  return Number.isFinite(n) ? (Math.round(n * 100) / 100).toString() : '0'
}

function dateOrBlank(value) {
  return value ? formatDisplayDate(value) : 'TBD'
}

function openDtr(intern) {
  go(`/supervisor-dtr.html?internId=${intern.id}`)
}

async function load() {
  isLoading.value = true
  error.value = ''
  await loadDepartment()
  if (scopeError.value) {
    interns.value = []
    isLoading.value = false
    return
  }
  try {
    const data = await supervisorAPI.interns()
    interns.value = Array.isArray(data) ? data : []
  } catch (e) {
    interns.value = []
    error.value = getApiErrorMessage(e, 'Unable to load interns.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
