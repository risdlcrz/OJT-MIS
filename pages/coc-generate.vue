<template>
  <section class="cocgen-page">
    <div class="cocgen-toolbar">
      <div>
        <h2 class="cocgen-title">Certificate of Completion</h2>
        <p class="cocgen-subtitle">
          Generate a certificate for interns who have rendered all of their required hours.
          The preview follows the official Commission on Audit letter.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" @click="resetForm">
          <i class="fas fa-rotate-left me-1"></i> Reset
        </button>
        <button type="button" class="btn btn-primary" :disabled="!selectedInternId" @click="generateCertificate">
          <i class="fas fa-print me-1"></i> Generate Certificate
        </button>
      </div>
    </div>

    <div v-if="notice" class="alert alert-success mb-0">{{ notice }}</div>
    <div v-if="error" class="alert alert-danger mb-0 d-flex justify-content-between align-items-center">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-danger" @click="loadInterns">Retry</button>
    </div>

    <div class="cocgen-workspace">
      <aside class="cocgen-sidebar">
        <div class="card shadow-sm">
          <div class="card-header cocgen-sidebar-head">
            <h3 class="card-title mb-0"><i class="fas fa-certificate me-2"></i>Certificate Details</h3>
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
                  placeholder="Search interns who completed required hours"
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
                  <li v-else-if="!filteredInterns.length" class="cocgen-lookup-state">
                    No interns have completed their required hours.
                  </li>
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
                          {{ formatHours(intern.hoursRendered) }} / {{ intern.requiredHours }} hrs
                          · {{ intern.schoolName || intern.school || '—' }}
                        </span>
                      </button>
                    </li>
                  </template>
                </ul>
              </div>
              <p class="cocgen-hint mb-0">Only interns who finished their required hours can be selected.</p>
            </div>

            <details class="cocgen-form-section" open>
              <summary class="cocgen-form-heading">
                <span>Intern</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="cocgenHonorific" class="form-label">Honorific</label>
                <select id="cocgenHonorific" v-model="form.honorific" class="form-select">
                  <option>Ms.</option>
                  <option>Mr.</option>
                </select>
              </div>

              <div class="mb-3">
                <label for="cocgenFullName" class="form-label">Full Name</label>
                <input id="cocgenFullName" v-model="form.fullName" type="text" class="form-control" readonly />
              </div>

              <div class="mb-3">
                <label for="cocgenSchool" class="form-label">School</label>
                <input id="cocgenSchool" v-model="form.schoolName" type="text" class="form-control" readonly />
              </div>

              <div class="mb-3">
                <label for="cocgenOffice" class="form-label">Office / Place of Training</label>
                <input id="cocgenOffice" v-model="form.office" type="text" class="form-control" />
              </div>
            </details>

            <details class="cocgen-form-section" open>
              <summary class="cocgen-form-heading">
                <span>Training</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="row g-2">
                <div class="col-6">
                  <label for="cocgenHours" class="form-label">Hours Completed</label>
                  <input id="cocgenHours" :value="form.hoursLabel" type="text" class="form-control" readonly />
                </div>
                <div class="col-6">
                  <label for="cocgenRequired" class="form-label">Required Hours</label>
                  <input id="cocgenRequired" :value="form.requiredHours || '—'" type="text" class="form-control" readonly />
                </div>
                <div class="col-6">
                  <label for="cocgenStart" class="form-label">Date Started</label>
                  <input id="cocgenStart" :value="form.dateStarted || '—'" type="text" class="form-control" readonly />
                </div>
                <div class="col-6">
                  <label for="cocgenEnd" class="form-label">Date Completed</label>
                  <input id="cocgenEnd" :value="form.dateCompleted || '—'" type="text" class="form-control" readonly />
                </div>
              </div>

              <div class="mb-3 mt-2">
                <label for="cocgenIssued" class="form-label">Date Issued</label>
                <input id="cocgenIssued" v-model="form.dateIssued" type="date" class="form-control" />
              </div>

              <div class="mb-0">
                <label for="cocgenPaper" class="form-label">Print Size</label>
                <select id="cocgenPaper" v-model="form.paper" class="form-select">
                  <option value="letter">Letter (8.5 × 11 in)</option>
                  <option value="a4">A4 (210 × 297 mm)</option>
                </select>
              </div>
            </details>

            <details class="cocgen-form-section" open>
              <summary class="cocgen-form-heading">
                <span>Signatory</span>
                <i class="fas fa-chevron-right cocgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="cocgenSignatory" class="form-label">Signatory</label>
                <select id="cocgenSignatory" v-model="selectedSignatoryId" class="form-select" @change="applySignatory">
                  <option :value="null">Select a signatory</option>
                  <option v-for="person in signatories" :key="person.id" :value="person.id">
                    {{ person.name }}
                  </option>
                </select>
              </div>
              <div class="mb-3">
                <label for="cocgenSignName" class="form-label">Name</label>
                <input id="cocgenSignName" v-model="form.signatoryName" type="text" class="form-control" />
              </div>
              <div class="mb-3">
                <label for="cocgenSignPosition" class="form-label">Position</label>
                <input id="cocgenSignPosition" v-model="form.signatoryPosition" type="text" class="form-control" />
              </div>
              <div class="mb-0">
                <label for="cocgenSignOffice" class="form-label">Division / Office</label>
                <input id="cocgenSignOffice" v-model="form.signatoryOffice" type="text" class="form-control" />
              </div>
            </details>
          </div>
        </div>
      </aside>

      <div class="cocgen-preview">
        <div class="cocgen-preview-head">
          <div>
            <span class="cocgen-preview-eyebrow">Live Preview</span>
            <p class="cocgen-preview-note mb-0">
              <i class="fas fa-ruler-combined me-1"></i>
              Fits Letter and A4 on one page
            </p>
          </div>
        </div>

        <div class="cocgen-preview-stage">
          <article class="coc-sheet" aria-label="Certificate of Completion preview">
            <header class="coc-head">
              <img class="coc-seal" :src="coaLogo" alt="Commission on Audit seal" />
              <div class="coc-head-copy">
                <p>Republic of the Philippines</p>
                <p class="coc-agency">COMMISSION ON AUDIT</p>
                <p>HUMAN RESOURCE MANAGEMENT OFFICE</p>
                <p>Commonwealth Avenue, Quezon City</p>
              </div>
            </header>

            <h1 class="coc-title">CERTIFICATE OF COMPLETION</h1>

            <p class="coc-body">
              This is to certify that <span class="coc-fill">{{ certifiedName }}</span>, of the <span class="coc-fill">{{ form.schoolName || 'School' }}</span>, has satisfactorily completed <span class="coc-fill">{{ form.hoursLabel || '___' }}</span> hours of office training at the <span class="coc-fill">{{ form.office || 'assigned office' }}</span>, Commission on Audit. The training covered the period from <span class="coc-fill">{{ periodStart }}</span> to <span class="coc-fill">{{ periodEnd }}</span>.
            </p>

            <p class="coc-issued">
              Issued this <span class="coc-fill">{{ issued.day }}</span> day of <span class="coc-fill">{{ issued.month }}</span>, <span class="coc-fill">{{ issued.year }}</span> at the Commission on Audit, Quezon City, Philippines.
            </p>

            <footer class="coc-sign">
              <div class="coc-sign-line" aria-hidden="true"></div>
              <p class="coc-sign-name">{{ form.signatoryName || 'SIGNATORY NAME' }}</p>
              <p>{{ form.signatoryPosition || 'Position' }}</p>
              <p>{{ form.signatoryOffice || 'Division' }}</p>
              <p>Human Resource Management Office</p>
            </footer>
          </article>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, inject, onMounted, reactive, ref, watch } from 'vue'
import coaLogo from '../assets/images/logo.png'
import { getApiErrorMessage, hrAttendanceAPI, signatoriesAPI } from '@/services/api'

const dialog = inject('dialog', null)
const { showConfirm } = dialog || {}

const form = reactive({
  honorific: 'Ms.',
  fullName: '',
  schoolName: '',
  office: '',
  hoursLabel: '',
  requiredHours: 0,
  dateStarted: '',
  dateCompleted: '',
  dateIssued: todayInput(),
  paper: 'letter',
  signatoryName: '',
  signatoryPosition: '',
  signatoryOffice: ''
})

const interns = ref([])
const internsLoading = ref(false)
const internError = ref('')
const internQuery = ref('')
const internSearchOpen = ref(false)
const selectedInternId = ref(null)
const signatories = ref([])
const selectedSignatoryId = ref(null)
const notice = ref('')
const error = ref('')

const filteredInterns = computed(() => {
  const query = internQuery.value.trim().toLowerCase()
  if (!query) return interns.value
  return interns.value.filter((intern) =>
    [intern.fullName, intern.applicantNo, intern.schoolName, intern.school, intern.id]
      .filter((value) => value !== null && value !== undefined && value !== '')
      .some((value) => String(value).toLowerCase().includes(query))
  )
})

const certifiedName = computed(() => {
  if (!form.fullName) return `${form.honorific} INTERN NAME`
  return `${form.honorific} ${form.fullName}`
})

const periodStart = computed(() => formatLongDate(form.dateStarted) || '__________')
const periodEnd = computed(() => formatLongDate(form.dateCompleted) || '__________')
const issued = computed(() => splitDate(form.dateIssued))

const routeInternId = computed(() => {
  try {
    const params = new URLSearchParams(window.location.search || '')
    const id = Number(params.get('internId'))
    return Number.isFinite(id) && id > 0 ? id : null
  } catch {
    return null
  }
})

watch([interns, internsLoading], () => {
  if (internsLoading.value) return
  const id = routeInternId.value
  if (!id) return
  const intern = interns.value.find((item) => item.id === id)
  if (intern) {
    applyIntern(intern)
    error.value = ''
  } else if (!error.value) {
    error.value = 'That intern has not completed the required hours, so a certificate cannot be generated.'
  }
})

function todayInput() {
  const now = new Date()
  const month = String(now.getMonth() + 1).padStart(2, '0')
  const day = String(now.getDate()).padStart(2, '0')
  return `${now.getFullYear()}-${month}-${day}`
}

function toDateInput(value) {
  if (!value) return ''
  const match = String(value).match(/^(\d{4}-\d{2}-\d{2})/)
  if (match) return match[1]
  const date = new Date(String(value).replace(/(\.\d{3})\d+/, '$1'))
  if (Number.isNaN(date.getTime())) return ''
  const month = String(date.getMonth() + 1).padStart(2, '0')
  const day = String(date.getDate()).padStart(2, '0')
  return `${date.getFullYear()}-${month}-${day}`
}

function parseInputDate(value) {
  if (!value) return null
  const date = new Date(`${String(value).slice(0, 10)}T00:00:00`)
  return Number.isNaN(date.getTime()) ? null : date
}

function ordinal(day) {
  const teen = day % 100
  if (teen >= 11 && teen <= 13) return `${day}th`
  if (day % 10 === 1) return `${day}st`
  if (day % 10 === 2) return `${day}nd`
  if (day % 10 === 3) return `${day}rd`
  return `${day}th`
}

function formatLongDate(value) {
  const date = parseInputDate(value)
  if (!date) return ''
  return date.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
}

function splitDate(value) {
  const date = parseInputDate(value)
  if (!date) return { day: '___', month: '________', year: '____' }
  return {
    day: ordinal(date.getDate()),
    month: date.toLocaleDateString('en-US', { month: 'long' }),
    year: String(date.getFullYear())
  }
}

function formatHours(value) {
  const number = Number(value)
  if (!Number.isFinite(number)) return '0'
  return Number.isInteger(number) ? String(number) : number.toFixed(2).replace(/0$/, '').replace(/\.0$/, '')
}

function certificateName(intern) {
  const first = (intern.firstName || '').trim()
  const middle = (intern.middleName || '').trim()
  const last = (intern.lastName || '').trim()
  const suffix = (intern.suffix || '').trim()
  if (first && last) {
    const initial = middle ? ` ${middle.charAt(0).toUpperCase()}.` : ''
    return `${first}${initial} ${last}${suffix ? ` ${suffix}` : ''}`.toUpperCase()
  }
  return (intern.fullName || '').toUpperCase()
}

function applyIntern(intern) {
  selectedInternId.value = intern.id
  internQuery.value = intern.fullName || ''
  internSearchOpen.value = false
  error.value = ''
  form.fullName = certificateName(intern)
  form.schoolName = intern.schoolName || intern.school || ''
  form.office = intern.applicantOffice || ''
  form.hoursLabel = formatHours(intern.hoursRendered)
  form.requiredHours = intern.requiredHours || 0
  form.dateStarted = toDateInput(intern.startDate)
  form.dateCompleted = toDateInput(intern.endDate)
  if (!form.dateIssued) form.dateIssued = todayInput()
}

function applySignatory() {
  const person = signatories.value.find((item) => String(item.id) === String(selectedSignatoryId.value))
  if (!person) return
  form.signatoryName = (person.name || '').toUpperCase()
  form.signatoryPosition = person.position || ''
  form.signatoryOffice = person.department || ''
}

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

async function loadInterns() {
  internsLoading.value = true
  internError.value = ''
  error.value = ''
  try {
    const data = await hrAttendanceAPI.eligibleForCertificate()
    interns.value = Array.isArray(data) ? data : []
  } catch (e) {
    interns.value = []
    internError.value = getApiErrorMessage(e, 'Could not load interns who completed their hours.')
    error.value = internError.value
  } finally {
    internsLoading.value = false
  }
}

async function loadSignatories() {
  try {
    const data = await signatoriesAPI.getAll()
    signatories.value = Array.isArray(data) ? data : []
    if (!selectedSignatoryId.value && signatories.value.length) {
      selectedSignatoryId.value = signatories.value[0].id
      applySignatory()
    }
  } catch {
    signatories.value = []
  }
}

function resetForm() {
  selectedInternId.value = null
  internQuery.value = ''
  notice.value = ''
  error.value = ''
  form.honorific = 'Ms.'
  form.fullName = ''
  form.schoolName = ''
  form.office = ''
  form.hoursLabel = ''
  form.requiredHours = 0
  form.dateStarted = ''
  form.dateCompleted = ''
  form.dateIssued = todayInput()
  form.paper = 'letter'
  if (signatories.value.length) {
    selectedSignatoryId.value = signatories.value[0].id
    applySignatory()
  }
}

async function generateCertificate() {
  const intern = interns.value.find((item) => item.id === selectedInternId.value)
  if (!intern) {
    error.value = 'Select an intern who has completed the required hours.'
    return
  }

  const confirmed = showConfirm
    ? await showConfirm(`Generate a Certificate of Completion for ${intern.fullName}?`, 'Confirm Generation')
    : window.confirm(`Generate a Certificate of Completion for ${intern.fullName}?`)
  if (!confirmed) return

  notice.value = ''
  error.value = ''
  try {
    await hrAttendanceAPI.logGeneratedDocument({
      documentType: 'Certificate of Completion',
      internId: intern.id,
      internName: intern.fullName
    })
    notice.value = `Certificate of Completion generated for ${intern.fullName}.`
    printCertificate()
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Could not record the certificate generation.')
  }
}

function printCertificate() {
  const style = document.createElement('style')
  style.id = 'coc-print-page'
  const size = form.paper === 'a4' ? 'A4' : 'letter'
  style.textContent = `@page { size: ${size} portrait; margin: 12mm; }`
  document.head.appendChild(style)
  const cleanup = () => {
    style.remove()
    window.removeEventListener('afterprint', cleanup)
  }
  window.addEventListener('afterprint', cleanup)
  window.print()
}

onMounted(() => {
  loadInterns()
  loadSignatories()
})
</script>

<style>
.cocgen-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
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
  max-width: 68ch;
  color: #74655d;
  font-size: 0.88rem;
}

.cocgen-workspace {
  display: grid;
  grid-template-columns: 330px minmax(0, 1fr);
  gap: 1.15rem;
  align-items: start;
}

.cocgen-sidebar > .card {
  overflow: visible;
}

.cocgen-sidebar-head {
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
  flex-direction: column;
  gap: 0.85rem;
  padding: 0.9rem 1rem 1.05rem;
}

.cocgen-form-section + .cocgen-form-section {
  padding-top: 0.85rem;
  border-top: 1px dashed #e9dccd;
}

.cocgen-form-heading {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin: 0 0 0.65rem;
  color: #a85300;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.12em;
  text-transform: uppercase;
  list-style: none;
  cursor: pointer;
}

.cocgen-form-heading::-webkit-details-marker,
.cocgen-form-heading::marker {
  content: '';
  display: none;
}

.cocgen-section-chevron {
  color: #c07a3a;
  font-size: 0.66rem;
  transition: transform 0.2s ease;
}

.cocgen-form-section[open] > .cocgen-form-heading .cocgen-section-chevron {
  transform: rotate(90deg);
}

.cocgen-form .form-label {
  margin-bottom: 0.25rem;
  color: #4a3931;
  font-size: 0.78rem;
  font-weight: 600;
}

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
.cocgen-lookup-option[aria-selected='true'] {
  background: #fff3e4;
}

.cocgen-lookup-option span,
.cocgen-lookup-state,
.cocgen-hint {
  color: #8a7a71;
  font-size: 0.75rem;
}

.cocgen-lookup-state {
  padding: 0.5rem 0.6rem;
}

.cocgen-preview-head {
  display: flex;
  justify-content: space-between;
  margin-bottom: 0.65rem;
}

.cocgen-preview-eyebrow {
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

.cocgen-preview-stage {
  display: flex;
  justify-content: center;
  padding: 1rem;
  overflow: auto;
  border: 1px solid #eadccb;
  border-radius: 1rem;
  background: #f4f1ea;
}

.coc-sheet {
  box-sizing: border-box;
  width: 186mm;
  height: 250mm;
  padding: 14mm 16mm 12mm;
  border: 1.5px solid #111;
  background: #fff;
  color: #111;
  font-family: "Times New Roman", Times, serif;
  zoom: 0.62;
}

.coc-head {
  display: grid;
  grid-template-columns: 22mm 1fr 22mm;
  align-items: center;
}

.coc-seal {
  width: 22mm;
  height: 22mm;
  object-fit: contain;
}

.coc-head-copy {
  text-align: center;
}

.coc-head-copy p {
  margin: 0;
  font-size: 12pt;
  line-height: 1.25;
}

.coc-agency {
  font-size: 14pt !important;
  font-weight: 700;
  letter-spacing: 0.04em;
}

.coc-title {
  margin: 11mm 0 9mm;
  font-size: 18pt;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-align: center;
  text-decoration: underline;
  text-underline-offset: 3px;
}

.coc-body,
.coc-issued {
  margin: 0;
  font-size: 13.5pt;
  line-height: 1.85;
  text-align: justify;
}

.coc-body {
  text-indent: 12mm;
}

.coc-issued {
  margin-top: 6mm;
  text-indent: 12mm;
}

.coc-fill {
  padding: 0 0.5mm;
  font-weight: 700;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.coc-sign {
  margin-top: 16mm;
  text-align: center;
}

.coc-sign p {
  margin: 0;
  font-size: 12.5pt;
  line-height: 1.35;
}

.coc-sign-line {
  width: 68mm;
  height: 12mm;
  margin: 0 auto 1mm;
  border-bottom: 1px solid transparent;
}

.coc-sign-name {
  font-size: 13.5pt !important;
  font-weight: 700;
  letter-spacing: 0.03em;
}

@media (max-width: 1100px) {
  .cocgen-workspace {
    grid-template-columns: 1fr;
  }

  .coc-sheet {
    zoom: 0.48;
  }
}

@media print {
  nav,
  .main-header,
  .main-sidebar,
  .main-footer,
  .sidebar-backdrop,
  .modal,
  .cocgen-toolbar,
  .cocgen-sidebar,
  .cocgen-preview-head,
  .alert,
  .no-print {
    display: none !important;
  }

  html,
  body,
  .app-shell,
  .app-shell > .content-wrapper,
  .shell-content,
  .content-wrapper {
    display: block !important;
    width: auto !important;
    height: auto !important;
    min-height: 0 !important;
    margin: 0 !important;
    padding: 0 !important;
    overflow: visible !important;
    background: #fff !important;
    box-shadow: none !important;
  }

  .cocgen-page,
  .cocgen-workspace,
  .cocgen-preview,
  .cocgen-preview-stage {
    display: block !important;
    width: auto !important;
    margin: 0 !important;
    padding: 0 !important;
    border: 0 !important;
    background: #fff !important;
    overflow: visible !important;
  }

  .coc-sheet {
    width: 100% !important;
    height: 250mm !important;
    max-height: 250mm !important;
    margin: 0 !important;
    zoom: 1 !important;
    box-shadow: none !important;
    break-inside: avoid;
    page-break-inside: avoid;
    page-break-after: avoid;
  }
}
</style>
