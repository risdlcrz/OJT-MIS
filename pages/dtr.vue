<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <div>
          <h3 class="card-title mb-0">Daily Time Record</h3>
          <p class="text-muted small mb-0">Same day view as the intern record, with the intern name on every row.</p>
        </div>
        <div class="input-group input-group-sm" style="width: min(100%, 280px)">
          <span class="input-group-text"><i class="fas fa-search"></i></span>
          <input
            v-model="search"
            type="search"
            class="form-control"
            placeholder="Search intern name"
            aria-label="Search intern name"
          />
        </div>
      </div>

      <div v-if="isLoading" class="card-body text-center">
        <div class="spinner-border text-primary" role="status"></div>
      </div>
      <div v-else-if="error" class="alert alert-danger m-3">
        {{ error }}
        <button type="button" class="btn btn-sm btn-outline-danger ms-2" @click="load">Retry</button>
      </div>
      <div v-else class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light">
            <tr>
              <th>Intern Name</th>
              <th>Date</th>
              <th>AM Time In</th>
              <th>AM Time Out</th>
              <th>PM Time In</th>
              <th>PM Time Out</th>
              <th>Total Hours</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="rows.length === 0">
              <td colspan="8" class="text-center text-muted py-4">No attendance records yet.</td>
            </tr>
            <tr v-for="row in rows" :key="row.id">
              <td class="text-dark fw-semibold">{{ row.name }}</td>
              <td>{{ formatDisplayDate(row.date) }}</td>
              <td>{{ formatClock(row.morning?.timeIn) }}</td>
              <td>{{ formatClock(row.morning?.timeOut) }}</td>
              <td>{{ formatClock(row.afternoon?.timeIn) }}</td>
              <td>{{ formatClock(row.afternoon?.timeOut) }}</td>
              <td>{{ formatHours(row.hours) }}</td>
              <td>
                <span class="badge" :class="row.status === 'Late' ? 'bg-warning text-dark' : 'bg-success'">
                  {{ row.status || '—' }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { dtrAPI, getApiErrorMessage } from '@/services/api'
import { dateKey, formatClock, formatDisplayDate } from '@/utils/datetime'

const records = ref([])
const search = ref('')
const isLoading = ref(true)
const error = ref('')

const rows = computed(() => {
  const query = search.value.trim().toLowerCase()
  const grouped = groupByInternDay(records.value)
  if (!query) return grouped
  return grouped.filter((row) => row.name.toLowerCase().includes(query))
})

function shiftHours(record) {
  if (!record?.timeIn || !record?.timeOut) return 0
  const start = clockMinutes(record.timeIn)
  const end = clockMinutes(record.timeOut)
  if (start == null || end == null || end < start) return 0
  return (end - start) / 60
}

function clockMinutes(value) {
  const text = String(value)
  const match = text.match(/(?:T|\s|^)(\d{1,2}):(\d{2})/)
  if (!match) return null
  return Number(match[1]) * 60 + Number(match[2])
}

function groupByInternDay(list) {
  const map = new Map()
  for (const record of list || []) {
    const key = `${record.internId ?? record.internName ?? 'unknown'}|${dateKey(record.date)}`
    if (!map.has(key)) {
      map.set(key, {
        id: key,
        name: record.internName || record.internEmail || 'Unknown intern',
        date: record.date,
        morning: null,
        afternoon: null
      })
    }
    const row = map.get(key)
    const slot = String(record.shiftType || '').toLowerCase() === 'afternoon' ? 'afternoon' : 'morning'
    const current = row[slot]
    const started = clockMinutes(record.timeIn) ?? -1
    const currentStarted = current ? (clockMinutes(current.timeIn) ?? -1) : -1
    if (!current || started >= currentStarted) row[slot] = record
    if (record.internName) row.name = record.internName
  }

  return [...map.values()]
    .map((row) => {
      const hours = shiftHours(row.morning) + shiftHours(row.afternoon)
      const morningIn = clockMinutes(row.morning?.timeIn)
      const late = morningIn != null && morningIn > 8 * 60
      return {
        ...row,
        hours: Math.round(hours * 100) / 100,
        status: late ? 'Late' : 'On Time'
      }
    })
    .sort((a, b) => {
      const byDate = dateKey(b.date).localeCompare(dateKey(a.date))
      if (byDate !== 0) return byDate
      return a.name.localeCompare(b.name)
    })
}

function formatHours(hours) {
  const value = Number(hours)
  return Number.isFinite(value) && value > 0 ? value.toFixed(2) : '—'
}

async function load() {
  isLoading.value = true
  error.value = ''
  try {
    const data = await dtrAPI.getAll()
    records.value = Array.isArray(data) ? data : []
  } catch (e) {
    records.value = []
    error.value = getApiErrorMessage(e, 'Unable to load daily time records.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
