<template>
  <section class="cocgen-page">
    <div class="cocgen-toolbar">
      <div>
        <h2 class="cocgen-title">Certificate of Completion Generation</h2>
        <p class="cocgen-subtitle">
          Generate Certificate of Completion for interns who have fully rendered their required hours.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" @click="resetForm">
          <i class="fas fa-rotate-left me-1"></i> Reset
        </button>
        <button type="button" class="btn btn-primary" @click="generateCertificate">
          <i class="fas fa-print me-1"></i> Generate Certificate
        </button>
      </div>
    </div>

    <div v-if="notice" class="alert alert-success m-3 mb-0">{{ notice }}</div>
    <div v-if="error" class="alert alert-danger m-3 mb-0 d-flex justify-content-between align-items-center">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-danger" @click="loadInterns">Retry</button>
    </div>

    <div v-if="isInternsLoading" class="text-center my-5">
      <div class="spinner-border text-primary" role="status"></div>
      <p>Loading interns...</p>
    </div>

    <div v-else-if="filteredInterns.length === 0" class="alert alert-info m-3">
      <p>No interns found with completed status. All required hours must be fully rendered.</p>
    </div>

    <div v-else class="cocgen-workspace">
      <!-- ================= INTERN SELECTION ================= -->
      <aside class="cocgen-sidebar">
        <div class="card shadow-sm">
          <div class="card-header cocgen-sidebar-head">
            <h3 class="card-title mb-0"><i class="far fa-user me-2"></i>Select Intern</h3>
          </div>

          <div class="card-body cocgen-form">
            <div class="cocgen-lookup">
              <label for="cocgenInternSearch" class="form-label">Find Intern</label>
              <div class="cocgen-lookup-field" @focusout="onInternFocusOut">
                <i class="fas fa-search" aria-hidden="true"></i>
                <input
                  id="cocgenInternSearch"
                  v-model="internQuery"
                  type="search"
                  class="form-control"
                  placeholder="Search by name or ID number"
                  autocomplete="off"
                  role="combobox"
                  :aria-expanded="internSearchOpen"
                  aria-controls="cocgenInternResults"
                  @focus="openInternSearch"
                  @input="onInternInput"
                  @keydown.esc="internSearchOpen = false"
                />
                <ul
                  v-if="internSearchOpen"
                  id="cocgenInternResults"
                  class="cocgen-lookup-results"
                  role="listbox"
                >
                  <li v-if="internsLoading" class="cocgen-lookup-state">Loading interns…</li>
                  <li v-else-if="internError" class="cocgen-lookup-state text-danger">{{ internError }}</li>
                  <li v-else-if="!filteredInterns.length" class="cocgen-lookup-state">No matching interns found.</li>
                  <template v-else>
                    <li v-for="intern in filteredInterns" :key="intern.id">
                      <button
                        type="button"
                        class="cocgen-lookup-option"
                        role="option"
                        :aria-selected="selectedInternId === intern.id"
                        @mousedown.prevent
                        @click="applyIntern(intern)"
                      >
                        <strong>{{ intern.fullName }}</strong>
                        <span>
                          {{ intern.applicantNo || `#${intern.id}` }}
                          &middot; {{ intern.school || '—' }}
                        </span>
                      </button>
                    </li>
                  </template>
                </ul>
              </div>
              <p class="cocgen-hint mb-0">Pick an intern to fill the certificate details automatically.</p>
            </div>

            <details class="cocgen-form-section" open>
              <summary class="cocgen-form-heading">
                <span>Intern Details</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="cocgenFullName" class="form-label">Full Name</label>
                <input
                  id="cocgenFullName"
                  v-model="form.fullName"
                  type="text"
                  class="form-control"
                  readonly
                />
              </div>

              <div class="mb-3">
                <label for="cocgenOffice" class="form-label">Office / Department</label>
                <select id="cocgenOffice" v-model="form.office" class="form-select" readonly>
                  <option v-for="office in officeOptions" :key="office" :value="office">
                    {{ office }}
                  </option>
                </select>
              </div>

              <div class="mb-3">
                <label for="cocgenIDNumber" class="form-label">ID Number</label>
                <input
                  id="cocgenIDNumber"
                  v-model="form.idNumber"
                  type="text"
                  class="form-control"
                  readonly
                />
              </div>
            </details>

            <details class="cocgen-form-section">
              <summary class="cocgen-form-heading">
                <span>Course & School</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="cocgenCourse" class="form-label">Course</label>
                <input
                  id="cocgenCourse"
                  v-model="form.course"
                  type="text"
                  class="form-control"
                  readonly
                />
              </div>

              <div class="mb-3">
                <label for="cocgenSchool" class="form-label">School Name</label>
                <input
                  id="cocgenSchool"
                  v-model="form.schoolName"
                  type="text"
                  class="form-control"
                  readonly
                />
              </div>
            </details>

            <details class="cocgen-form-section">
              <summary class="cocgen-form-heading">
                <span>Tenure</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="row g-2">
                <div class="col-6">
                  <label for="cocgenDateStarted" class="form-label">Date Started</label>
                  <input id="cocgenDateStarted" v-model="form.dateStarted" type="date" class="form-control" readonly />
                </div>
                <div class="col-6">
                  <label for="cocgenDateCompleted" class="form-label">Date Completed</label>
                  <input id="cocgenDateCompleted" v-model="form.dateCompleted" type="date" class="form-control" readonly />
                </div>
              </div>
            </details>

            <details class="cocgen-form-section">
              <summary class="cocgen-form-heading">
                <span>Hours Rendered</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="row g-2">
                <div class="col-6">
                  <label for="cocgenHoursRendered" class="form-label">Hours Rendered</label>
                  <input
                    id="cocgenHoursRendered"
                    v-model="form.hoursRendered"
                    type="number"
                    class="form-control"
                    readonly
                  />
                </div>
                <div class="col-6">
                  <label for="cocgenRequiredHours" class="form-label">Required Hours</label>
                  <input
                    id="cocgenRequiredHours"
                    v-model="form.requiredHours"
                    type="number"
                    class="form-control"
                    readonly
                  />
                </div>
              </div>
            </details>
          </div>
        </div>
      </aside>

      <!-- ================= PREVIEW ================= -->
      <div class="cocgen-preview">
        <div class="cocgen-preview-head">
          <div>
            <span class="cocgen-preview-eyebrow">Live Preview</span>
            <p class="cocgen-preview-note mb-0">
              <i class="fas fa-ruler-combined me-1"></i> Letter format (8.5″ × 11″)
            </p>
          </div>
        </div>

        <div class="cocgen-preview-stage">
          <div class="cocgen-cards">
            <div class="cocgen-card-wrap">
              <span class="cocgen-card-tag">Front</span>

              <article class="cocgen-card cocgen-card--front" aria-label="Certificate of Completion front">
                <header class="cocgen-front-head">
                  <div class="cocgen-seal" aria-hidden="true">
                    <img src="/assets/images/logo.png" alt="COA Logo" />
                  </div>
                  <div class="cocgen-head-copy">
                    <span class="cocgen-republic">Republic of the Philippines</span>
                    <strong class="cocgen-agency">COMMISSION ON AUDIT</strong>
                    <span class="cocgen-agency-address">Commonwealth Ave., Quezon City</span>
                  </div>
                </header>

                <div class="cocgen-front-body">
                  <div class="cocgen-details">
                    <h2 class="cocgen-name">{{ form.fullName || 'INTERN NAME' }}</h2>
                    <p class="cocgen-meta">
                      <span>Course</span>
                      <strong>{{ form.course || '—' }}</strong>
                    </p>
                    <p class="cocgen-meta">
                      <span>School</span>
                      <strong>{{ form.schoolName || '—' }}</strong>
                    </p>
                    <p class="cocgen-meta">
                      <span>Date Started</span>
                      <strong>{{ formattedDates.started }}</strong>
                    </p>
                    <p class="cocgen-meta">
                      <span>Date Completed</span>
                      <strong>{{ formattedDates.dateCompleted }}</strong>
                    </p>
                    <p class="cocgen-meta">
                      <span>Hours Rendered</span>
                      <strong>{{ form.hoursRendered }} / {{ form.requiredHours }}</strong>
                    </p>
                  </div>
                </div>

                <footer class="cocgen-card-footer">
                  <span>Certificate of Completion</span>
                </footer>
              </article>
            </div>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, inject, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import QRCode from 'qrcode'
import officesData from '../assets/json/offices.json'
import { getApiErrorMessage, internsAPI, internPortalAPI } from '@/services/api'

/* Same shared office list that feeds the Signatories module dropdown. */
const officeOptions = [
  ...new Set((officesData.offices || []).map((office) => office.name).filter(Boolean))
]

/* ===================== Reactive State ===================== */
const dialog = inject('dialog', null)
const { showConfirm } = dialog || {}

const form = reactive({
  fullName: '',
  office: '',
  idNumber: '',
  course: '',
  dateStarted: '',
  dateCompleted: '',
  hoursRendered: 0,
  requiredHours: 0
})

const interns = ref([])
const internsLoading = ref(false)
const internError = ref('')
const internQuery = ref('')
const internSearchOpen = ref(false)
const selectedInternId = ref(null)

const filteredInterns = computed(() => {
  const query = internQuery.value.trim().toLowerCase()

  if (!query) return interns.value.filter(i => i.status === 'Completed') || []

  return interns.value.filter((intern) =>
    [intern.fullName, intern.applicantNo, intern.id]
      .filter((value) => value !== null && value !== undefined && value !== '')
      .some((value) => String(value).toLowerCase().includes(query))
  ).filter(i => i.status === 'Completed')
})

/* ===================== Intern Autocomplete ===================== */
async function loadInterns() {
  if (internsLoading.value || interns.value.length) return

  internsLoading.value = true
  internError.value = ''

  try {
    interns.value = await internsAPI.getAll()
  } catch (error) {
    internError.value = getApiErrorMessage(error, 'Could not load interns.')
  } finally {
    internsLoading.value = false
  }
}

/* Parse route query for internId and auto-apply the intern */
const routeInternId = computed(() => {
  try {
    const search = window.location.search?.replace('?', '')
    if (!search) return null
    const params = new URLSearchParams(search)
    return params.get('internId') ? Number(params.get('internId')) : null
  } catch {
    return null
  }
})

/* Auto-apply selected intern after interns are loaded */
watch(interns, () => {
  if (!internsLoading.value) {
    const id = routeInternId.value
    if (id !== null) {
      const intern = interns.value.find(i => i.id === id)
      if (intern) {
        applyIntern(intern)
      }
    }
  }
}, { immediate: true })

function openInternSearch() {
  internSearchOpen.value = true
  loadInterns()
}

function onInternInput() {
  selectedInternId.value = null
  internSearchOpen.value = true
  loadInterns()
}

function onInternFocusOut(event) {
  if (!event.currentTarget.contains(event.relatedTarget)) {
    internSearchOpen.value = false
  }
}

function toDateInput(value) {
  if (!value) return ''

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

/* Apply selected intern to the form */
function applyIntern(intern) {
  selectedInternId.value = intern.id
  internQuery.value = intern.fullName || `#${intern.id}`
  internSearchOpen.value = false

  Object.assign(form, {
    fullName: intern.fullName || '',
    office: intern.applicantOffice || intern.requestOffice || '',
    idNumber: intern.applicantNo || String(intern.id),
    course: intern.courseName || intern.program || '',
    dateStarted: toDateInput(intern.startDate) || '',
    dateCompleted: toDateInput(intern.endDate) || '',
    hoursRendered: intern.hoursRendered || 0,
    requiredHours: intern.requiredHours || 0
  })
}

/* ===================== QR & Rendering ===================== */
const formattedDates = computed(() => {
  const started = form.dateStarted ? new Date(form.dateStarted + 'T00:00:00') : null
  const completed = form.dateCompleted ? new Date(form.dateCompleted + 'T00:00:00') : null

  return {
    started: started && !Number.isNaN(started.getTime())
      ? started.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
      : '—',
    dateCompleted: completed && !Number.isNaN(completed.getTime())
      ? completed.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
      : '—'
  }
})

/* ===================== Generate Certificate ===================== */
async function generateCertificate() {
  if (!selectedInternId.value) {
    alert('Please select an intern first.')
    return
  }

  /* Verify the selected intern has completed status */
  const selectedIntern = interns.value.find(i => i.id === selectedInternId.value)
  if (!selectedIntern || selectedIntern.status !== 'Completed') {
    alert('This intern does not have a Completed status. Only interns with fully rendered required hours can generate a Certificate of Completion.')
    return
  }

  const ok = await showConfirm(
    `Generate Certificate of Completion for ${selectedIntern.fullName}?`,
    'Confirm Generation'
  )
  if (!ok) return

  try {
    /* Build the certificate data and trigger print */
    form.fullName = selectedIntern.fullName
    form.office = selectedIntern.applicantOffice || selectedIntern.requestOffice || ''
    form.idNumber = selectedIntern.applicantNo || `#${selectedIntern.id}`
    form.course = selectedIntern.courseName || selectedIntern.program || ''
    form.dateStarted = toDateInput(selectedIntern.startDate)
    form.dateCompleted = toDateInput(selectedIntern.endDate)
    form.hoursRendered = selectedIntern.hoursRendered || 0
    form.requiredHours = selectedIntern.requiredHours || 0

    notice.value = `Certificate of Completion generated for ${selectedIntern.fullName}.`
    await printCertificate()
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Failed to generate certificate.')
  }
}

function formatDate(value) {
  if (!value) return '\u2014'

  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) return value

  return date.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
}

/* ===================== Print Certificate ===================== */
function printCertificate() {
  /* Trigger the browser print dialog with Letter-sized page */
  window.print()
}
</script>

<style>
/* =====================================================================
   Certificate of Completion Generation
   Mirrors ID Generation design patterns but uses Letter format (8.5 x 11in)
   All selectors prefixed with .cocgen- to avoid touching the app shell.
   ===================================================================== */
.cocgen-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  overflow: visible;
}

.cocgen-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
}

.cocgen-title {
  margin: 0;
  color: #1e0f0c;
  font-size: 1.4rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.cocgen-subtitle {
  margin: 0.25rem 0 0;
  max-width: 62ch;
  color: #74655d;
  font-size: 0.88rem;
}

/* ================= WORKSPACE LAYOUT ================= */
.cocgen-workspace {
  display: grid;
  grid-template-columns: 330px minmax(0, 1fr);
  gap: 1.15rem;
  align-items: start;
}

/* ================= SIDEBAR ================= */
.cocgen-sidebar > .card {
  display: flex;
  flex-direction: column;
  overflow: visible;
}

.cocgen-sidebar-head {
  flex: 0 0 auto;
  background: #fff7ec;
  border-bottom: 1px solid #f3e4d3;
}

.cocgen-sidebar-head .card-title {
  color: #1e0f0c;
  font-size: 1rem;
  font-weight: 800;
}

.cocgen-form {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-height: 0;
  gap: 0.85rem;
  padding: 0.9rem 1rem 1.05rem;
  overflow: visible;
}

.cocgen-form::-webkit-scrollbar {
  width: 8px;
}

.cocgen-form::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: #e0cdb9;
}

.cocgen-form-section + .cocgen-form-section {
  padding-top: 0.85rem;
  border-top: 1px dashed #e9dccd;
}

.cocgen-form-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  margin: 0;
  padding: 0.1rem 0;
  color: #a85300;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  list-style: none;
  cursor: pointer;
  user-select: none;
  transition: color 0.2s ease;
}

/* Hide the native disclosure triangle so the chevron is the only marker. */
.cocgen-form-heading::-webkit-details-marker {
  display: none;
}

.cocgen-form-heading::marker {
  content: '';
}

/* Chevron rotation when open */
.cocgen-form-heading:hover {
  color: #7d3d00;
}

.cocgen-form-heading:focus-visible {
  outline: 2px solid #f26522;
  outline-offset: 4px;
  border-radius: 4px;
}

.cocgen-section-chevron {
  flex: 0 0 auto;
  color: #c07a3a;
  font-size: 0.66rem;
  transform: rotate(0deg);
  transition: transform 0.28s cubic-bezier(0.4, 0, 0.2, 1);
}

.cocgen-form-section[open] > .cocgen-form-heading .cocgen-section-chevron {
  transform: rotate(90deg);
}

/* Space only when the section is expanded, so closed rows stay compact. */
.cocgen-form-section[open] > .cocgen-form-heading {
  margin-bottom: 0.65rem;
}

/* Tighter field rhythm inside the accordion. */
.cocgen-form .mb-3 {
  margin-bottom: 0.6rem !important;
}

.cocgen-form .form-label {
  margin-bottom: 0.25rem;
  color: #4a3931;
  font-size: 0.78rem;
  font-weight: 600;
}

.cocgen-form .form-control,
.cocgen-form .form-select {
  padding-top: 0.34rem;
  padding-bottom: 0.34rem;
  font-size: 0.84rem;
}

.cocgen-form .form-control {
  padding-right: 0.55rem;
  padding-left: 0.55rem;
}

/* ================= LOOKUP STYLES ================= */
.cocgen-lookup-field {
  position: relative;
}

.cocgen-lookup-field > i {
  position: absolute;
  top: 50%;
  left: 0.6rem;
  color: #b98d68;
  font-size: 0.75rem;
  pointer-events: none;
  transform: translateY(-50%);
}

.cocgen-lookup-field .form-control {
  padding-left: 1.7rem;
}

.cocgen-lookup-results {
  position: absolute;
  z-index: 20;
  top: calc(100% + 0.25rem);
  right: 0;
  left: 0;
  max-height: 15rem;
  margin: 0;
  padding: 0.3rem;
  overflow-y: auto;
  list-style: none;
  border: 1px solid #e6d7c6;
  border-radius: 0.6rem;
  background: #fff;
  box-shadow: 0 16px 32px -18px rgba(54, 31, 12, 0.55);
}

.cocgen-lookup-option {
  display: flex;
  flex-direction: column;
  width: 100%;
  gap: 0.1rem;
  padding: 0.45rem 0.55rem;
  border: 0;
  border-radius: 0.45rem;
  background: transparent;
  color: #3a2a22;
  text-align: left;
}

.cocgen-lookup-option:hover,
.cocgen-lookup-option:focus-visible,
.cocgen-lookup-option[aria-selected='true'] {
  background: #fff3e4;
  outline: none;
}

.cocgen-lookup-option strong {
  font-size: 0.8rem;
  font-weight: 700;
}

.cocgen-lookup-option span {
  color: #8a7a71;
  font-size: 0.68rem;
}

.cocgen-lookup-state {
  padding: 0.5rem 0.6rem;
  color: #8a7a71;
  font-size: 0.75rem;
}

/* ================= PREVIEW STYLES ================= */
.cocgen-preview {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  min-width: 0;
}

.cocgen-preview-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.4rem;
}

.cocgen-preview-eyebrow {
  display: block;
  color: #1e0f0c;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.cocgen-preview-note {
  color: #8a7a71;
  font-size: 0.75rem;
}

/* ================= CERTIFICATE CARD ================= */
.cocgen-preview-stage {
  display: block;
  padding: clamp(0.6rem, 1.2vw, 0.9rem);
  border: 1px solid #eadccb;
  border-radius: 1rem;
  background: #f4f1ea;
  overflow: visible;
}

.cocgen-cards {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: center;
  gap: clamp(0.6rem, 1vw, 0.9rem);
  width: 100%;
}

/* Certificate card dimensions use physical units for print preview */
.cocgen-card-wrap {
  --cocgen-card-scale: 1;
  --cocgen-card-width: 8.5in;
  --cocgen-card-height: 11in;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.45rem;
}

/* Scale the card from the top so the preview can be enlarged */
.cocgen-card-wrap > .cocgen-card {
  flex: 0 0 auto;
  margin:
    0
    calc((1 - var(--cocgen-card-scale)) * var(--cocgen-card-width) / -2)
    calc((1 - var(--cocgen-card-scale)) * var(--cocgen-card-height) * -1);
  transform: scale(var(--cocgen-card-scale));
  transform-origin: top center;
}

/* Certificate card visual style */
.cocgen-card {
  --cocgen-ink: #111;
  --cocgen-accent: #e87822;
  --cocgen-paper: #fff;
  position: relative;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  width: var(--cocgen-card-width);
  height: var(--cocgen-card-height);
  overflow: hidden;
  border: 1px solid #888;
  border-radius: 0;
  background: var(--cocgen-paper);
  color: var(--cocgen-ink);
  font-family: Arial, Helvetica, sans-serif;
  line-height: 1.2;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.12);
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
  /* Letter-sized card */
  width: 8.5in !important;
  height: 11in !important;
}

.cocgen-card--front {
  background: #fff;
}

.cocgen-front-head {
  display: flex;
  align-items: center;
  gap: 2.5mm;
  padding: 2.8mm 3.5mm 2.4mm;
  background: var(--cocgen-accent);
  color: #fff;
}

.cocgen-seal {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 13mm;
  height: 13mm;
  border-radius: 50%;
  background: #fff;
  overflow: hidden;
}

.cocgen-seal img {
  width: 13mm;
  height: 13mm;
  object-fit: cover;
  display: block;
}

.cocgen-head-copy {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  min-width: 0;
  text-align: center;
}

.cocgen-republic {
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 7pt;
  letter-spacing: 0.02em;
}

.cocgen-agency {
  margin: 0.3mm 0;
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 11.2pt;
  font-weight: 700;
  letter-spacing: 0.04em;
  line-height: 1.05;
}

.cocgen-agency-address {
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 7.5pt;
}

.cocgen-front-body {
  display: flex;
  align-items: flex-start;
  gap: 4mm;
  padding: 4mm 5mm 1mm;
  background: #fff;
}

.cocgen-details {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-width: 0;
}

.cocgen-name {
  margin: 0 0 2.4mm;
  color: #000;
  font-size: 11.5pt;
  font-weight: 800;
  line-height: 1.05;
  text-align: center;
  text-transform: uppercase;
}

.cocgen-meta {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 3mm;
  margin: 0 0 1.1mm;
  line-height: 1.2;
}

.cocgen-meta span,
.cocgen-meta strong {
  color: #000;
  font-size: 7pt;
  font-weight: 400;
}

.cocgen-meta strong {
  text-align: right;
  white-space: nowrap;
}

/* ================= PRINT STYLES ================= */
@page {
  size: Letter portrait;
  margin: 0;
}

@media print {
  html,
  body {
    width: auto !important;
    height: auto !important;
    min-height: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
    overflow: visible !important;
    background: #fff !important;
  }

  /* Hide every non-printable surface: app chrome, controls, and hints. */
  nav,
  .main-header,
  .main-sidebar,
  .main-footer,
  .sidebar-backdrop,
  .modal,
  .cocgen-toolbar,
  .cocgen-sidebar,
  .cocgen-preview-head,
  .cocgen-card-tag,
  .no-print {
    display: none !important;
  }

  .cocgen-page button,
  .cocgen-page input,
  .cocgen-page textarea,
  .cocgen-page select,
  .cocgen-page .btn {
    display: none !important;
  }

  .app-shell {
    display: block !important;
    width: auto !important;
    height: auto !important;
    min-height: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
    background: #fff !important;
  }

  .app-shell > .content-wrapper,
  .shell-content,
  .shell-body {
    display: block !important;
    width: auto !important;
    min-height: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
    overflow: visible !important;
    border-radius: 0 !important;
    background: #fff !important;
    box-shadow: none !important;
  }

  /* Certificate front stays on one page: 8.5in inside the usable width. */
  .cocgen-page {
    display: block !important;
    width: auto !important;
    margin: 0 !important;
    padding: 0 !important;
    background: #fff !important;
  }

  .cocgen-workspace,
  .cocgen-preview,
  .cocgen-preview-stage {
    position: static !important;
    display: block !important;
    width: auto !important;
    min-width: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
    border: 0 !important;
    border-radius: 0 !important;
    background: #fff !important;
    box-shadow: none !important;
    overflow: visible !important;
  }

  /* Front certificate: 8.5in width, 11in height */
  .cocgen-card-wrap {
    display: block !important;
    width: 8.5in !important;
    margin: 0 !important;
    padding: 0 !important;
    break-inside: avoid;
    page-break-inside: avoid;
  }

  .cocgen-card {
    width: 8.5in !important;
    height: 11in !important;
    margin: 0 !important;
    border: 1px solid #000 !important;
    border-radius: 0 !important;
    background: #fff !important;
    box-shadow: none !important;
    transform: none !important;
    break-inside: avoid;
    page-break-inside: avoid;
  }

  /* Never add a trailing break after the last badge. */
  .cocgen-card-wrap:last-child {
    break-after: auto;
    page-break-after: auto;
  }
}
</style>