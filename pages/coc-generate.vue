<template>
  <section class="cocgen-page">
    <div class="cocgen-toolbar">
      <div>
        <h2 class="cocgen-title">Certificate of Completion</h2>
        <p class="cocgen-subtitle">
          Generate a certificate for interns who have rendered all of their required hours.
          The certificate is rendered on one landscape page and emailed to the intern as a PDF.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" :disabled="sending" @click="resetForm">
          <i class="fas fa-rotate-left me-1"></i> Reset
        </button>
        <button type="button" class="btn btn-outline-secondary" :disabled="sending" @click="printCertificate">
          <i class="fas fa-print me-1"></i> Print Preview
        </button>
        <button
          type="button"
          class="btn btn-outline-primary"
          :disabled="!selectedInternId || sending"
          title="Open ID generation with this intern selected"
          @click="openIdGenerator"
        >
          <i class="far fa-id-card me-1"></i> Generate ID
        </button>
        <button
          type="button"
          class="btn btn-primary"
          :disabled="!selectedInternId || sending"
          :aria-busy="sending"
          @click="generateCertificate"
        >
          <span
            v-if="sending"
            class="spinner-border spinner-border-sm me-1"
            role="status"
            aria-hidden="true"
          ></span>
          <i v-else class="fas fa-paper-plane me-1"></i>
          {{ sending ? 'Sending…' : (selectedCocHistory ? 'Regenerate Certificate' : 'Generate Certificate') }}
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
                        <span v-if="cocHistoryFor(intern)" class="cocgen-lookup-badge">
                          <i class="fas fa-check-circle me-1"></i>
                          Already generated
                          <template v-if="issuedOn(cocHistoryFor(intern))">&middot; {{ issuedOn(cocHistoryFor(intern)) }}</template>
                          &middot; Regenerate
                        </span>
                      </button>
                    </li>
                  </template>
                </ul>
              </div>
              <p class="cocgen-hint mb-0">Only interns marked Completed can be selected.</p>
              <p v-if="selectedCocHistory" class="cocgen-generated-hint mb-0">
                <i class="fas fa-circle-check me-1"></i>
                A Certificate of Completion was already generated for this intern on
                {{ issuedOn(selectedCocHistory) || 'a previous date' }}.
                Generating again adds a new issuance record.
              </p>
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
                  <option value="letter">Letter landscape (11 × 8.5 in)</option>
                  <option value="a4">A4 landscape (297 × 210 mm)</option>
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
              Landscape, one Letter or A4 page
            </p>
          </div>
        </div>

        <div class="cocgen-preview-stage">
          <article
            class="coc-sheet"
            :class="{ 'is-a4': form.paper === 'a4' }"
            aria-label="Certificate of Completion preview"
          >
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
import '../assets/css/coc-generate.css'
import { computed, inject, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import coaLogo from '../assets/images/logo.png'
import { getApiErrorMessage, certificateAPI, hrAttendanceAPI, signatoriesAPI } from '@/services/api'
import { go } from '@/utils/navigation'
import { DOC_TYPES, issuedOn, useIssuanceHistory } from '@/utils/documents'

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
  paper: 'a4',
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
const sending = ref(false)
const notice = ref('')
const error = ref('')

/* Issuance history: marks interns whose Certificate of Completion already
   exists and powers the "Already generated / Regenerate" labels. */
const {
  historyFor: cocHistoryFor,
  selected: selectedCocHistory,
  load: loadGeneratedDocuments
} = useIssuanceHistory(DOC_TYPES.COC, selectedInternId)

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

const routeSearch = ref(window.location.search || '')

const routeInternId = computed(() => {
  try {
    const params = new URLSearchParams(routeSearch.value)
    const id = Number(params.get('internId'))
    return Number.isFinite(id) && id > 0 ? id : null
  } catch {
    return null
  }
})

function applyRouteIntern() {
  if (internsLoading.value) return
  const id = routeInternId.value
  if (!id) return
  const intern = interns.value.find((item) => Number(item.id) === id)
  if (intern) {
    applyIntern(intern)
    error.value = ''
  } else if (!error.value) {
    error.value = 'That intern has not completed the required hours, so a certificate cannot be generated.'
  }
}

function syncRoute() {
  routeSearch.value = window.location.search || ''
}

watch([interns, internsLoading, routeInternId], applyRouteIntern)

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
    // The COC dropdown is limited to interns the completion service already
    // marked Completed, so a certificate is only offered at the end of the OJT.
    interns.value = (Array.isArray(data) ? data : []).filter(
      (intern) => String(intern.status).toLowerCase() === 'completed'
    )
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

function openIdGenerator() {
  const internId = Number(selectedInternId.value)
  if (!Number.isFinite(internId) || internId <= 0) {
    error.value = 'Select an intern first so the ID can be linked to their profile.'
    return
  }

  go(`/idgenerate.html?internId=${internId}`)
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
  form.paper = 'a4'
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

  const message = cocHistoryFor(intern)
    ? `A Certificate of Completion was already generated for ${intern.fullName}. Generate a new issuance and email it to the intern's registered address?`
    : `Generate the Certificate of Completion for ${intern.fullName} and email it to the intern's registered address?`
  const confirmed = showConfirm
    ? await showConfirm(message, 'Confirm Generation')
    : window.confirm(message)
  if (!confirmed) return

  notice.value = ''
  error.value = ''
  sending.value = true
  try {
    const result = await certificateAPI.generateAndSend(intern.id, {
      honorific: form.honorific,
      office: form.office,
      signatoryId: selectedSignatoryId.value,
      signatoryName: form.signatoryName,
      signatoryPosition: form.signatoryPosition,
      signatoryOffice: form.signatoryOffice,
      dateIssued: form.dateIssued || null,
      paper: form.paper
    })
    notice.value =
      result?.message || `Certificate of Completion sent to ${intern.fullName}.`
    await loadGeneratedDocuments()
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Could not generate and send the certificate.')
  } finally {
    sending.value = false
  }
}

function printCertificate() {
  const style = document.createElement('style')
  style.id = 'coc-print-page'
  const size = form.paper === 'a4' ? 'A4' : 'letter'
  style.textContent = `@page { size: ${size} landscape; margin: 8mm; }`
  document.head.appendChild(style)
  const cleanup = () => {
    style.remove()
    window.removeEventListener('afterprint', cleanup)
  }
  window.addEventListener('afterprint', cleanup)
  window.print()
}

function onCertificateIntern(event) {
  const id = Number(event.detail?.internId)
  if (!Number.isFinite(id) || id <= 0) return
  const params = new URLSearchParams(routeSearch.value)
  params.set('internId', String(id))
  const next = params.toString()
  routeSearch.value = next ? `?${next}` : ''
}

onMounted(() => {
  window.addEventListener('hashchange', syncRoute)
  window.addEventListener('popstate', syncRoute)
  window.addEventListener('ojt:certificate-intern', onCertificateIntern)
  loadInterns()
  loadSignatories()
  loadGeneratedDocuments()
})

onBeforeUnmount(() => {
  window.removeEventListener('hashchange', syncRoute)
  window.removeEventListener('popstate', syncRoute)
  window.removeEventListener('ojt:certificate-intern', onCertificateIntern)
})
</script>
