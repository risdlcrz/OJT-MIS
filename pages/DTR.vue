<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">List of OJTs</h3>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered modern-table">
            <thead>
              <tr class="text-center">
                <th>OJT NO.</th>
                <th>FIRST NAME</th>
                <th>LAST NAME</th>
                <th>MIDDLE NAME</th>
                <th>SUFFIX</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="intern in interns" :key="intern.applicantNo">
                <td>{{ intern.applicantNo }}</td>
                <td>{{ intern.firstName || '' }}</td>
                <td>{{ intern.lastName || '' }}</td>
                <td>{{ intern.middleName || '' }}</td>
                <td>{{ intern.suffix || '' }}</td>
                <td class="text-center">
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action d-inline-flex flex-column align-items-center p-2" @click="openDtr(intern)">
                    <i class="fas fa-eye fa-lg mb-1"></i>
                    <span class="action-label">View</span>
                  </button>
                </td>
              </tr>
              <tr v-if="interns.length === 0">
                <td colspan="6" class="text-center text-muted py-4">No confirmed interns found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="selectedIntern" class="modal-backdrop fade show" @click="closeDtr"></div>
    <div v-if="selectedIntern" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeDtr">
      <div class="modal-dialog modal-xl modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header bg-orange py-2">
            <h5 class="modal-title text-white">View DTR</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeDtr">&times;</button>
          </div>
          <div class="modal-body">
            <div v-if="validationMessage" class="alert alert-danger">{{ validationMessage }}</div>
            <div class="table-responsive">
              <table class="table table-bordered">
                <thead class="thead-light text-center">
                  <tr>
                    <th>Date</th>
                    <th>AM In (editable)</th>
                    <th>AM Out (editable)</th>
                    <th>PM In (editable)</th>
                    <th>PM Out (editable)</th>
                    <th>Total Hours (decimal)</th>
                  </tr>
                </thead>
                <tbody>
                  <tr v-for="(day, index) in dtrRows" :key="`${day.date}-${index}`">
                    <td class="text-center">{{ day.date }}</td>
                    <td><input v-model="day.am_in" type="time" class="form-control form-control-sm" min="00:00" max="12:00" @change="handleTimeChange(index)"></td>
                    <td><input v-model="day.am_out" type="time" class="form-control form-control-sm" min="00:00" max="12:00" @change="handleTimeChange(index)"></td>
                    <td><input v-model="day.pm_in" type="time" class="form-control form-control-sm" min="13:00" max="23:59" @change="handleTimeChange(index)"></td>
                    <td><input v-model="day.pm_out" type="time" class="form-control form-control-sm" min="13:00" max="23:59" @change="handleTimeChange(index)"></td>
                    <td class="text-center">{{ formatHoursDecimal(rowMinutes(day)) }}</td>
                  </tr>
                </tbody>
              </table>
              <small class="text-muted">Working hours: 08:00 - 17:00. Lunch break: 12:00 - 13:00 (no working time during break).</small>
            </div>
          </div>
          <div class="modal-footer border-0">
            <button type="button" class="btn btn-success modern-action" @click="saveDtr">Save</button>
            <button type="button" class="btn btn-outline-secondary modern-action" @click="closeDtr">Close</button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onBeforeUnmount, onMounted, ref, computed } from 'vue'
import '../assets/css/Forms.css'

const APPLICANTS_KEY = 'ojt_applicants_v1'
const applicants = ref([])
const selectedIntern = ref(null)
const dtrRows = ref([])
const validationMessage = ref('')

const interns = computed(() =>
  applicants.value.filter((applicant) => applicant.accepted && applicant.orientation?.confirmed)
)

function readJson(key, fallback) {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch (error) {
    console.error(`Failed to read ${key}:`, error)
    return fallback
  }
}

function loadApplicants() {
  applicants.value = readJson(APPLICANTS_KEY, [])
}

function toMinutes(value) {
  if (!value) return null
  const [hours, minutes] = String(value).split(':').map(Number)
  return Number.isNaN(hours) || Number.isNaN(minutes) ? null : hours * 60 + minutes
}

function rowMinutes(day) {
  const amIn = toMinutes(day.am_in)
  const amOut = toMinutes(day.am_out)
  const pmIn = toMinutes(day.pm_in)
  const pmOut = toMinutes(day.pm_out)
  let minutes = 0
  if (amIn !== null && amOut !== null && amOut >= amIn) minutes += Math.max(0, Math.min(amOut, 720) - amIn)
  if (pmIn !== null && pmOut !== null && pmOut >= pmIn) minutes += Math.max(0, pmOut - Math.max(pmIn, 780))
  return minutes
}

function formatHoursDecimal(minutes) {
  return minutes === 0 ? '0.000' : (minutes / 60).toFixed(3)
}

function dateString(date) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}-${String(date.getDate()).padStart(2, '0')}`
}

function nextBusinessDay(dateValue) {
  const date = new Date(`${dateValue}T00:00:00`)
  do date.setDate(date.getDate() + 1)
  while (date.getDay() === 0 || date.getDay() === 6)
  return dateString(date)
}

function initialRows(startDate) {
  const date = new Date(`${startDate || dateString(new Date())}T00:00:00`)
  while (date.getDay() === 0 || date.getDay() === 6) date.setDate(date.getDate() + 1)
  return [{ date: dateString(date), am_in: '', am_out: '', pm_in: '', pm_out: '' }]
}

function hasTime(day) {
  return Boolean(day.am_in || day.am_out || day.pm_in || day.pm_out)
}

function openDtr(intern) {
  selectedIntern.value = intern
  const savedRows = readJson(`dtr_${intern.applicantNo}`, null)
  dtrRows.value = Array.isArray(savedRows) && savedRows.length
    ? savedRows.map((row) => ({ ...row }))
    : initialRows(intern.orientation?.date || intern.dateStarted)
  validationMessage.value = ''
}

function closeDtr() {
  selectedIntern.value = null
  validationMessage.value = ''
}

function handleTimeChange(index) {
  validationMessage.value = dtrRows.value[index].pm_out && toMinutes(dtrRows.value[index].pm_out) > 1020
    ? 'PM Out cannot be later than 17:00.'
    : ''
  const last = dtrRows.value[dtrRows.value.length - 1]
  if (index === dtrRows.value.length - 1 && hasTime(last)) {
    dtrRows.value.push({
      date: nextBusinessDay(last.date),
      am_in: '',
      am_out: '',
      pm_in: '',
      pm_out: ''
    })
  }
}

function saveDtr() {
  if (!selectedIntern.value) return
  const invalid = dtrRows.value.some((day) => day.pm_out && toMinutes(day.pm_out) > 1020)
  if (invalid) {
    validationMessage.value = 'Please fix PM Out times later than 17:00 before saving.'
    return
  }
  localStorage.setItem(`dtr_${selectedIntern.value.applicantNo}`, JSON.stringify(dtrRows.value))
  localStorage.setItem('ojt_interns_updated_at', new Date().toISOString())
  closeDtr()
}

onMounted(() => {
  loadApplicants()
  window.addEventListener('storage', loadApplicants)
})

onBeforeUnmount(() => window.removeEventListener('storage', loadApplicants))
</script>

<style scoped>
.action-label {
  font-size: 0.7rem;
  font-weight: bold;
  text-transform: uppercase;
}
.modal-body {
  max-height: calc(100vh - 220px);
  overflow: auto;
}
</style>
