<template>
  <section class="content">
    <div class="ojt-stat-grid">
      <div class="ojt-stat">
        <span class="ojt-stat-label">Total generated</span>
        <span class="ojt-stat-value">{{ loading ? '…' : documents.length }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">ID cards</span>
        <span class="ojt-stat-value">{{ loading ? '…' : idCount }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Certificates</span>
        <span class="ojt-stat-value">{{ loading ? '…' : cocCount }}</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Interns served</span>
        <span class="ojt-stat-value">{{ loading ? '…' : internCount }}</span>
      </div>
    </div>

    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Documents History</h3>
          <p class="ojt-table-subtitle">
            Every ID card and Certificate of Completion HR has generated, with the exact details recorded on the intern's profile.
          </p>
        </div>
        <div class="ojt-table-tools">
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="loading" @click="load">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
          <button type="button" class="btn btn-outline-primary btn-sm" @click="openGenerator('coc-generate.html')">
            <i class="fas fa-certificate me-1"></i> Generate COC
          </button>
          <button type="button" class="btn btn-primary btn-sm" @click="openGenerator('idgenerate.html')">
            <i class="far fa-id-card me-1"></i> Generate ID
          </button>
        </div>
      </div>

      <div v-if="error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ error }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" :disabled="loading" @click="load">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="query" type="search" class="form-control form-control-sm" autocomplete="off"
                 placeholder="Search intern, ID no., office, signatory" aria-label="Search documents" />
        </div>
        <div class="ojt-segment" role="tablist" aria-label="Filter by document type">
          <button v-for="tab in typeTabs" :key="tab.value" type="button" role="tab"
                  :class="{ active: typeFilter === tab.value }" :aria-selected="typeFilter === tab.value"
                  @click="typeFilter = tab.value">
            {{ tab.label }}
          </button>
        </div>
        <span class="ojt-toolbar-count">{{ filteredDocuments.length }} of {{ documents.length }} record(s)</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Document</th>
              <th>Intern</th>
              <th class="col-date">Date Issued</th>
              <th>Finalized Details</th>
              <th>Signatory</th>
              <th class="col-date">Generated</th>
              <th class="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="7" :loading="loading" :empty="!loading && filteredDocuments.length === 0"
                        icon="far fa-file-alt"
                        :title="documents.length ? 'No documents match your filters' : 'No documents generated yet'"
                        :text="documents.length ? 'Try a different search or document type.' : 'Generate an ID or Certificate of Completion to start the history.'" />
            <template v-if="!loading">
              <tr v-for="doc in filteredDocuments" :key="doc.id" class="ojt-row-link" tabindex="0"
                  @click="selected = doc" @keydown.enter="selected = doc">
                <td>
                  <StatusPill :status="doc.documentType" :label="doc.documentType === 'ID' ? 'ID Card' : 'COC'"
                              :tone="doc.documentType === 'ID' ? 'brand' : 'info'" />
                </td>
                <td>
                  <span class="ojt-cell-primary">{{ doc.internName || (doc.internId ? `Intern #${doc.internId}` : 'Unlinked intern') }}</span>
                  <span class="ojt-cell-secondary">{{ doc.school || doc.office || 'No school on record' }}</span>
                </td>
                <td class="col-date">
                  <span v-if="formatDateOnly(doc.dateIssued)">{{ formatDateOnly(doc.dateIssued) }}</span>
                  <span v-else class="ojt-na">Not set</span>
                </td>
                <td>
                  <template v-if="doc.documentType === 'ID'">
                    <span class="ojt-cell-primary">ID No. {{ doc.idNumber || 'not set' }}</span>
                    <span class="ojt-cell-secondary">
                      {{ doc.office || 'No office' }}<template v-if="doc.validUntil"> &middot; valid until {{ formatDateOnly(doc.validUntil) }}</template>
                    </span>
                  </template>
                  <template v-else>
                    <span class="ojt-cell-primary">{{ formatHours(doc.hoursRendered) }} of {{ doc.requiredHours || 0 }} hrs</span>
                    <span class="ojt-cell-secondary">{{ doc.recipientEmail ? `Emailed to ${doc.recipientEmail}` : 'Not emailed' }}</span>
                  </template>
                </td>
                <td>
                  <template v-if="doc.signatoryName">
                    <span class="d-block">{{ doc.signatoryName }}</span>
                    <span v-if="doc.signatoryPosition" class="ojt-cell-secondary text-capitalize">{{ doc.signatoryPosition }}</span>
                  </template>
                  <span v-else class="ojt-na">No signatory</span>
                </td>
                <td class="col-date">
                  <span class="d-block">{{ formatDateTime(doc.createdAt) }}</span>
                  <span v-if="doc.generatedBy" class="ojt-cell-secondary">by {{ doc.generatedBy }}</span>
                </td>
                <td class="col-actions">
                  <button type="button" class="ojt-icon-btn" title="View details" :aria-label="`View ${doc.documentType} details`"
                          @click.stop="selected = doc">
                    <i class="fas fa-eye"></i>
                  </button>
                  <button type="button" class="ojt-icon-btn" :title="doc.internId ? 'Regenerate for this intern' : 'Open generator'"
                          :aria-label="`Regenerate ${doc.documentType}`" @click.stop="regenerate(doc)">
                    <i class="fas fa-rotate-right"></i>
                  </button>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

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
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { go } from '@/utils/navigation'
import { getApiErrorMessage, hrAttendanceAPI } from '@/services/api'
import { DOC_TYPES } from '@/utils/documents'

const documents = ref([])
const loading = ref(true)
const error = ref('')
const query = ref('')
const typeFilter = ref('all')
const typeTabs = [
  { value: 'all', label: 'All' },
  { value: DOC_TYPES.ID, label: 'ID Cards' },
  { value: DOC_TYPES.COC, label: 'Certificates' }
]
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
  go(`/${page}${queryString}`)
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

@media (max-width: 575.98px) {
  .docsh-detail-grid {
    grid-template-columns: minmax(0, 1fr);
  }
}
</style>
