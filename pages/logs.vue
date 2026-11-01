<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">Activity Logs</h3>
        <button type="button" class="btn btn-outline-secondary btn-sm" :disabled="isLoading" @click="loadLogs">
          <i class="fas fa-sync-alt me-1"></i> Refresh
        </button>
      </div>

      <div class="card-body">
        <div class="row g-2 mb-3">
          <div class="col-md-5">
            <input
              v-model.trim="search"
              type="search"
              class="form-control"
              placeholder="Search user, email, or action"
              aria-label="Search logs"
            />
          </div>
          <div class="col-md-7 d-flex flex-wrap align-items-center justify-content-md-end gap-1">
            <button
              v-for="tab in categoryTabs"
              :key="tab.value"
              type="button"
              class="btn btn-sm"
              :class="category === tab.value ? 'btn-primary' : 'btn-outline-secondary'"
              @click="category = tab.value"
            >
              {{ tab.label }}
              <span class="badge ms-1" :class="category === tab.value ? 'text-bg-light' : 'text-bg-secondary'">
                {{ countFor(tab.value) }}
              </span>
            </button>
          </div>
        </div>

        <div v-if="isLoading" class="text-center my-4">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="text-muted mt-2 mb-0">Loading activity logs...</p>
        </div>

        <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center">
          <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="loadLogs">Retry</button>
        </div>

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th scope="col" class="text-center" style="width: 70px;">#</th>
                <th scope="col" style="width: 170px;">Date &amp; Time</th>
                <th scope="col">User</th>
                <th scope="col" style="width: 120px;">Category</th>
                <th scope="col">Action</th>
                <th scope="col">Details</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(log, index) in filteredLogs" :key="log.id">
                <td class="text-center text-dark">{{ index + 1 }}</td>
                <td class="text-muted small">{{ formatDateTime(log.createdAt) }}</td>
                <td>
                  <div class="fw-semibold text-dark">{{ log.userName || '-' }}</div>
                  <div class="text-muted small">{{ log.email }}</div>
                </td>
                <td>
                  <span class="badge" :class="categoryBadgeClass(log.category)">{{ log.category }}</span>
                </td>
                <td class="text-dark">
                  {{ log.action }}
                </td>
                <td class="text-muted small">{{ log.details || '-' }}</td>
              </tr>
              <tr v-if="filteredLogs.length === 0">
                <td colspan="6" class="text-center text-muted py-4">No activity logs match your filters.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getApiErrorMessage, logsAPI } from '@/services/api'

const logs = ref([])
const category = ref('all')
const search = ref('')
const isLoading = ref(false)
const loadError = ref('')

const categoryTabs = [
  { value: 'all', label: 'All' },
  { value: 'HR', label: 'HR' },
  { value: 'Supervisor', label: 'Supervisor' },
  { value: 'Intern', label: 'Intern' }
]

const filteredLogs = computed(() => {
  const term = search.value.toLowerCase()
  return logs.value.filter((log) => {
    const categoryOk = category.value === 'all' || log.category === category.value
    const searchOk =
      !term ||
      [log.userName, log.email, log.action, log.details]
        .join(' ')
        .toLowerCase()
        .includes(term)
    return categoryOk && searchOk
  })
})

function countFor(value) {
  return value === 'all' ? logs.value.length : logs.value.filter((log) => log.category === value).length
}

function categoryBadgeClass(value) {
  if (value === 'HR') return 'text-bg-warning'
  if (value === 'Supervisor') return 'text-bg-info'
  return 'text-bg-secondary'
}

function formatDateTime(value) {
  if (!value) return '-'
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return '-'
  return parsed.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

async function loadLogs() {
  isLoading.value = true
  loadError.value = ''
  try {
    logs.value = await logsAPI.getAll({ take: 1000 })
  } catch (error) {
    loadError.value = getApiErrorMessage(error, 'Could not load activity logs.')
  } finally {
    isLoading.value = false
  }
}

onMounted(loadLogs)
</script>
