<template>
  <section class="docsh-page">
    <div class="docsh-toolbar">
      <div>
        <h2 class="docsh-title">Documents History</h2>
        <p class="docsh-subtitle">
          Every ID card and Certificate of Completion that HR has generated and finalized,
          with the exact details recorded on the intern's profile.
        </p>
      </div>

      <div class="d-flex flex-wrap gap-2">
        <button type="button" class="btn btn-outline-secondary" :disabled="loading" @click="load">
          <i class="fas fa-rotate me-1" :class="{ 'fa-spin': loading }"></i>
          Refresh
        </button>
        <button type="button" class="btn btn-outline-primary" @click="openGenerator('coc-generate.html')">
          <i class="fas fa-certificate me-1"></i> Generate COC
        </button>
        <button type="button" class="btn btn-primary" @click="openGenerator('idgenerate.html')">
          <i class="far fa-id-card me-1"></i> Generate ID
        </button>
      </div>
    </div>

    <div v-if="notice" class="alert alert-success mb-0">{{ notice }}</div>
    <div v-if="error" class="alert alert-danger mb-0 d-flex justify-content-between align-items-center">
      <span>{{ error }}</span>
      <button type="button" class="btn btn-sm btn-outline-danger" :disabled="loading" @click="load">
        Retry
      </button>
    </div>

    <div v-if="loading" class="docsh-loading">
      <div class="spinner-border text-primary" role="status">
        <span class="visually-hidden">Loading documents…</span>
      </div>
    </div>

    <template v-else>
      <div class="docsh-stats">
        <article class="docsh-stat">
          <span class="docsh-stat-label">Total generated</span>
          <strong class="docsh-stat-value">{{ documents.length }}</strong>
        </article>
        <article class="docsh-stat">
          <span class="docsh-stat-label">ID cards</span>
          <strong class="docsh-stat-value">{{ idCount }}</strong>
        </article>
        <article class="docsh-stat">
          <span class="docsh-stat-label">Certificates</span>
          <strong class="docsh-stat-value">{{ cocCount }}</strong>
        </article>
        <article class="docsh-stat">
          <span class="docsh-stat-label">Interns served</span>
          <strong class="docsh-stat-value">{{ internCount }}</strong>
        </article>
      </div>

      <div class="card shadow-sm">
        <div class="card-header docsh-filterbar">
          <div class="docsh-search">
            <i class="fas fa-search" aria-hidden="true"></i>
            <input
              id="docshSearch"
              v-model="query"
              type="search"
              class="form-control"
              placeholder="Search by intern, ID number, office, school, or signatory"
              autocomplete="off"
            />
          </div>

          <select v-model="typeFilter" class="form-select docsh-type-filter" aria-label="Filter by document type">
            <option value="all">All documents</option>
            <option value="ID">ID cards</option>
            <option value="Certificate of Completion">Certificates of Completion</option>
          </select>

          <span class="docsh-count">
            {{ filteredDocuments.length }}
            {{ filteredDocuments.length === 1 ? 'record' : 'records' }}
          </span>
        </div>

        <div class="card-body p-0">
          <div class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col">Document</th>
                  <th scope="col">Intern</th>
                  <th scope="col">Date Issued</th>
                  <th scope="col">Finalized Details</th>
                  <th scope="col">Signatory</th>
                  <th scope="col">Generated</th>
                  <th scope="col" class="text-end">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="doc in filteredDocuments" :key="doc.id">
                  <td>
                    <span class="badge docsh-badge" :class="doc.documentType === 'ID' ? 'docsh-badge--id' : 'docsh-badge--coc'">
                      {{ doc.documentType === 'ID' ? 'ID' : 'COC' }}
                    </span>
                    <span class="docsh-type-label">{{ doc.documentType }}</span>
                  </td>
                  <td>
                    <button
                      v-if="doc.internId"
                      type="button"
                      class="docsh-intern-link"
                      @click="openProfile(doc)"
                    >
                      {{ doc.internName || `Intern #${doc.internId}` }}
                    </button>
                    <span v-else class="docsh-intern-name">{{ doc.internName || 'Unlinked intern' }}</span>
                  </td>
                  <td class="text-nowrap">{{ formatDateOnly(doc.dateIssued) || '—' }}</td>
                  <td class="small">
                    <template v-if="doc.documentType === 'ID'">
                      <span class="docsh-detail-strong">ID No. {{ doc.idNumber || '—' }}</span>
                      <span v-if="doc.office"> &middot; {{ doc.office }}</span>
                      <span v-if="doc.validUntil"> &middot; valid until {{ formatDateOnly(doc.validUntil) }}</span>
                    </template>
                    <template v-else>
                      <span class="docsh-detail-strong">{{ formatHours(doc.hoursRendered) }} of {{ doc.requiredHours || 0 }} hrs</span>
                      <span v-if="doc.recipientEmail"> &middot; emailed to {{ doc.recipientEmail }}</span>
                    </template>
                  </td>
                  <td class="small">{{ doc.signatoryName || '—' }}</td>
                  <td class="small text-nowrap">
                    {{ formatDateTime(doc.createdAt) }}
                    <span v-if="doc.generatedBy" class="docsh-generated-by">by {{ doc.generatedBy }}</span>
                  </td>
                  <td class="text-end text-nowrap">
                    <button type="button" class="btn btn-sm btn-outline-secondary me-1" @click="selected = doc">
                      <i class="fas fa-eye me-1"></i> View
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary"
                      :title="doc.internId ? 'Open the generator with this intern selected' : 'Open the generator'"
                      @click="regenerate(doc)"
                    >
                      <i class="fas fa-rotate-right me-1"></i>
                      {{ doc.internId ? 'Regenerate' : 'Generate' }}
                    </button>
                  </td>
                </tr>
                <tr v-if="filteredDocuments.length === 0">
                  <td colspan="7" class="text-center text-muted py-5">
                    <i class="fas fa-folder-open fa-2x d-block mb-2"></i>
                    <template v-if="documents.length">
                      No documents match the current search or filter.
                    </template>
                    <template v-else>
                      No documents have been generated yet. Generate an ID or Certificate of Completion to start the history.
                    </template>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </template>

    <Teleport to="body">
      <div
        v-if="selected"
        class="docsh-modal-layer"
        role="dialog"
        aria-modal="true"
        aria-label="Generated document details"
        @click.self="selected = null"
      >
        <div class="docsh-modal">
          <div class="docsh-modal-head">
            <div>
              <span class="docsh-modal-eyebrow">Finalized record</span>
              <h3 class="docsh-modal-title">
                {{ selected.documentType === 'ID' ? 'Intern ID Card' : 'Certificate of Completion' }}
              </h3>
            </div>
            <button type="button" class="btn-close" aria-label="Close" @click="selected = null"></button>
          </div>

          <div class="docsh-modal-body">
            <dl class="docsh-detail-grid">
              <div class="docsh-detail-row">
                <dt>Intern</dt>
                <dd>{{ selected.internName || 'Unlinked intern' }}</dd>
              </div>
              <div class="docsh-detail-row">
                <dt>Document type</dt>
                <dd>{{ selected.documentType }}</dd>
              </div>
              <div class="docsh-detail-row">
                <dt>Date issued</dt>
                <dd>{{ formatDateOnly(selected.dateIssued) || '—' }}</dd>
              </div>
              <div class="docsh-detail-row">
                <dt>Office</dt>
                <dd>{{ selected.office || '—' }}</dd>
              </div>
              <div class="docsh-detail-row">
                <dt>School</dt>
                <dd>
                  {{ selected.school || '—' }}
                  <span v-if="selected.schoolAddress" class="docsh-detail-sub">{{ selected.schoolAddress }}</span>
                </dd>
              </div>

              <template v-if="selected.documentType === 'ID'">
                <div class="docsh-detail-row">
                  <dt>ID number</dt>
                  <dd>{{ selected.idNumber || '—' }}</dd>
                </div>
                <div class="docsh-detail-row">
                  <dt>Valid until</dt>
                  <dd>{{ formatDateOnly(selected.validUntil) || '—' }}</dd>
                </div>
                <div class="docsh-detail-row">
                  <dt>Emergency contact</dt>
                  <dd>
                    {{ selected.emergencyContactName || '—' }}
                    <span v-if="selected.emergencyContactNumber" class="docsh-detail-sub">{{ selected.emergencyContactNumber }}</span>
                    <span v-if="selected.emergencyContactAddress" class="docsh-detail-sub">{{ selected.emergencyContactAddress }}</span>
                  </dd>
                </div>
              </template>

              <template v-else>
                <div class="docsh-detail-row">
                  <dt>Hours completed</dt>
                  <dd>{{ formatHours(selected.hoursRendered) }} of {{ selected.requiredHours || 0 }} required hours</dd>
                </div>
                <div class="docsh-detail-row">
                  <dt>Training period</dt>
                  <dd>{{ formatDateOnly(selected.dateStarted) || '—' }} &ndash; {{ formatDateOnly(selected.dateCompleted) || '—' }}</dd>
                </div>
                <div class="docsh-detail-row">
                  <dt>Sent to</dt>
                  <dd>{{ selected.recipientEmail || '—' }}</dd>
                </div>
              </template>

              <div class="docsh-detail-row">
                <dt>Signatory</dt>
                <dd>
                  {{ selected.signatoryName || '—' }}
                  <span v-if="selected.signatoryPosition" class="docsh-detail-sub">{{ selected.signatoryPosition }}</span>
                  <span v-if="selected.signatoryOffice" class="docsh-detail-sub">{{ selected.signatoryOffice }}</span>
                </dd>
              </div>
              <div class="docsh-detail-row">
                <dt>Generated</dt>
                <dd>
                  {{ formatDateTime(selected.createdAt) }}
                  <span v-if="selected.generatedBy" class="docsh-detail-sub">by {{ selected.generatedBy }}</span>
                </dd>
              </div>
            </dl>
          </div>

          <div class="docsh-modal-foot">
            <button
              v-if="selected.internId"
              type="button"
              class="btn btn-outline-secondary"
              @click="openProfile(selected)"
            >
              <i class="fas fa-user me-1"></i> Open Intern Profile
            </button>
            <button type="button" class="btn btn-outline-primary" @click="regenerate(selected)">
              <i class="fas fa-rotate-right me-1"></i> Regenerate
            </button>
            <button type="button" class="btn btn-primary" @click="selected = null">Close</button>
          </div>
        </div>
      </div>
    </Teleport>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { getApiErrorMessage, hrAttendanceAPI } from '@/services/api'

const documents = ref([])
const loading = ref(false)
const error = ref('')
const notice = ref('')
const query = ref('')
const typeFilter = ref('all')
const selected = ref(null)

const filteredDocuments = computed(() => {
  const search = query.value.trim().toLowerCase()
  return documents.value.filter((doc) => {
    if (typeFilter.value !== 'all' && doc.documentType !== typeFilter.value) return false
    if (!search) return true

    return [
      doc.internName,
      doc.idNumber,
      doc.office,
      doc.school,
      doc.schoolAddress,
      doc.signatoryName,
      doc.signatoryPosition,
      doc.signatoryOffice,
      doc.recipientEmail,
      doc.documentType,
      doc.generatedBy
    ]
      .filter((value) => value !== null && value !== undefined && value !== '')
      .some((value) => String(value).toLowerCase().includes(search))
  })
})

const idCount = computed(
  () => documents.value.filter((doc) => doc.documentType === 'ID').length
)
const cocCount = computed(
  () => documents.value.filter((doc) => doc.documentType === 'Certificate of Completion').length
)
const internCount = computed(
  () => new Set(documents.value.map((doc) => doc.internId).filter(Boolean)).size
)

async function load() {
  loading.value = true
  error.value = ''
  notice.value = ''
  try {
    const data = await hrAttendanceAPI.getGeneratedDocuments()
    documents.value = Array.isArray(data) ? data : []
  } catch (e) {
    documents.value = []
    error.value = getApiErrorMessage(e, 'Could not load the documents history.')
  } finally {
    loading.value = false
  }
}

function formatDateOnly(value) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''
  return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
}

function formatDateTime(value) {
  if (!value) return 'Unknown'
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Unknown'
  return date.toLocaleString('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: 'numeric',
    minute: '2-digit'
  })
}

function formatHours(value) {
  const number = Number(value)
  if (!Number.isFinite(number)) return '0'
  return Number.isInteger(number) ? String(number) : number.toFixed(2).replace(/0$/, '').replace(/\.0$/, '')
}

function openGenerator(page, internId = null) {
  const queryString = internId ? `?internId=${internId}` : ''
  window.history.pushState({}, '', `/${page}${queryString}`)
  window.dispatchEvent(new PopStateEvent('popstate'))
}

function openProfile(doc) {
  const internId = Number(doc?.internId)
  if (!Number.isFinite(internId) || internId <= 0) return
  selected.value = null
  openGenerator('internlist.html', internId)
}

function regenerate(doc) {
  const internId = Number(doc?.internId)
  selected.value = null
  openGenerator(
    doc?.documentType === 'ID' ? 'idgenerate.html' : 'coc-generate.html',
    Number.isFinite(internId) && internId > 0 ? internId : null
  )
}

onMounted(load)
</script>

<style>
.docsh-page {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.docsh-toolbar {
  display: flex;
  flex-wrap: wrap;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
}

.docsh-title {
  margin: 0;
  color: #1e0f0c;
  font-size: 1.4rem;
  font-weight: 800;
  letter-spacing: -0.02em;
}

.docsh-subtitle {
  margin: 0.25rem 0 0;
  max-width: 72ch;
  color: #74655d;
  font-size: 0.88rem;
}

.docsh-loading {
  display: flex;
  justify-content: center;
  padding: 3rem 0;
}

.docsh-stats {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 0.85rem;
}

.docsh-stat {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  padding: 0.85rem 1rem;
  border: 1px solid #efe1d0;
  border-radius: 0.85rem;
  background: linear-gradient(145deg, #fffdf9, #fff7ec);
}

.docsh-stat-label {
  color: #8a7a71;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.docsh-stat-value {
  color: #1e0f0c;
  font-size: 1.45rem;
  font-weight: 800;
  line-height: 1.1;
}

.docsh-filterbar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.65rem;
  background: #fff7ec;
  border-bottom: 1px solid #f3e4d3;
}

.docsh-search {
  position: relative;
  flex: 1 1 260px;
}

.docsh-search > i {
  position: absolute;
  top: 50%;
  left: 0.65rem;
  color: #b98d68;
  font-size: 0.75rem;
  pointer-events: none;
  transform: translateY(-50%);
}

.docsh-search .form-control {
  padding-left: 1.75rem;
}

.docsh-type-filter {
  flex: 0 0 auto;
  width: auto;
  min-width: 190px;
}

.docsh-count {
  margin-left: auto;
  color: #8a7a71;
  font-size: 0.75rem;
  font-weight: 700;
}

.docsh-badge {
  padding: 0.3rem 0.5rem;
  font-size: 0.66rem;
  font-weight: 800;
  letter-spacing: 0.06em;
}

.docsh-badge--id {
  background: #0d6efd;
  color: #fff;
}

.docsh-badge--coc {
  background: #0dcaf0;
  color: #1e0f0c;
}

.docsh-type-label {
  display: inline-block;
  margin-left: 0.45rem;
  color: #6b5b52;
  font-size: 0.78rem;
}

.docsh-intern-link {
  padding: 0;
  border: 0;
  background: transparent;
  color: #b85f00;
  font-weight: 700;
  text-align: left;
  text-decoration: underline;
  text-underline-offset: 2px;
}

.docsh-intern-link:hover,
.docsh-intern-link:focus-visible {
  color: #7d3d00;
}

.docsh-intern-name {
  color: #3a2a22;
  font-weight: 600;
}

.docsh-detail-strong {
  color: #3a2a22;
  font-weight: 700;
}

.docsh-generated-by {
  display: block;
  color: #8a7a71;
  font-size: 0.7rem;
}

.docsh-modal-layer {
  position: fixed;
  inset: 0;
  z-index: 1060;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 1rem;
  background: rgba(26, 15, 12, 0.55);
}

.docsh-modal {
  display: flex;
  flex-direction: column;
  width: min(720px, 100%);
  max-height: calc(100vh - 2rem);
  overflow: hidden;
  border-radius: 1rem;
  background: #fff;
  box-shadow: 0 30px 70px rgba(15, 23, 42, 0.35);
}

.docsh-modal-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
  padding: 1rem 1.25rem;
  background: linear-gradient(135deg, #ff8c1a, #f0a43a);
  color: #fff;
}

.docsh-modal-eyebrow {
  display: block;
  font-size: 0.65rem;
  font-weight: 800;
  letter-spacing: 0.14em;
  opacity: 0.85;
  text-transform: uppercase;
}

.docsh-modal-title {
  margin: 0.2rem 0 0;
  font-size: 1.15rem;
  font-weight: 800;
}

.docsh-modal-head .btn-close {
  filter: invert(1);
  opacity: 0.85;
}

.docsh-modal-body {
  flex: 1 1 auto;
  min-height: 0;
  padding: 1.1rem 1.25rem;
  overflow-y: auto;
}

.docsh-detail-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 0.85rem 1.25rem;
  margin: 0;
}

.docsh-detail-row {
  min-width: 0;
}

.docsh-detail-row dt {
  margin-bottom: 0.15rem;
  color: #8a7a71;
  font-size: 0.68rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-transform: uppercase;
}

.docsh-detail-row dd {
  margin: 0;
  color: #1e0f0c;
  font-size: 0.88rem;
  overflow-wrap: anywhere;
}

.docsh-detail-sub {
  display: block;
  color: #8a7a71;
  font-size: 0.76rem;
}

.docsh-modal-foot {
  display: flex;
  flex-wrap: wrap;
  justify-content: flex-end;
  gap: 0.5rem;
  padding: 0.9rem 1.25rem 1.1rem;
  border-top: 1px solid #f1e7d8;
}

@media (max-width: 900px) {
  .docsh-stats {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }
}

@media (max-width: 575.98px) {
  .docsh-stats {
    grid-template-columns: minmax(0, 1fr);
  }

  .docsh-type-filter {
    width: 100%;
  }

  .docsh-count {
    margin-left: 0;
  }

  .docsh-detail-grid {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
