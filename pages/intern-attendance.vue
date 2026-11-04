<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header">
        <h3 class="card-title mb-0">Attendance</h3>
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
              <th>Date</th>
              <th>Time In</th>
              <th>Time Out</th>
              <th>Hours</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="rows.length === 0">
              <td colspan="5" class="text-center text-muted py-4">No attendance records yet.</td>
            </tr>
            <tr v-for="row in rows" :key="row.id">
              <td>{{ formatDate(row.date) }}</td>
              <td>{{ formatTime(row.timeIn) }}</td>
              <td>{{ row.timeOut ? formatTime(row.timeOut) : '—' }}</td>
              <td>{{ hours(row) }}</td>
              <td>{{ row.status || '—' }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const rows = ref([])
const isLoading = ref(true)
const error = ref('')

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

function hours(row) {
  if (!row.timeOut) return '—'
  const diff = (new Date(row.timeOut) - new Date(row.timeIn)) / 36e5
  return Number.isFinite(diff) ? diff.toFixed(2) : '—'
}

async function load() {
  isLoading.value = true
  error.value = ''
  try {
    const data = await internPortalAPI.getPortal()
    rows.value = data.attendance || []
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load attendance.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
