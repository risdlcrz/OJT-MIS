<template>
  <section class="content">
    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Daily Time Record</h3>
          <p class="ojt-table-subtitle">
            <template v-if="selectedIntern">
              Showing <strong>{{ selectedIntern.fullName }}</strong>
              &middot; {{ totalHours }} hrs in this range
            </template>
            <template v-else>Attendance of interns in <strong>{{ department || 'your department' }}</strong>.</template>
          </p>
        </div>
        <div class="ojt-table-tools">
          <button type="button" class="btn btn-outline-secondary btn-sm" @click="go('/supervisor-interns.html')">
            <i class="fas fa-arrow-left me-1"></i> Intern List
          </button>
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="loadRecords">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
        </div>
      </div>

      <div v-if="scopeError || error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ scopeError || error }}</span>
        <button v-if="!scopeError" type="button" class="btn btn-sm btn-outline-danger" @click="loadRecords">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <select v-model="internId" class="form-select form-select-sm" aria-label="Intern" @change="onInternChange">
          <option value="">All interns</option>
          <option v-for="i in interns" :key="i.id" :value="String(i.id)">{{ i.fullName }}</option>
        </select>
        <div class="d-flex align-items-center gap-1">
          <label for="dtrFrom" class="small text-muted">From</label>
          <input id="dtrFrom" v-model="from" type="date" class="form-control form-control-sm" @change="loadRecords" />
        </div>
        <div class="d-flex align-items-center gap-1">
          <label for="dtrTo" class="small text-muted">To</label>
          <input id="dtrTo" v-model="to" type="date" class="form-control form-control-sm" @change="loadRecords" />
        </div>
        <button v-if="from || to" type="button" class="btn btn-link btn-sm p-0" @click="clearDates">Clear dates</button>
        <span class="ojt-toolbar-count">{{ rows.length }} day(s)</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th v-if="!internId">Intern</th>
              <th class="col-date">Date</th>
              <th class="col-center">AM In</th>
              <th class="col-center">AM Out</th>
              <th class="col-center">PM In</th>
              <th class="col-center">PM Out</th>
              <th class="col-num">Hours</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="internId ? 7 : 8" :loading="isLoading" :empty="!isLoading && rows.length === 0"
                        icon="far fa-clock" title="No attendance records"
                        :text="from || to ? 'No punches in the selected date range.' : 'Time records appear once interns start logging attendance.'" />
            <template v-if="!isLoading">
              <tr v-for="row in rows" :key="row.key">
                <td v-if="!internId"><span class="ojt-cell-primary">{{ row.name }}</span></td>
                <td class="col-date">{{ formatDisplayDate(row.date) }}</td>
                <td class="col-center"><Punch :value="row.morning?.timeIn" /></td>
                <td class="col-center"><Punch :value="row.morning?.timeOut" :open="!!row.morning?.timeIn" /></td>
                <td class="col-center"><Punch :value="row.afternoon?.timeIn" /></td>
                <td class="col-center"><Punch :value="row.afternoon?.timeOut" :open="!!row.afternoon?.timeIn" /></td>
                <td class="col-num">{{ row.hours > 0 ? row.hours.toFixed(2) : '0.00' }}</td>
                <td><StatusPill :status="row.status" /></td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, h, onBeforeUnmount, onMounted, ref } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { replaceUrl } from '@/utils/navigation'
import { getApiErrorMessage, supervisorAPI } from '@/services/api'
import { dateKey, formatClock, formatDisplayDate } from '@/utils/datetime'
import { go, useDepartment } from '@/utils/supervisor'

/* A punch cell: the clock time, "Missing" when the shift has a time in but
   no time out, otherwise a quiet "No punch". */
const Punch = (props) => {
  if (props.value) return h('span', { class: 'font-monospace' }, formatClock(props.value))
  return h('span', { class: 'ojt-na' }, props.open ? 'Missing' : 'No punch')
}
Punch.props = ['value', 'open']

const { department, scopeError, loadDepartment } = useDepartment()

const interns = ref([])
const records = ref([])
const internId = ref(routeInternId())
const from = ref('')
const to = ref('')
const isLoading = ref(true)
const error = ref('')

function routeInternId() {
  const id = Number(new URLSearchParams(window.location.search).get('internId'))
  return Number.isFinite(id) && id > 0 ? String(id) : ''
}

const selectedIntern = computed(() => interns.value.find((i) => String(i.id) === internId.value) || null)

const rows = computed(() => {
  const map = new Map()
  for (const r of records.value) {
    const key = `${r.internId}|${dateKey(r.date)}`
    if (!map.has(key)) {
      map.set(key, { key, name: r.internName || 'Intern', date: r.date, morning: null, afternoon: null, hours: 0, late: false })
    }
    const row = map.get(key)
    const slot = String(r.shiftType || '').toLowerCase() === 'afternoon' ? 'afternoon' : 'morning'
    row[slot] = r
    row.hours += Number(r.hours) || 0
    if (String(r.status || '').toLowerCase() === 'late') row.late = true
  }
  return [...map.values()]
    .map((row) => ({ ...row, hours: Math.round(row.hours * 100) / 100, status: row.late ? 'Late' : 'On Time' }))
    .sort((a, b) => dateKey(b.date).localeCompare(dateKey(a.date)) || a.name.localeCompare(b.name))
})

const totalHours = computed(() => rows.value.reduce((sum, r) => sum + r.hours, 0).toFixed(2))

function onInternChange() {
  const url = internId.value ? `/supervisor-dtr.html?internId=${internId.value}` : '/supervisor-dtr.html'
  replaceUrl(url)
  loadRecords()
}

function clearDates() {
  from.value = ''
  to.value = ''
  loadRecords()
}

async function loadRecords() {
  if (scopeError.value) return
  isLoading.value = true
  error.value = ''
  try {
    const params = { take: 1000 }
    if (internId.value) params.internId = internId.value
    if (from.value) params.from = from.value
    if (to.value) params.to = to.value
    const data = await supervisorAPI.dtr(params)
    records.value = Array.isArray(data) ? data : []
  } catch (e) {
    records.value = []
    error.value = getApiErrorMessage(e, 'Unable to load time records.')
  } finally {
    isLoading.value = false
  }
}

/* A notification deep link (?internId=) while this page is already open. */
function onRouteChange() {
  const id = routeInternId()
  if (id === internId.value) return
  internId.value = id
  loadRecords()
}

onBeforeUnmount(() => {
  window.removeEventListener('hashchange', onRouteChange)
  window.removeEventListener('popstate', onRouteChange)
})

onMounted(async () => {
  window.addEventListener('hashchange', onRouteChange)
  window.addEventListener('popstate', onRouteChange)
  await loadDepartment()
  if (scopeError.value) {
    isLoading.value = false
    return
  }
  try {
    interns.value = await supervisorAPI.interns()
  } catch {
    interns.value = []
  }
  await loadRecords()
})
</script>
