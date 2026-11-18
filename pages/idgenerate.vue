<template>
  <section class="idgen-page">
    <div class="idgen-toolbar">
      <div>
        <h2 class="idgen-title">ID Generation</h2>
        <p class="idgen-subtitle">
          Customize the student trainee details, then print the foldable badge: front and back sit side by side,
          so cut along the outer border and fold down the center line.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" :disabled="saving" @click="resetForm">
          <i class="fas fa-rotate-left me-1"></i> Reset
        </button>
        <button type="button" class="btn btn-outline-secondary" :disabled="saving" @click="printCard">
          <i class="fas fa-print me-1"></i> Print Preview
        </button>
        <button
          type="button"
          class="btn btn-primary"
          :disabled="saving || !selectedInternId"
          :aria-busy="saving"
          title="Save this ID to the intern's document history and print it"
          @click="generateId"
        >
          <span v-if="saving" class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>
          <i v-else class="far fa-id-card me-1"></i>
          {{ saving ? 'Saving…' : (selectedIdHistory ? 'Regenerate ID' : 'Generate ID') }}
        </button>
      </div>
    </div>

    <div v-if="notice" class="alert alert-success mb-0">{{ notice }}</div>
    <div v-if="error" class="alert alert-danger mb-0 d-flex justify-content-between align-items-center">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-danger" @click="error = ''">Dismiss</button>
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
                        <span v-if="idHistoryFor(intern)" class="idgen-lookup-badge">
                          <i class="fas fa-check-circle me-1"></i>
                          Already generated
                          <template v-if="issuedOn(idHistoryFor(intern))">&middot; {{ issuedOn(idHistoryFor(intern)) }}</template>
                          &middot; Regenerate
                        </span>
                      </button>
                    </li>
                  </template>
                </ul>
              </div>
              <p class="idgen-hint mb-0">Pick an intern to fill the card details automatically.</p>
              <p v-if="selectedInternId && !selectedIdHistory" class="idgen-hint mb-0">
                <i class="fas fa-info-circle me-1"></i>
                No ID has been generated for this intern yet.
              </p>
              <p v-if="selectedIdHistory" class="idgen-generated-hint mb-0">
                <i class="fas fa-circle-check me-1"></i>
                An ID was already generated for this intern on
                {{ issuedOn(selectedIdHistory) || 'a previous date' }}.
                Generating again adds a new issuance record.
              </p>
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
                <select id="idgenSignatoryOffice" v-model="form.signatoryOffice" class="form-select">
                  <option value="">Select Office / Division</option>
                  <option
                    v-if="form.signatoryOffice && !officeOptions.includes(form.signatoryOffice)"
                    :value="form.signatoryOffice"
                  >
                    {{ form.signatoryOffice }}
                  </option>
                  <option v-for="office in officeOptions" :key="office" :value="office">
                    {{ office }}
                  </option>
                </select>
                <p class="idgen-hint mb-0 mt-1">Auto-selected from the intern's assigned office; change it if needed.</p>
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
                <label for="idgenAutoQr" class="form-check-label">Generate signed attendance QR</label>
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
                <template v-if="selectedInternId && !attendanceToken">
                  No ID has been generated for this intern yet. A unique attendance QR is issued when you click Generate ID.
                </template>
                <template v-else>
                  Each generated ID gets its own random attendance code. Generating a new ID cancels the previous card.
                </template>
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
              <i class="fas fa-ruler-combined me-1"></i> 176 mm &times; 125 mm foldable &middot; A4 landscape &middot; fold on the dashed center line
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
                  <div v-else class="idgen-qr-empty" aria-hidden="true"></div>
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
import '../assets/css/idgenerate.css'
import { computed, inject, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import QRCode from 'qrcode'
import { getApiErrorMessage, hrAttendanceAPI, internsAPI } from '@/services/api'
import { DOC_TYPES, issuedOn, useIssuanceHistory } from '@/utils/documents'
/* Same shared office list that feeds the Signatories module dropdown. */
import { matchOffice, officeOptions } from '@/utils/offices'

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
  signatoryOffice: 'HRMO - Human Resource Management Office'
}

const form = reactive({ ...defaultForm })
const photoUrl = ref('')
const signatureUrl = ref('')
const photoError = ref('')
const signatureError = ref('')
const autoQr = ref(true)
const attendanceToken = ref('')
const qrValue = ref('')
const qrDataUrl = ref('')
const qrError = ref('')
const saving = ref(false)
const notice = ref('')
const error = ref('')

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

/* Issuance history for the "Already generated / Regenerate" labels. */
const {
  historyFor: idHistoryFor,
  selected: selectedIdHistory,
  load: loadGeneratedDocuments
} = useIssuanceHistory(DOC_TYPES.ID, selectedInternId)

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

let internsLoadPromise = null

function loadInterns() {
  if (interns.value.length || internsLoadPromise) return internsLoadPromise

  internsLoading.value = true
  internError.value = ''

  internsLoadPromise = (async () => {
    try {
      interns.value = await internsAPI.getAll()
    } catch (error) {
      internError.value = getApiErrorMessage(error, 'Could not load interns.')
      /* If loading failed, allow a retry on the next call. */
      internsLoadPromise = null
    } finally {
      internsLoading.value = false
    }
  })()

  return internsLoadPromise
}

function openInternSearch() {
  internSearchOpen.value = true
  loadInterns()
  loadGeneratedDocuments()
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
async function applyIntern(intern) {
  selectedInternId.value = intern.id
  internQuery.value = intern.fullName || intern.applicantNo || `#${intern.id}`
  internSearchOpen.value = false
  notice.value = ''
  error.value = ''
  attendanceToken.value = ''
  try {
    const qr = await internsAPI.attendanceQr(intern.id)
    attendanceToken.value = qr.payload || ''
  } catch (error) {
    qrError.value = getApiErrorMessage(error, 'Could not load the attendance QR.')
  }

  Object.assign(form, defaultForm)
  form.fullName = intern.fullName || form.fullName
  form.office = intern.applicantOffice || intern.requestOffice || form.office
  /* Auto-select the intern's assigned office in the Office / Division
     dropdown; it stays editable and the default is kept when no match. */
  form.signatoryOffice =
    matchOffice(intern.applicantOffice) || matchOffice(intern.requestOffice) || form.signatoryOffice
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

const defaultQrValue = computed(() => (autoQr.value ? attendanceToken.value : ''))

/* Until an attendance code is issued (on Generate ID), the card still shows a
   scannable QR built from the intern's ID details. It identifies the card but
   is not accepted by the attendance scanner, which needs the signed code. */
const verificationQrValue = computed(() => {
  const idNumber = String(form.idNumber || '').trim()
  if (!idNumber) return ''
  const name = String(form.fullName || '').trim()
  return name ? `OJT-MIS ID No. ${idNumber} | ${name}` : `OJT-MIS ID No. ${idNumber}`
})

const qrPayload = computed(() => {
  const value = autoQr.value ? defaultQrValue.value : qrValue.value.trim()
  return value || verificationQrValue.value
})

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
      width: 600,
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

/* Deep link (for example, from a notification): ?internId=12 selects
   that intern in the search box once the list has loaded. */
function routeInternId() {
  try {
    const params = new URLSearchParams(window.location.search)
    const id = Number(params.get('internId'))
    return Number.isFinite(id) && id > 0 ? id : null
  } catch {
    return null
  }
}

async function applyRouteIntern(id) {
  if (!id) return
  await loadInterns()
  const intern = interns.value.find((item) => Number(item.id) === Number(id))
  if (intern) applyIntern(intern)
}

function onIdInternEvent(event) {
  const id = Number(event?.detail?.internId)
  if (Number.isFinite(id) && id > 0) applyRouteIntern(id)
}

onMounted(() => {
  window.addEventListener('ojt:id-intern', onIdInternEvent)
  loadGeneratedDocuments()
  applyRouteIntern(routeInternId())
})

onBeforeUnmount(() => {
  qrRequest += 1
  window.removeEventListener('ojt:id-intern', onIdInternEvent)
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
  selectedInternId.value = null
  internQuery.value = ''
  attendanceToken.value = ''
  notice.value = ''
  error.value = ''
}

const idPayload = computed(() => ({
  documentType: 'ID',
  internId: selectedInternId.value ? Number(selectedInternId.value) : null,
  internName: (form.fullName || '').trim(),
  idNumber: form.idNumber,
  office: form.office,
  school: form.schoolName,
  schoolAddress: form.schoolAddress,
  dateIssued: form.dateIssued || null,
  dateStarted: form.dateStarted || null,
  validUntil: form.validUntil || null,
  emergencyContactName: form.emergencyName,
  emergencyContactNumber: form.emergencyNumber,
  emergencyContactAddress: form.emergencyAddress,
  signatoryName: form.signatoryName,
  signatoryPosition: form.signatoryPosition,
  signatoryOffice: form.signatoryOffice
}))

async function generateId() {
  const internId = Number(selectedInternId.value)
  if (!Number.isFinite(internId) || internId <= 0) {
    error.value = 'Select an intern first so the ID can be saved to their document history.'
    return
  }

  const internName = (form.fullName || '').trim()
  if (!internName) {
    error.value = 'Enter the intern full name before generating the ID.'
    return
  }

  const message = selectedIdHistory.value
    ? `An ID was already generated for ${internName}. Generate a new ID issuance? The QR on the previous card will stop working for attendance.`
    : `Generate the ID for ${internName} and save it to the intern's document history?`
  const confirmed = dialog?.showConfirm
    ? await dialog.showConfirm(message, selectedIdHistory.value ? 'Regenerate ID' : 'Generate ID')
    : window.confirm(message)
  if (!confirmed) return

  notice.value = ''
  error.value = ''
  saving.value = true
  try {
    /* Every generated ID gets a fresh unique attendance code; this cancels
       any card printed earlier for the same intern. */
    if (autoQr.value) {
      const issued = await internsAPI.issueAttendanceQr(internId)
      attendanceToken.value = issued.payload || ''
      await renderQr(qrPayload.value)
    }

    await hrAttendanceAPI.logGeneratedDocument(idPayload.value)
    notice.value = `ID for ${internName} was saved to the intern's document history.`
    await loadGeneratedDocuments()
    await printCard()
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Could not save the generated ID. Please try again.')
  } finally {
    saving.value = false
  }
}

async function printCard() {
  /* Make sure the preview QR is ready before the browser snapshots the page. */
  if (qrPayload.value && !qrDataUrl.value) {
    await renderQr(qrPayload.value)
  }

  window.print()
}
</script>
