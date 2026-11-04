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
              <option value="Present">Present</option>
              <option value="Absent">Absent</option>
              <option value="Leave">Leave</option>
              <option value="Late">Late</option>
              <option value="Early Exit">Early Exit</option>
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
              <th scope="col" style="width: 40px;">SI</th>
              <th scope="col">ID</th>
              <th scope="col" style="width: 200px;">Employee/Trainee Name</th>
              <th scope="col">Employment Type</th>
              <th scope="col">Shift</th>
              <th scope="col">Workplace</th>
              <th scope="col" colspan="3" class="text-center">Date Log</th>
              <th scope="col" class="text-center">Present</th>
              <th scope="col" class="text-center">Absent</th>
              <th scope="col" class="text-center">Leave</th>
              <th scope="col" class="text-center">Late</th>
              <th scope="col" class="text-center">Early Exit</th>
              <th scope="col" class="text-center">Expected Hours</th>
              <th scope="col" class="text-center">Actual Hours</th>
              <th scope="col" class="text-center">Diff Hours</th>
              <th scope="col" class="text-center">Avg Hours</th>
            </tr>
            <tr class="table-light" style="border-top: none;">
              <th colspan="6"></th>
              <th scope="col" class="text-center small">Entry</th>
              <th scope="col" class="text-center small">Exit</th>
              <th scope="col" class="text-center small">Hour</th>
              <th colspan="10"></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(row, index) in paginatedData" :key="row.id">
              <td class="text-center text-muted small">{{ (currentPage - 1) * rowsPerPage + index + 1 }}</td>
              <td class="text-dark fw-semibold">{{ row.id }}</td>
              <td>
                <div class="d-flex align-items-center gap-2">
                  <img
                    v-if="row.avatar"
                    :src="row.avatar"
                    :alt="row.name"
                    class="rounded-circle"
                    style="width: 32px; height: 32px; object-fit: cover"
                  />
                  <div v-else class="rounded-circle bg-light d-flex align-items-center justify-content-center" style="width: 32px; height: 32px;">
                    <i class="fas fa-user text-muted small"></i>
                  </div>
                  <span class="text-dark">{{ row.name }}</span>
                </div>
              </td>
              <td class="text-dark">{{ row.employmentType || '—' }}</td>
              <td class="text-dark">{{ row.shift || '—' }}</td>
              <td class="text-dark">{{ row.workplace || '—' }}</td>
              <td class="text-center">{{ formatTime(row.dateLogEntry) }}</td>
              <td class="text-center">{{ formatTime(row.dateLogExit) }}</td>
              <td class="text-center">{{ formatDuration(row.dateLogHour) }}</td>
              <td class="text-center">
                <span class="badge bg-success">{{ row.present || 0 }}</span>
              </td>
              <td class="text-center">
                <span class="badge bg-danger">{{ row.absent || 0 }}</span>
              </td>
              <td class="text-center">
                <span class="badge bg-warning text-dark">{{ row.leave || 0 }}</span>
              </td>
              <td class="text-center">
                <span class="badge bg-warning text-dark">{{ row.late || 0 }}</span>
              </td>
              <td class="text-center">
                <span class="badge bg-orange">{{ row.earlyExit || 0 }}</span>
              </td>
              <td class="text-center">{{ formatHours(row.expectedHours) }}</td>
              <td class="text-center">{{ formatHours(row.actualHours) }}</td>
              <td class="text-center">{{ formatHoursDiff(row.diffHours) }}</td>
              <td class="text-center">{{ formatHours(row.avgHours) }}</td>
            </tr>
            <tr v-if="paginatedData.length === 0">
              <td :colspan="19" class="text-center text-muted py-4">
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
    data = data.filter(row => {
      const rowDate = row.date ? new Date(row.date).toDateString() : null
      const fromDate = new Date(filters.dateFrom).toDateString()
      return rowDate >= fromDate
    })
  }
  if (filters.dateTo) {
    data = data.filter(row => {
      const rowDate = row.date ? new Date(row.date).toDateString() : null
      const toDate = new Date(filters.dateTo).toDateString()
      return rowDate <= toDate
    })
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
        return (b.actualHours || 0) - (a.actualHours || 0)
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
function formatTime(time) {
  if (!time) return '—'
  const date = new Date(time)
  if (!Number.isNaN(date.getTime())) {
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: true })
  }
  return '—'
}

function formatDuration(duration) {
  if (!duration || duration === 0) return '—'
  const hours = Math.floor(duration / 60)
  const minutes = duration % 60
  return `${hours}h ${minutes}m`
}

function formatHours(hours) {
  if (!hours || hours === 0) return '—'
  const wholeHours = Math.floor(hours)
  const minutes = Math.round((hours % 1) * 60)
  return minutes > 0 ? `${wholeHours}h ${minutes}m` : `${wholeHours}h`
}

function formatHoursDiff(diff) {
  if (diff === null || diff === undefined || diff === 0) return '0h'
  const isNegative = diff < 0
  const absDiff = Math.abs(diff)
  const wholeHours = Math.floor(absDiff)
  const minutes = Math.round((absDiff % 1) * 60)
  const formatted = minutes > 0 ? `${wholeHours}h ${minutes}m` : `${wholeHours}h`
  return isNegative ? `-${formatted}` : `+${formatted}`
}

async function loadAttendanceData() {
  isLoading.value = true
  loadError.value = ''
  try {
    // TODO: Replace with actual API call
    // const response = await fetch('/api/attendance')
    // attendanceData.value = await response.json()

    // Sample data for demonstration
    attendanceData.value = [
      {
        id: 'E001',
        name: 'John Doe',
        avatar: null,
        employmentType: 'Intern',
        shift: '8AM - 5PM',
        workplace: 'Sales Dept',
        date: '2024-01-15',
        status: 'Present',
        dateLogEntry: '2024-01-15T08:05:00',
        dateLogExit: '2024-01-15T17:00:00',
        dateLogHour: 480,
        present: 18,
        absent: 2,
        leave: 1,
        late: 1,
        earlyExit: 0,
        expectedHours: 160,
        actualHours: 155.5,
        diffHours: -4.5,
        avgHours: 8.6
      },
      {
        id: 'E002',
        name: 'Jane Smith',
        avatar: null,
        employmentType: 'Trainee',
        shift: '8AM - 5PM',
        workplace: 'HR Dept',
        date: '2024-01-15',
        status: 'Absent',
        dateLogEntry: null,
        dateLogExit: null,
        dateLogHour: 0,
        present: 16,
        absent: 4,
        leave: 0,
        late: 0,
        earlyExit: 0,
        expectedHours: 160,
        actualHours: 128,
        diffHours: -32,
        avgHours: 8.0
      },
      {
        id: 'E003',
        name: 'Michael Johnson',
        avatar: null,
        employmentType: 'Intern',
        shift: '8AM - 5PM',
        workplace: 'IT Dept',
        date: '2024-01-15',
        status: 'Late',
        dateLogEntry: '2024-01-15T08:35:00',
        dateLogExit: '2024-01-15T17:15:00',
        dateLogHour: 480,
        present: 19,
        absent: 0,
        leave: 0,
        late: 2,
        earlyExit: 1,
        expectedHours: 160,
        actualHours: 160,
        diffHours: 0,
        avgHours: 8.42
      }
    ]
  } catch (error) {
    loadError.value = 'Failed to load attendance data. Please try again.'
    console.error('Attendance data load error:', error)
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
.bg-orange {
  background-color: #ff6b00;
}

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
