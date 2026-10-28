<template>
  <section class="idgen-page">
    <div class="idgen-toolbar">
      <div>
        <h2 class="idgen-title">ID Generation</h2>
        <p class="idgen-subtitle">
          Customize the student trainee details, then print the front and back on separate 88&nbsp;mm &times; 125&nbsp;mm cards.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" @click="resetForm">
          <i class="fas fa-rotate-left me-1"></i> Reset
        </button>
        <button type="button" class="btn btn-primary" @click="printCard">
          <i class="fas fa-print me-1"></i> Print ID
        </button>
      </div>
    </div>

    <div class="idgen-workspace">
      <!-- ================= EDITOR ================= -->
      <aside class="idgen-sidebar">
        <div class="card shadow-sm">
          <div class="card-header idgen-sidebar-head">
            <h3 class="card-title mb-0"><i class="far fa-id-card me-2"></i>Card Details</h3>
          </div>

          <div class="card-body idgen-form">
            <div class="idgen-lookup">
              <label for="idgenInternSearch" class="form-label">Find Intern</label>
              <div class="idgen-lookup-field" @focusout="onInternFocusOut">
                <i class="fas fa-search" aria-hidden="true"></i>
                <input
                  id="idgenInternSearch"
                  v-model="internQuery"
                  type="search"
                  class="form-control"
                  placeholder="Search by name or ID number"
                  autocomplete="off"
                  role="combobox"
                  aria-autocomplete="list"
                  :aria-expanded="internSearchOpen"
                  aria-controls="idgenInternResults"
                  @focus="openInternSearch"
                  @input="onInternInput"
                  @keydown.esc="internSearchOpen = false"
                />
                <ul
                  v-if="internSearchOpen"
                  id="idgenInternResults"
                  class="idgen-lookup-results"
                  role="listbox"
                >
                  <li v-if="internsLoading" class="idgen-lookup-state">Loading interns…</li>
                  <li v-else-if="internError" class="idgen-lookup-state text-danger">{{ internError }}</li>
                  <li v-else-if="!internMatches.length" class="idgen-lookup-state">No interns found.</li>
                  <template v-else>
                    <li v-for="intern in internMatches" :key="intern.id">
                      <button
                        type="button"
                        class="idgen-lookup-option"
                        role="option"
                        :aria-selected="selectedInternId === intern.id"
                        @mousedown.prevent
                        @click="applyIntern(intern)"
                      >
                        <strong>{{ intern.fullName }}</strong>
                        <span>
                          {{ intern.applicantNo || `#${intern.id}` }}
                          &middot; {{ intern.applicantOffice || intern.school || '—' }}
                        </span>
                      </button>
                    </li>
                  </template>
                </ul>
              </div>
              <p class="idgen-hint mb-0">Pick an intern to fill the card details automatically.</p>
            </div>

            <details class="idgen-form-section" open>
              <summary class="idgen-form-heading">
                <span>Personal</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="idgenFullName" class="form-label">Full Name</label>
                <input
                  id="idgenFullName"
                  v-model="form.fullName"
                  type="text"
                  class="form-control"
                  maxlength="60"
                  placeholder="LOVELY ROSE L. ANDAYA"
                />
              </div>

              <div class="mb-3">
                <label for="idgenOffice" class="form-label">Office / Department</label>
                <select id="idgenOffice" v-model="form.office" class="form-select">
                  <option
                    v-if="form.office && !officeOptions.includes(form.office)"
                    :value="form.office"
                  >
                    {{ form.office }}
                  </option>
                  <option v-for="office in officeOptions" :key="office" :value="office">
                    {{ office }}
                  </option>
                </select>
              </div>

              <div class="mb-3">
                <label for="idgenNumber" class="form-label">ID Number</label>
                <input
                  id="idgenNumber"
                  v-model="form.idNumber"
                  type="text"
                  class="form-control"
                  maxlength="30"
                  placeholder="2026-007"
                />
              </div>

              <div class="idgen-media">
                <div class="idgen-media-thumb">
                  <img v-if="photoUrl" :src="photoUrl" alt="Trainee photo preview" />
                  <i v-else class="far fa-user"></i>
                </div>
                <div class="idgen-media-body">
                  <div class="d-flex flex-wrap gap-2">
                    <label class="btn btn-sm btn-outline-primary mb-0">
                      <i class="fas fa-camera me-1"></i>
                      {{ photoUrl ? 'Change Photo' : 'Upload Photo' }}
                      <input type="file" accept="image/*" class="d-none" @change="handlePhotoChange" />
                    </label>
                    <button
                      v-if="photoUrl"
                      type="button"
                      class="btn btn-sm btn-outline-secondary"
                      @click="photoUrl = ''"
                    >
                      Remove
                    </button>
                  </div>
                  <p class="idgen-hint mb-0">JPG or PNG, up to 5 MB. Cropped to a square photo box.</p>
                  <p v-if="photoError" class="text-danger small mb-0 mt-1">{{ photoError }}</p>
                </div>
              </div>
            </details>

            <details class="idgen-form-section" open>
              <summary class="idgen-form-heading">
                <span>Dates</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="row g-2">
                <div class="col-6">
                  <label for="idgenStarted" class="form-label">Date Started</label>
                  <input id="idgenStarted" v-model="form.dateStarted" type="date" class="form-control" />
                </div>
                <div class="col-6">
                  <label for="idgenValid" class="form-label">Valid Until</label>
                  <input id="idgenValid" v-model="form.validUntil" type="date" class="form-control" />
                </div>
                <div class="col-12">
                  <label for="idgenIssued" class="form-label">Date Issued</label>
                  <input id="idgenIssued" v-model="form.dateIssued" type="date" class="form-control" />
                </div>
              </div>
            </details>

            <details class="idgen-form-section">
              <summary class="idgen-form-heading">
                <span>School</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="idgenSchoolName" class="form-label">School Name</label>
                <input
                  id="idgenSchoolName"
                  v-model="form.schoolName"
                  type="text"
                  class="form-control"
                  maxlength="80"
                  placeholder="COMMONWEALTH HIGH SCHOOL"
                />
              </div>

              <div class="mb-0">
                <label for="idgenSchoolAddress" class="form-label">School Address</label>
                <input
                  id="idgenSchoolAddress"
                  v-model="form.schoolAddress"
                  type="text"
                  class="form-control"
                  maxlength="120"
                  placeholder="ECOLS ST., BRGY. COMMONWEALTH, QUEZON CITY"
                />
              </div>
            </details>

            <details class="idgen-form-section" open>
              <summary class="idgen-form-heading">
                <span>Emergency</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="idgenEmergencyName" class="form-label">Contact Name</label>
                <input
                  id="idgenEmergencyName"
                  v-model="form.emergencyName"
                  type="text"
                  class="form-control"
                  maxlength="60"
                  placeholder="MARYROSE L. ANDAYA"
                />
              </div>

              <div class="mb-3">
                <label for="idgenEmergencyAddress" class="form-label">Contact Address</label>
                <input
                  id="idgenEmergencyAddress"
                  v-model="form.emergencyAddress"
                  type="text"
                  class="form-control"
                  maxlength="140"
                  placeholder="239 DON FABIAN ST. BRGY. COMMONWEALTH, QUEZON CITY"
                />
              </div>

              <div class="mb-0">
                <label for="idgenEmergencyNumber" class="form-label">Contact Number</label>
                <input
                  id="idgenEmergencyNumber"
                  v-model="form.emergencyNumber"
                  type="tel"
                  class="form-control"
                  maxlength="30"
                  placeholder="0906 921 1834"
                />
              </div>
            </details>

            <details class="idgen-form-section">
              <summary class="idgen-form-heading">
                <span>Authorized Signatory</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="mb-3">
                <label for="idgenSignatoryName" class="form-label">Signatory Name</label>
                <input
                  id="idgenSignatoryName"
                  v-model="form.signatoryName"
                  type="text"
                  class="form-control"
                  maxlength="60"
                  placeholder="RODULFO A. DUZON, JR"
                />
              </div>

              <div class="mb-3">
                <label for="idgenSignatoryPosition" class="form-label">Position</label>
                <input
                  id="idgenSignatoryPosition"
                  v-model="form.signatoryPosition"
                  type="text"
                  class="form-control"
                  maxlength="60"
                  placeholder="Administrative Officer V"
                />
              </div>

              <div class="mb-0">
                <label for="idgenSignatoryOffice" class="form-label">Office / Division</label>
                <input
                  id="idgenSignatoryOffice"
                  v-model="form.signatoryOffice"
                  type="text"
                  class="form-control"
                  maxlength="60"
                  placeholder="HRMO-HRDMID"
                />
              </div>
            </details>

            <details class="idgen-form-section">
              <summary class="idgen-form-heading">
                <span>Signature &amp; QR</span>
                <i class="fas fa-chevron-right idgen-section-chevron" aria-hidden="true"></i>
              </summary>

              <div class="idgen-media idgen-media--signature">
                <div class="idgen-signature-thumb">
                  <img v-if="signatureUrl" :src="signatureUrl" alt="Trainee signature preview" />
                  <span v-else>Signature</span>
                </div>
                <div class="idgen-media-body">
                  <div class="d-flex flex-wrap gap-2">
                    <label class="btn btn-sm btn-outline-primary mb-0">
                      <i class="fas fa-pen-nib me-1"></i>
                      {{ signatureUrl ? 'Change Signature' : 'Upload Signature' }}
                      <input type="file" accept="image/*" class="d-none" @change="handleSignatureChange" />
                    </label>
                    <button
                      v-if="signatureUrl"
                      type="button"
                      class="btn btn-sm btn-outline-secondary"
                      @click="signatureUrl = ''"
                    >
                      Remove
                    </button>
                  </div>
                  <p class="idgen-hint mb-0">Optional PNG or JPG, up to 2 MB. A signature line is printed when empty.</p>
                  <p v-if="signatureError" class="text-danger small mb-0 mt-1">{{ signatureError }}</p>
                </div>
              </div>

              <div class="form-check form-switch idgen-switch">
                <input
                  id="idgenAutoQr"
                  v-model="autoQr"
                  class="form-check-input"
                  type="checkbox"
                  role="switch"
                />
                <label for="idgenAutoQr" class="form-check-label">Generate QR from ID details</label>
              </div>

              <label for="idgenQrValue" class="form-label">QR Content</label>
              <textarea
                id="idgenQrValue"
                v-model="qrValue"
                class="form-control form-control-sm"
                rows="3"
                maxlength="300"
                :disabled="autoQr"
              ></textarea>
              <p v-if="qrError" class="text-danger small mb-0 mt-1">{{ qrError }}</p>
              <p v-else class="idgen-hint mb-0 mt-1">
                Scans to a verification summary of the ID number, name, office, and validity.
              </p>
            </details>
          </div>
        </div>
      </aside>

      <!-- ================= PREVIEW ================= -->
      <div class="idgen-preview">
        <div class="idgen-preview-head">
          <div>
            <span class="idgen-preview-eyebrow">Live Preview</span>
            <p class="idgen-preview-note mb-0">
              <i class="fas fa-ruler-combined me-1"></i> 88 mm &times; 125 mm &middot; front and back print on separate pages
            </p>
          </div>
        </div>

        <div class="idgen-preview-stage">
          <div class="idgen-cards">
            <div class="idgen-card-wrap">
              <span class="idgen-card-tag">Front</span>

            <article class="idgen-card idgen-card--front" aria-label="Student trainee ID front">
              <header class="idgen-front-head">
                <div class="idgen-seal" aria-hidden="true">
                  <img src="/assets/images/logo.png" alt="COA Logo" />
                </div>
                <div class="idgen-head-copy">
                  <span class="idgen-republic">Republic of the Philippines</span>
                  <strong class="idgen-agency">COMMISSION ON AUDIT</strong>
                  <span class="idgen-agency-address">Commonwealth Ave., Quezon City</span>
                </div>
              </header>

              <div class="idgen-bar idgen-bar--title">STUDENT TRAINEE</div>

              <div class="idgen-front-body">
                <div class="idgen-photo">
                  <img v-if="photoUrl" :src="photoUrl" alt="Trainee portrait" />
                </div>

                <div class="idgen-details">
                  <h2 class="idgen-name">{{ form.fullName || 'STUDENT NAME' }}</h2>

                  <p class="idgen-meta">
                    <span>Date Started</span>
                    <strong>{{ formattedDates.started }}</strong>
                  </p>
                  <p class="idgen-meta">
                    <span>Valid Until</span>
                    <strong>{{ formattedDates.validUntil }}</strong>
                  </p>
                  <p class="idgen-meta">
                    <span>Date Issued</span>
                    <strong>{{ formattedDates.issued }}</strong>
                  </p>
                </div>
              </div>

              <div class="idgen-signature">
                <img v-if="signatureUrl" :src="signatureUrl" alt="Trainee signature" />
                <span>Trainee Signature</span>
              </div>

              <div class="idgen-bar idgen-bar--office">{{ form.office || 'OFFICE / DEPARTMENT' }}</div>

              <div class="idgen-qr-block">
                <div class="idgen-qr-frame">
                  <img v-if="qrDataUrl" :src="qrDataUrl" alt="ID verification QR code" />
                  <div v-else class="idgen-qr-empty">QR</div>
                </div>
              </div>

              <footer class="idgen-card-footer">
                <span>No. {{ form.idNumber || '—' }}</span>
              </footer>
            </article>
          </div>

          <div class="idgen-card-wrap">
            <span class="idgen-card-tag">Back</span>

            <article class="idgen-card idgen-card--back" aria-label="Student trainee ID back">
              <div class="idgen-back-inner">
                <div class="idgen-back-top">
                  <div class="idgen-bar idgen-bar--title">SCHOOL INFORMATION</div>

                  <div class="idgen-back-school">
                    <strong>{{ form.schoolName || 'SCHOOL NAME' }}</strong>
                    <span>{{ form.schoolAddress || 'SCHOOL ADDRESS' }}</span>
                  </div>

                  <div class="idgen-back-section">
                    <div class="idgen-back-section-title">IN CASE OF EMERGENCY, PLEASE NOTIFY</div>

                    <dl class="idgen-back-rows">
                      <div class="idgen-back-row">
                        <dt>Name</dt>
                        <dd>{{ form.emergencyName || '—' }}</dd>
                      </div>
                      <div class="idgen-back-row">
                        <dt>Address</dt>
                        <dd>{{ form.emergencyAddress || '—' }}</dd>
                      </div>
                      <div class="idgen-back-row">
                        <dt>Contact No</dt>
                        <dd>{{ form.emergencyNumber || '—' }}</dd>
                      </div>
                    </dl>
                  </div>
                </div>

                <div class="idgen-back-bottom">
                  <div class="idgen-back-signatory">
                    <strong>{{ form.signatoryName || 'AUTHORIZED SIGNATORY' }}</strong>
                    <span>{{ form.signatoryPosition || 'POSITION' }}</span>
                    <span>{{ form.signatoryOffice || 'OFFICE / DIVISION' }}</span>
                  </div>

                  <footer class="idgen-back-footer">
                    If found, please return to Human Resource Database Management<br />
                    Information Division (HRDMID), HRMO or call 02 8951 0905
                  </footer>
                </div>
              </div>
            </article>
          </div>
        </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, inject, onBeforeUnmount, reactive, ref, watch } from 'vue'
import QRCode from 'qrcode'
import officesData from '../assets/json/offices.json'
import { getApiErrorMessage, internsAPI } from '@/services/api'

/* Same shared office list that feeds the Signatories module dropdown. */
const officeOptions = [
  ...new Set((officesData.offices || []).map((office) => office.name).filter(Boolean))
]

const defaultForm = {
  fullName: 'LOVELY ROSE L. ANDAYA',
  office: 'AS-INFORMATION TECHNOLOGY OFFICE',
  idNumber: '2026-007',
  dateStarted: '2026-01-26',
  validUntil: '2026-02-26',
  dateIssued: '2026-01-26',
  schoolName: 'COMMONWEALTH HIGH SCHOOL',
  schoolAddress: 'ECOLS ST. BRGY COMMONWEALTH, QUEZON CITY',
  emergencyName: 'MARYROSE L. ANDAYA',
  emergencyAddress: '239 DON FABIAN ST. BRGY. COMMONWEALTH, QUEZON CITY',
  emergencyNumber: '0906 921 1834',
  signatoryName: 'RODULFO A. DUZON, JR',
  signatoryPosition: 'Administrative Officer V',
  signatoryOffice: 'HRMO-HRDMID'
}

const form = reactive({ ...defaultForm })
const photoUrl = ref('')
const signatureUrl = ref('')
const photoError = ref('')
const signatureError = ref('')
const autoQr = ref(true)
const qrValue = ref('')
const qrDataUrl = ref('')
const qrError = ref('')

const dialog = inject('dialog', null)

function formatDate(value) {
  if (!value) return '\u2014'

  const date = new Date(`${value}T00:00:00`)
  if (Number.isNaN(date.getTime())) return value

  return date.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' })
}

const formattedDates = computed(() => ({
  started: formatDate(form.dateStarted),
  validUntil: formatDate(form.validUntil),
  issued: formatDate(form.dateIssued)
}))

/* ---------------- Intern autocomplete ---------------- */
const interns = ref([])
const internsLoading = ref(false)
const internError = ref('')
const internQuery = ref('')
const internSearchOpen = ref(false)
const selectedInternId = ref(null)

const internMatches = computed(() => {
  const query = internQuery.value.trim().toLowerCase()

  if (!query) return interns.value.slice(0, 8)

  return interns.value
    .filter((intern) =>
      [intern.fullName, intern.applicantNo, intern.id]
        .filter((value) => value !== null && value !== undefined && value !== '')
        .some((value) => String(value).toLowerCase().includes(query))
    )
    .slice(0, 8)
})

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

/* The interns table has no photo/signature column, so those stay manual and
   are cleared to avoid showing the previous intern's images. */
function applyIntern(intern) {
  selectedInternId.value = intern.id
  internQuery.value = intern.fullName || intern.applicantNo || `#${intern.id}`
  internSearchOpen.value = false

  Object.assign(form, defaultForm)
  form.fullName = intern.fullName || form.fullName
  form.office = intern.applicantOffice || intern.requestOffice || form.office
  form.idNumber = intern.applicantNo || String(intern.id)
  form.dateStarted = toDateInput(intern.startDate) || form.dateStarted
  form.validUntil = toDateInput(intern.endDate) || form.validUntil
  form.dateIssued = toDateInput(intern.hireDate) || form.dateIssued
  form.schoolName = intern.schoolName || intern.school || form.schoolName
  form.emergencyName = intern.guardianName || form.emergencyName
  form.emergencyNumber = intern.guardianContact || intern.contactNumber || form.emergencyNumber
  form.emergencyAddress = intern.houseAddress || form.emergencyAddress

  photoUrl.value = ''
  signatureUrl.value = ''
  photoError.value = ''
  signatureError.value = ''
}

const defaultQrValue = computed(() =>
  [
    'OJT-MIS STUDENT TRAINEE ID',
    `ID No.: ${form.idNumber || 'N/A'}`,
    `Name: ${form.fullName || 'N/A'}`,
    `Office: ${form.office || 'N/A'}`,
    `Valid Until: ${formattedDates.value.validUntil}`
  ].join('\n')
)

const qrPayload = computed(() => (autoQr.value ? defaultQrValue.value : qrValue.value.trim()))

/* Keep the editable QR text in sync while auto-generation is enabled. */
watch(
  defaultQrValue,
  (value) => {
    if (autoQr.value) qrValue.value = value
  },
  { immediate: true }
)

/* Re-enabling auto mode refreshes the textarea with the current details. */
watch(autoQr, (enabled) => {
  if (enabled) qrValue.value = defaultQrValue.value
})

let qrRequest = 0

async function renderQr(value) {
  const request = ++qrRequest

  if (!value) {
    qrDataUrl.value = ''
    qrError.value = ''
    return
  }

  try {
    const dataUrl = await QRCode.toDataURL(value, {
      margin: 0,
      width: 320,
      errorCorrectionLevel: 'M',
      color: { dark: '#201714', light: '#FFFFFF' }
    })

    if (request === qrRequest) {
      qrDataUrl.value = dataUrl
      qrError.value = ''
    }
  } catch (error) {
    if (request === qrRequest) {
      qrDataUrl.value = ''
      qrError.value = 'Could not generate the QR code. Try shorter content.'
    }
  }
}

watch(qrPayload, renderQr, { immediate: true })

onBeforeUnmount(() => {
  qrRequest += 1
})

function loadImage(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = () => {
      const image = new Image()
      image.onload = () => resolve(image)
      image.onerror = () => reject(new Error('Could not read that image.'))
      image.src = reader.result
    }

    reader.onerror = () => reject(new Error('Could not read the selected file.'))
    reader.readAsDataURL(file)
  })
}

async function cropImage(file, targetWidth, targetHeight) {
  const image = await loadImage(file)
  const canvas = document.createElement('canvas')
  canvas.width = targetWidth
  canvas.height = targetHeight

  const context = canvas.getContext('2d')
  if (!context) throw new Error('Could not process that image.')

  const scale = Math.max(targetWidth / image.width, targetHeight / image.height)
  const width = image.width * scale
  const height = image.height * scale

  context.drawImage(image, (targetWidth - width) / 2, (targetHeight - height) / 2, width, height)
  return canvas.toDataURL('image/jpeg', 0.9)
}

async function fitImage(file, maxWidth, maxHeight) {
  const image = await loadImage(file)
  const scale = Math.min(maxWidth / image.width, maxHeight / image.height, 1)
  const width = Math.max(1, Math.round(image.width * scale))
  const height = Math.max(1, Math.round(image.height * scale))

  const canvas = document.createElement('canvas')
  canvas.width = width
  canvas.height = height

  const context = canvas.getContext('2d')
  if (!context) throw new Error('Could not process that image.')

  context.drawImage(image, 0, 0, width, height)
  return canvas.toDataURL('image/png')
}

function validateImage(file, maxBytes) {
  if (!file) return 'No file selected.'
  if (!file.type.startsWith('image/')) return 'Please choose an image file.'
  if (file.size > maxBytes) return `Image must be ${Math.round(maxBytes / (1024 * 1024))} MB or smaller.`
  return ''
}

async function handlePhotoChange(event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  photoError.value = ''

  const problem = validateImage(file, 5 * 1024 * 1024)
  if (problem) {
    photoError.value = problem
    return
  }

  try {
    photoUrl.value = await cropImage(file, 490, 490)
  } catch (error) {
    photoError.value = error.message || 'Could not read that image.'
  }
}

async function handleSignatureChange(event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''
  signatureError.value = ''

  const problem = validateImage(file, 2 * 1024 * 1024)
  if (problem) {
    signatureError.value = problem
    return
  }

  try {
    signatureUrl.value = await fitImage(file, 600, 240)
  } catch (error) {
    signatureError.value = error.message || 'Could not read that image.'
  }
}

async function resetForm() {
  const message = 'Reset all ID fields to their defaults?'

  if (dialog?.showConfirm) {
    const confirmed = await dialog.showConfirm(message, 'Reset ID')
    if (!confirmed) return
  } else if (!window.confirm(message)) {
    return
  }

  Object.assign(form, defaultForm)
  photoUrl.value = ''
  signatureUrl.value = ''
  photoError.value = ''
  signatureError.value = ''
  autoQr.value = true
}

async function printCard() {
  /* Make sure the preview QR is ready before the browser snapshots the page. */
  if (qrPayload.value && !qrDataUrl.value) {
    await renderQr(qrPayload.value)
  }

  window.print()
}
</script>

<style>
/* =====================================================================
   ID Generation
   Card sizes use physical units so the screen preview matches print.
   All selectors are prefixed with .idgen- to avoid touching the app shell.
   ===================================================================== */
.idgen-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
  overflow: visible;
}

.idgen-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
}

.idgen-title {
  margin: 0;
  color: #1e0f0c;
  font-size: 1.4rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.idgen-subtitle {
  margin: 0.25rem 0 0;
  max-width: 62ch;
  color: #74655d;
  font-size: 0.88rem;
}

.idgen-workspace {
  display: grid;
  grid-template-columns: 330px minmax(0, 1fr);
  gap: 1.15rem;
  align-items: start;
}

.idgen-sidebar,
.idgen-preview {
  position: static;
  min-width: 0;
}

.idgen-sidebar > .card {
  display: flex;
  flex-direction: column;
  overflow: visible;
}

.idgen-sidebar-head {
  flex: 0 0 auto;
  background: #fff7ec;
  border-bottom: 1px solid #f3e4d3;
}

.idgen-sidebar-head .card-title {
  color: #1e0f0c;
  font-size: 1rem;
  font-weight: 800;
}

.idgen-form {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-height: 0;
  gap: 0.85rem;
  padding: 0.9rem 1rem 1.05rem;
  overflow: visible;
}

.idgen-form::-webkit-scrollbar {
  width: 8px;
}

.idgen-form::-webkit-scrollbar-thumb {
  border-radius: 999px;
  background: #e0cdb9;
}

.idgen-form-section + .idgen-form-section {
  padding-top: 0.85rem;
  border-top: 1px dashed #e9dccd;
}

.idgen-form-heading {
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
.idgen-form-heading::-webkit-details-marker {
  display: none;
}

.idgen-form-heading::marker {
  content: '';
}

.idgen-form-heading:hover {
  color: #7d3d00;
}

.idgen-form-heading:focus-visible {
  outline: 2px solid #f26522;
  outline-offset: 4px;
  border-radius: 4px;
}

.idgen-section-chevron {
  flex: 0 0 auto;
  color: #c07a3a;
  font-size: 0.66rem;
  transform: rotate(0deg);
  transition: transform 0.28s cubic-bezier(0.4, 0, 0.2, 1);
}

.idgen-form-section[open] > .idgen-form-heading .idgen-section-chevron {
  transform: rotate(90deg);
}

/* Space only when the section is expanded, so closed rows stay compact. */
.idgen-form-section[open] > .idgen-form-heading {
  margin-bottom: 0.65rem;
}

/* Tighter field rhythm inside the accordion. */
.idgen-form .mb-3 {
  margin-bottom: 0.6rem !important;
}

.idgen-form .form-label {
  margin-bottom: 0.25rem;
  color: #4a3931;
  font-size: 0.78rem;
  font-weight: 600;
}

.idgen-form .form-control,
.idgen-form .form-select {
  padding-top: 0.34rem;
  padding-bottom: 0.34rem;
  font-size: 0.84rem;
}

.idgen-form .form-control {
  padding-right: 0.55rem;
  padding-left: 0.55rem;
}

.idgen-hint {
  margin-top: 0.25rem;
  color: #8a7a71;
  font-size: 0.7rem;
  line-height: 1.35;
}

/* Intern autocomplete */
.idgen-lookup-field {
  position: relative;
}

.idgen-lookup-field > i {
  position: absolute;
  top: 50%;
  left: 0.6rem;
  color: #b98d68;
  font-size: 0.75rem;
  pointer-events: none;
  transform: translateY(-50%);
}

.idgen-lookup-field .form-control {
  padding-left: 1.7rem;
}

.idgen-lookup-results {
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

.idgen-lookup-option {
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

.idgen-lookup-option:hover,
.idgen-lookup-option:focus-visible,
.idgen-lookup-option[aria-selected='true'] {
  background: #fff3e4;
  outline: none;
}

.idgen-lookup-option strong {
  font-size: 0.8rem;
  font-weight: 700;
}

.idgen-lookup-option span {
  color: #8a7a71;
  font-size: 0.68rem;
}

.idgen-lookup-state {
  padding: 0.5rem 0.6rem;
  color: #8a7a71;
  font-size: 0.75rem;
}

.idgen-media {
  display: flex;
  gap: 0.7rem;
  align-items: flex-start;
  padding: 0.65rem;
  border: 1px solid #efe3d6;
  border-radius: 0.75rem;
  background: #fffdf9;
}

.idgen-media-thumb {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 64px;
  height: 80px;
  overflow: hidden;
  border: 1px solid #e5d6c6;
  border-radius: 0.5rem;
  background: #f7eee3;
  color: #b98d68;
  font-size: 1.5rem;
}

.idgen-media-thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.idgen-media-body {
  flex: 1 1 auto;
  min-width: 0;
}

.idgen-media--signature {
  margin-bottom: 0.7rem;
}

.idgen-signature-thumb {
  display: flex;
  flex: 0 0 auto;
  align-items: center;
  justify-content: center;
  width: 96px;
  height: 52px;
  overflow: hidden;
  padding: 0.25rem;
  border: 1px solid #e5d6c6;
  border-radius: 0.5rem;
  background: #fff;
  color: #b7a597;
  font-size: 0.66rem;
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.idgen-signature-thumb img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.idgen-switch {
  margin-bottom: 0.55rem;
}

.idgen-switch .form-check-label {
  color: #4a3931;
  font-size: 0.8rem;
  font-weight: 600;
}

.idgen-preview {
  display: flex;
  flex-direction: column;
  gap: 0.65rem;
  min-width: 0;
}

.idgen-preview-head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.4rem;
}

.idgen-preview-eyebrow {
  display: block;
  color: #1e0f0c;
  font-size: 0.78rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

.idgen-preview-note {
  color: #8a7a71;
  font-size: 0.75rem;
}

.idgen-preview-stage {
  display: block;
  padding: clamp(0.6rem, 1.2vw, 0.9rem);
  border: 1px solid #eadccb;
  border-radius: 1rem;
  background: #f4f1ea;
  overflow: visible;
}

.idgen-cards {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-start;
  justify-content: center;
  gap: clamp(0.6rem, 1vw, 0.9rem);
  width: 100%;
}

.idgen-card-wrap {
  --idgen-card-scale: 1;
  --idgen-card-width: 88mm;
  --idgen-card-height: 125mm;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.45rem;
}

/* Scale the badges from the top so the preview can be enlarged while still
   fitting beside the editor. The margins keep the layout box in sync with the
   scaled box (negative when shrinking, positive when enlarging). */
.idgen-card-wrap > .idgen-card {
  flex: 0 0 auto;
  margin:
    0
    calc((1 - var(--idgen-card-scale)) * var(--idgen-card-width) / -2)
    calc((1 - var(--idgen-card-scale)) * var(--idgen-card-height) * -1);
  transform: scale(var(--idgen-card-scale));
  transform-origin: top center;
}

.idgen-card-tag {
  display: inline-flex;
  align-items: center;
  padding: 0.2rem 0.65rem;
  border-radius: 999px;
  background: #f1e3d2;
  color: #8a541f;
  font-size: 0.66rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  text-transform: uppercase;
}

/* ---------------------------- ID CARD ---------------------------- */
.idgen-card {
  --idgen-ink: #111;
  --idgen-accent: #e87822;
  --idgen-paper: #ffffff;
  position: relative;
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  width: 88mm;
  height: 125mm;
  overflow: hidden;
  border: 1px solid #888;
  border-radius: 0;
  background: #fff;
  color: #000;
  font-family: Arial, Helvetica, sans-serif;
  line-height: 1.2;
  box-shadow: 0 4px 10px rgba(0, 0, 0, 0.12);
  -webkit-print-color-adjust: exact;
  print-color-adjust: exact;
}

.idgen-card--back {
  background: #fff;
}

.idgen-front-head {
  display: flex;
  align-items: center;
  gap: 2.5mm;
  padding: 2.8mm 3.5mm 2.4mm;
  background: var(--idgen-accent);
  color: #fff;
}

.idgen-seal {
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

.idgen-seal img {
  width: 13mm;
  height: 13mm;
  object-fit: contain;
  display: block;
}

.idgen-head-copy {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  min-width: 0;
  text-align: center;
}

.idgen-republic {
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 7pt;
  letter-spacing: 0.02em;
}

.idgen-agency {
  margin: 0.3mm 0;
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 11.2pt;
  font-weight: 700;
  letter-spacing: 0.04em;
  line-height: 1.05;
}

.idgen-agency-address {
  color: #fff;
  font-family: "Times New Roman", Times, serif;
  font-size: 7.5pt;
}

.idgen-bar {
  padding: 1.7mm 3mm;
  text-align: center;
  font-weight: 700;
  text-transform: uppercase;
  line-height: 1.15;
}

.idgen-bar--title {
  background: #1a1a1a;
  color: #fff;
  font-size: 9pt;
  letter-spacing: 0.18em;
}

.idgen-bar--office {
  background: var(--idgen-accent);
  color: #fff;
  font-size: 7pt;
  letter-spacing: 0.08em;
}

.idgen-front-body {
  display: flex;
  align-items: flex-start;
  gap: 4mm;
  padding: 4mm 5mm 1mm;
  background: #fff;
}

.idgen-photo {
  flex: 0 0 30mm;
  width: 30mm;
  height: 30mm;
  overflow: hidden;
  border: 0.25mm solid #000;
  background: #fff;
}

.idgen-photo img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.idgen-details {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  min-width: 0;
}

.idgen-name {
  margin: 0 0 2.4mm;
  color: #000;
  font-size: 11.5pt;
  font-weight: 800;
  line-height: 1.05;
  text-align: center;
  text-transform: uppercase;
}

.idgen-meta {
  display: flex;
  align-items: baseline;
  justify-content: space-between;
  gap: 3mm;
  margin: 0 0 1.1mm;
  line-height: 1.2;
}

.idgen-meta span,
.idgen-meta strong {
  color: #000;
  font-size: 7pt;
  font-weight: 400;
}

.idgen-meta strong {
  text-align: right;
  white-space: nowrap;
}

.idgen-signature {
  display: flex;
  flex-direction: column;
  align-items: flex-start;
  min-height: 8mm;
  margin: 1mm 0 3.5mm;
  padding: 0 5mm;
  color: #000;
  font-size: 7.5pt;
}

.idgen-signature img {
  max-width: 28mm;
  max-height: 7mm;
  object-fit: contain;
}

.idgen-qr-block {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 3mm 0 2mm;
  background: #fff;
}

.idgen-qr-frame {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 30mm;
  height: 30mm;
  overflow: hidden;
  background: #fff;
}

.idgen-qr-frame img {
  width: 100%;
  height: 100%;
  object-fit: contain;
}

.idgen-qr-empty {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  border: 0.3mm solid #000;
  font-size: 8pt;
  font-weight: 700;
}

.idgen-card-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  margin-top: auto;
  padding: 2.2mm 4mm;
  background: #111;
  color: #fff;
  font-size: 10pt;
  font-weight: 700;
}

/* ---------------------------- BACK SIDE ---------------------------- */
.idgen-watermark {
  position: absolute;
  inset: 0;
  z-index: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2mm;
  opacity: 0.055;
  pointer-events: none;
}

.idgen-watermark-seal {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52mm;
  height: 52mm;
  border: 2.5mm double var(--idgen-ink);
  border-radius: 50%;
  color: var(--idgen-ink);
  font-size: 24pt;
  font-weight: 800;
  letter-spacing: 0.06em;
}

.idgen-watermark-text {
  font-size: 8pt;
  font-weight: 700;
  letter-spacing: 0.2em;
  text-transform: uppercase;
}

.idgen-back-inner {
  position: relative;
  z-index: 2;
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  justify-content: space-between;
  height: 100%;
  min-height: 0;
  background: #fff;
}

.idgen-back-top {
  display: flex;
  flex-direction: column;
}

.idgen-back-bottom {
  display: flex;
  flex-direction: column;
  margin-top: auto;
}

.idgen-back-school {
  padding: 4mm 5mm 5mm;
  text-align: left;
  background: #fff;
}

.idgen-back-school strong {
  display: block;
  margin: 0 0 0.8mm;
  font-size: 9pt;
  font-weight: 800;
  letter-spacing: 0.02em;
  text-transform: uppercase;
}

.idgen-back-school > span:last-child {
  display: block;
  color: #000;
  font-size: 7.5pt;
  line-height: 1.3;
  text-transform: uppercase;
}

.idgen-back-section {
  margin: 0;
  overflow: hidden;
  border: none;
  border-radius: 0;
  background: transparent;
  box-shadow: none;
}

.idgen-back-section-title {
  padding: 1.8mm 3mm;
  background: var(--idgen-accent);
  color: #fff;
  font-size: 7pt;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-align: center;
  text-transform: uppercase;
}

.idgen-back-rows {
  margin: 0;
  padding: 3mm 6mm 4mm;
  background: #fff;
  border-bottom: 0.35mm solid #000;
}

.idgen-back-row {
  display: grid;
  grid-template-columns: 22mm minmax(0, 1fr);
  gap: 3mm;
  align-items: baseline;
  padding: 1.4mm 0;
}

.idgen-back-row dt {
  color: #000;
  font-size: 8pt;
  font-weight: 400;
}

.idgen-back-row dd {
  margin: 0;
  color: #000;
  font-size: 8pt;
  font-weight: 700;
  line-height: 1.3;
  text-transform: uppercase;
  overflow-wrap: anywhere;
}

.idgen-back-verify {
  display: flex;
  flex: 1 1 auto;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  min-height: 0;
  padding: 3mm 0 2mm;
}

.idgen-qr-frame--small {
  width: 22mm;
  height: 22mm;
}

.idgen-back-verify .idgen-qr-caption {
  margin-top: 1.2mm;
}

.idgen-back-signatory {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 0.3mm;
  margin-top: auto;
  padding: 0 8mm 6mm;
  text-align: center;
  background: #fff;
}

.idgen-back-signatory strong {
  font-size: 9pt;
  font-weight: 800;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.idgen-back-signatory span {
  color: #000;
  font-size: 7.5pt;
  line-height: 1.3;
}

.idgen-back-footer {
  padding: 2.2mm 5mm;
  background: #111;
  color: #fff;
  font-size: 6.5pt;
  line-height: 1.3;
  text-align: center;
}

/* ---------------------------- RESPONSIVE ---------------------------- */
/* Below ~1360px the 100%-scale badge pair no longer clears the split view,
   so the panels stack and the preview gets the full page width. */
@media (max-width: 1359.98px) {
  .idgen-workspace {
    grid-template-columns: minmax(0, 1fr);
  }

  /* Stacked layout: let the page scroll normally and show the full form. */
  .idgen-sidebar,
  .idgen-preview {
    position: static;
    top: auto;
  }

  .idgen-sidebar > .card {
    max-height: none;
    overflow: visible;
  }

  .idgen-form {
    overflow-y: visible;
  }
}

@media (max-width: 575.98px) {
  .idgen-toolbar .btn {
    flex: 1 1 auto;
  }

  .idgen-preview-stage {
    padding: 0.85rem;
    border-radius: 0.75rem;
  }
}

/* Large desktop monitors have room for an even bigger preview. */
@media (min-width: 1600px) {
  .idgen-card-wrap {
    --idgen-card-scale: 1.1;
  }
}

@media (min-width: 1920px) {
  .idgen-card-wrap {
    --idgen-card-scale: 1.2;
  }
}

/* ---------------------------- PRINT ---------------------------- */
/* One A4 sheet holds front and back side by side (88 + 8 + 88 = 184 mm).
   The page margin stays 0 so the browser does not reserve space for its
   title/URL header and footer. The 10 mm quiet zone is recreated as padding
   on the print container, which keeps the cards inside the printer's
   non-printable edge. Print at 100% / Default scale with headers & footers off
   if the browser adds them anyway. */
@page {
  size: A4 portrait;
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
  .idgen-toolbar,
  .idgen-sidebar,
  .idgen-preview-head,
  .idgen-card-tag,
  .no-print {
    display: none !important;
  }

  .idgen-page button,
  .idgen-page input,
  .idgen-page textarea,
  .idgen-page select,
  .idgen-page .btn {
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

  /* 10 mm safe area around the cutting guides (A4 = 210 x 297 mm). */
  .idgen-page {
    display: block !important;
    width: auto !important;
    margin: 0 !important;
    padding: 10mm !important;
    background: #fff !important;
  }

  .idgen-workspace,
  .idgen-preview,
  .idgen-preview-stage {
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

  /* Front and back stay on one row: 184 mm inside the 190 mm usable width. */
  .idgen-cards {
    display: flex !important;
    flex-wrap: nowrap !important;
    align-items: flex-start !important;
    justify-content: center !important;
    gap: 8mm !important;
    width: auto !important;
    margin: 0 !important;
    padding: 0 !important;
    border: 0 !important;
    background: #fff !important;
    box-shadow: none !important;
    overflow: visible !important;
  }

  .idgen-card-wrap {
    display: block !important;
    width: 88mm !important;
    margin: 0 !important;
    padding: 0 !important;
    break-inside: avoid;
    page-break-inside: avoid;
  }

  .idgen-card {
    width: 88mm !important;
    height: 125mm !important;
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
  .idgen-card-wrap:last-child {
    break-after: auto;
    page-break-after: auto;
  }
}
</style>
