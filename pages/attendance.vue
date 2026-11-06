<template>
  <section class="content">
    <!-- Attendance Report Card -->
    <div class="card shadow-sm">
      <!-- Card Header -->
      <div class="card-header">
        <h3 class="card-title mb-0">Attendance Report</h3>
      </div>

      <!-- Control Bar -->
      <div class="card-body border-bottom">
        <div class="row g-2 mb-3">
          <!-- Search Input -->
          <div class="col-md-4">
            <div class="input-group input-group-sm">
              <span class="input-group-text"><i class="fas fa-search"></i></span>
              <input
                v-model="filters.search"
                type="text"
                class="form-control"
                placeholder="Search by Name or ID..."
                @input="currentPage = 1"
              />
            </div>
          </div>

          <!-- Date Range Filters -->
          <div class="col-md-2">
            <input
              v-model="filters.dateFrom"
              type="date"
              class="form-control form-control-sm"
              @change="currentPage = 1"
            />
          </div>
          <div class="col-md-2">
            <input
              v-model="filters.dateTo"
              type="date"
              class="form-control form-control-sm"
              @change="currentPage = 1"
            />
          </div>

          <!-- Status Filter -->
          <div class="col-md-2">
            <select
              v-model="filters.status"
              class="form-select form-select-sm"
              @change="currentPage = 1"
            >
              <option value="">All Status</option>
              <option value="On Time">On Time</option>
              <option value="Late">Late</option>
            </select>
          </div>

          <!-- Sort By -->
          <div class="col-md-2">
            <select
              v-model="filters.sortBy"
              class="form-select form-select-sm"
              @change="currentPage = 1"
            >
              <option value="name">Sort by Name</option>
              <option value="date">Sort by Date</option>
              <option value="status">Sort by Status</option>
              <option value="hours">Sort by Hours</option>
            </select>
          </div>
        </div>

        <!-- Second Row: Rows Per Page and Export Buttons -->
        <div class="row g-2">
          <!-- Rows Per Page -->
          <div class="col-md-3">
            <div class="d-flex align-items-center gap-2">
              <label for="rowsPerPageSelect" class="form-label mb-0">Show</label>
              <select
                id="rowsPerPageSelect"
                v-model.number="rowsPerPage"
                class="form-select form-select-sm"
                style="width: auto"
                @change="currentPage = 1"
              >
                <option :value="10">10</option>
                <option :value="25">25</option>
                <option :value="50">50</option>
                <option :value="100">100</option>
              </select>
              <span class="text-muted">rows</span>
            </div>
          </div>

          <!-- Export Buttons -->
          <div class="col-md-9 text-end">
            <button type="button" class="btn btn-sm btn-outline-secondary me-2" title="Print" @click="exportPrint">
              <i class="fas fa-print me-1"></i> Print
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary me-2" title="Export to Excel" @click="exportExcel">
              <i class="fas fa-file-excel me-1"></i> Excel
            </button>
            <button type="button" class="btn btn-sm btn-outline-secondary" title="Export to PDF" @click="exportPDF">
              <i class="fas fa-file-pdf me-1"></i> PDF
            </button>
          </div>
        </div>
      </div>

      <!-- Loading State -->
      <div v-if="isLoading" class="card-body text-center my-4">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
      </div>

      <!-- Error State -->
      <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center m-3">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" @click="loadAttendanceData">Retry</button>
      </div>

      <!-- Data Table -->
      <div v-else class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light">
            <tr>
              <th>Intern</th>
              <th>Date</th>
              <th class="text-center">AM Time In</th>
              <th class="text-center">AM Time Out</th>
              <th class="text-center">PM Time In</th>
              <th class="text-center">PM Time Out</th>
              <th class="text-center">Total Hours</th>
              <th class="text-center">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="row in paginatedData" :key="row.id">
              <td class="text-dark">{{ row.name }}</td>
              <td>{{ formatDate(row.date) }}</td>
              <td class="text-center">{{ formatTime(row.morning?.timeIn) }}</td>
              <td class="text-center">{{ formatTime(row.morning?.timeOut) }}</td>
              <td class="text-center">{{ formatTime(row.afternoon?.timeIn) }}</td>
              <td class="text-center">{{ formatTime(row.afternoon?.timeOut) }}</td>
              <td class="text-center">{{ formatHours(row.hours) }}</td>
              <td class="text-center">
                <span class="badge" :class="row.status === 'Late' ? 'bg-warning text-dark' : 'bg-success'">
                  {{ row.status }}
                </span>
              </td>
            </tr>
            <tr v-if="paginatedData.length === 0">
              <td colspan="8" class="text-center text-muted py-4">
                No attendance records found.
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Pagination Footer -->
      <div v-if="!isLoading && !loadError && filteredData.length > 0" class="card-footer d-flex justify-content-between align-items-center">
        <span class="text-muted small">
          Showing {{ (currentPage - 1) * rowsPerPage + 1 }} to {{ Math.min(currentPage * rowsPerPage, filteredData.length) }}
          of {{ filteredData.length }} records
        </span>

        <nav aria-label="Pagination">
          <ul class="pagination pagination-sm mb-0">
            <!-- First Page -->
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <button type="button" class="page-link" @click="currentPage = 1" :disabled="currentPage === 1">
                <i class="fas fa-step-backward"></i> First
              </button>
            </li>

            <!-- Previous Page -->
            <li class="page-item" :class="{ disabled: currentPage === 1 }">
              <button type="button" class="page-link" @click="currentPage--" :disabled="currentPage === 1">
                <i class="fas fa-chevron-left"></i> Prev
              </button>
            </li>

            <!-- Page Numbers -->
            <li v-for="page in visiblePages" :key="page" class="page-item" :class="{ active: page === currentPage }">
              <button
                v-if="page !== '...'"
                type="button"
                class="page-link"
                @click="currentPage = page"
                :class="{ active: page === currentPage }"
              >
                {{ page }}
              </button>
              <span v-else class="page-link">{{ page }}</span>
            </li>

            <!-- Next Page -->
            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
              <button type="button" class="page-link" @click="currentPage++" :disabled="currentPage === totalPages">
                Next <i class="fas fa-chevron-right"></i>
              </button>
            </li>

            <!-- Last Page -->
            <li class="page-item" :class="{ disabled: currentPage === totalPages }">
              <button type="button" class="page-link" @click="currentPage = totalPages" :disabled="currentPage === totalPages">
                Last <i class="fas fa-step-forward"></i>
              </button>
            </li>
          </ul>
        </nav>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, computed, reactive, onMounted } from 'vue'
import { dtrAPI, getApiErrorMessage } from '@/services/api'
import { dateKey as toDateKey, formatClock, formatDisplayDate } from '@/utils/datetime'

// ================= State =================
const isLoading = ref(false)
const loadError = ref('')
const currentPage = ref(1)
const rowsPerPage = ref(10)

// Sample data - replace with actual API call
const attendanceData = ref([])

// Filters
const filters = reactive({
  search: '',
  dateFrom: '',
  dateTo: '',
  status: '',
  sortBy: 'name'
})

// ================= Computed =================
const filteredData = computed(() => {
  let data = [...attendanceData.value]

  // Search filter
  if (filters.search) {
    const query = filters.search.toLowerCase()
    data = data.filter(
      row =>
        (row.name && row.name.toLowerCase().includes(query)) ||
        (row.id && row.id.toString().includes(query))
    )
  }

  // Date range filter
  if (filters.dateFrom) {
    data = data.filter(row => dateKey(row.date) >= filters.dateFrom)
  }
  if (filters.dateTo) {
    data = data.filter(row => dateKey(row.date) <= filters.dateTo)
  }

  // Status filter
  if (filters.status) {
    data = data.filter(row => row.status === filters.status)
  }

  // Sorting
  data.sort((a, b) => {
    switch (filters.sortBy) {
      case 'name':
        return (a.name || '').localeCompare(b.name || '')
      case 'date':
        return new Date(b.date || 0) - new Date(a.date || 0)
      case 'status':
        return (a.status || '').localeCompare(b.status || '')
      case 'hours':
        return (b.hours || 0) - (a.hours || 0)
      default:
        return 0
    }
  })

  return data
})

const totalPages = computed(() => Math.ceil(filteredData.value.length / rowsPerPage.value))

const paginatedData = computed(() => {
  const start = (currentPage.value - 1) * rowsPerPage.value
  const end = start + rowsPerPage.value
  return filteredData.value.slice(start, end)
})

const visiblePages = computed(() => {
  const pages = []
  const maxVisible = 5
  let startPage = Math.max(1, currentPage.value - Math.floor(maxVisible / 2))
  let endPage = Math.min(totalPages.value, startPage + maxVisible - 1)

  if (endPage - startPage < maxVisible - 1) {
    startPage = Math.max(1, endPage - maxVisible + 1)
  }

  if (startPage > 1) {
    pages.push(1)
    if (startPage > 2) pages.push('...')
  }

  for (let i = startPage; i <= endPage; i++) {
    pages.push(i)
  }

  if (endPage < totalPages.value) {
    if (endPage < totalPages.value - 1) pages.push('...')
    pages.push(totalPages.value)
  }

  return pages
})

// ================= Methods =================
function dateKey(value) {
  return toDateKey(value)
}

function formatDate(value) {
  return formatDisplayDate(value)
}

function formatTime(time) {
  return formatClock(time)
}

function formatHours(hours) {
  const value = Number(hours)
  return Number.isFinite(value) && value > 0 ? value.toFixed(2) : '—'
}

function clockMinutes(value) {
  if (!value) return null
  const match = String(value).match(/(?:T|\s|^)(\d{1,2}):(\d{2})/)
  if (!match) return null
  return Number(match[1]) * 60 + Number(match[2])
}

function isAfterEight(value) {
  const minutes = clockMinutes(value)
  return minutes != null && minutes > 8 * 60
}

function shiftHours(record) {
  const start = clockMinutes(record?.timeIn)
  const end = clockMinutes(record?.timeOut)
  if (start == null || end == null || end < start) return 0
  return (end - start) / 60
}

function groupByDay(records) {
  const rows = new Map()
  for (const record of records) {
    const key = `${record.internId ?? record.internName ?? 'unknown'}|${dateKey(record.date)}`
    if (!rows.has(key)) {
      rows.set(key, {
        id: key,
        name: record.internName || '—',
        date: record.date,
        morning: null,
        afternoon: null
      })
    }
    const row = rows.get(key)
    const slot = String(record.shiftType || '').toLowerCase() === 'afternoon' ? 'afternoon' : 'morning'
    const current = row[slot]
    const started = clockMinutes(record.timeIn) ?? -1
    const currentStarted = current ? (clockMinutes(current.timeIn) ?? -1) : -1
    if (!current || started >= currentStarted) row[slot] = record
    if (record.internName) row.name = record.internName
  }

  return [...rows.values()].map((row) => {
    const hours = shiftHours(row.morning) + shiftHours(row.afternoon)
    const late = isAfterEight(row.morning?.timeIn)
    return { ...row, hours: Math.round(hours * 100) / 100, status: late ? 'Late' : 'On Time' }
  })
}

async function loadAttendanceData() {
  isLoading.value = true
  loadError.value = ''
  try {
    attendanceData.value = groupByDay(await dtrAPI.getAll())
  } catch (error) {
    attendanceData.value = []
    loadError.value = getApiErrorMessage(error, 'Failed to load attendance data. Please try again.')
  } finally {
    isLoading.value = false
  }
}

// ================= Export Methods =================
function exportPrint() {
  window.print()
}

function exportExcel() {
  // TODO: Implement Excel export using xlsx library
  console.log('Export to Excel clicked')
  alert('Excel export functionality coming soon')
}

function exportPDF() {
  // TODO: Implement PDF export using jsPDF library
  console.log('Export to PDF clicked')
  alert('PDF export functionality coming soon')
}

// ================= Lifecycle =================
onMounted(() => {
  loadAttendanceData()
})
</script>

<style scoped>
.table {
  font-size: 0.875rem;
}

.badge {
  font-size: 0.75rem;
  padding: 0.35rem 0.65rem;
}

@media print {
  .card-body.border-bottom,
  .card-footer,
  .btn {
    display: none;
  }

  .table {
    font-size: 0.75rem;
  }
}
</style>
