<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <div>
          <h3 class="card-title mb-0">Documents History</h3>
          <p class="doc-history-subtitle mb-0">
            Permanent audit trail of every finalized Certificate of Completion and intern ID.
          </p>
        </div>
        <button
          type="button"
          class="btn btn-outline-secondary btn-sm"
          :disabled="isLoading"
          @click="loadHistory"
        >
          <i class="fas fa-sync-alt me-1"></i> Refresh
        </button>
      </div>

      <div class="card-body">
        <!-- Filters -->
        <div class="row g-2 mb-3">
          <div class="col-md-6">
            <div class="input-group input-group-sm">
              <span class="input-group-text"><i class="fas fa-search"></i></span>
              <input
                v-model.trim="search"
                type="search"
                class="form-control"
                placeholder="Search intern, document type, office, signatory…"
                aria-label="Search documents"
              />
            </div>
          </div>
          <div class="col-md-4">
            <select v-model="documentType" class="form-select form-select-sm" aria-label="Filter by document type">
              <option value="">All documents</option>
              <option value="Certificate of Completion">Certificate of Completion</option>
              <option value="Intern ID">Intern ID</option>
            </select>
          </div>
          <div class="col-md-2 d-flex align-items-center justify-content-md-end">
            <span class="doc-history-count">
              {{ filteredDocuments.length }}
              {{ filteredDocuments.length === 1 ? 'record' : 'records' }}
            </span>
          </div>
        </div>

        <!-- Summary pills -->
        <div v-if="!isLoading && !loadError && documents.length" class="doc-history-summary mb-3">
          <span class="doc-history-pill">
            <i class="fas fa-file-signature me-1"></i>
            {{ cocCount }} Certificate{{ cocCount === 1 ? '' : 's' }} of Completion
          </span>
          <span class="doc-history-pill">
            <i class="fas fa-id-card me-1"></i>
            {{ idCount }} Intern ID{{ idCount === 1 ? '' : 's' }}
          </span>
        </div>

        <div v-if="isLoading" class="text-center my-4">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="text-muted mt-2 mb-0">Loading document history...</p>
        </div>

        <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center">
          <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="loadHistory">Retry</button>
        </div>

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0 doc-history-table">
            <thead class="table-light">
              <tr>
                <th scope="col" style="min-width: 220px;">Document</th>
                <th scope="col" style="width: 140px;">Issued</th>
                <th scope="col" style="min-width: 260px;">Details</th>
                <th scope="col" style="width: 190px;">Signatory</th>
                <th scope="col" style="width: 170px;">Recorded</th>
                <th scope="col" class="text-end" style="width: 60px;">
                  <span class="visually-hidden">Expand</span>
                </th>
              </tr>
            </thead>
            <tbody>
              <template v-for="document in filteredDocuments" :key="document.id">
                <tr
                  class="doc-history-row"
                  :class="{ 'is-expanded': isExpanded(document.id) }"
                  @click="toggleExpanded(document.id)"
                >
                  <td>
                    <div class="doc-history-doc">
                      <span class="doc-history-icon" :class="iconClass(document.documentType)">
                        <i :class="documentIcon(document.documentType)"></i>
                      </span>
                      <div>
                        <div class="fw-semibold text-dark">
                          {{ document.documentType || 'Document' }}
                        </div>
                        <div class="text-muted small">
                          {{ document.internName || 'Unknown intern' }}
                          <span v-if="document.internId"> · #{{ document.internId }}</span>
                        </div>
                      </div>
                    </div>
                  </td>
                  <td class="text-muted small text-nowrap">{{ formatDisplayDate(document.issuedDate) }}</td>
                  <td>
                    <div v-if="document.summary.length" class="doc-history-chips">
                      <span
                        v-for="chip in document.summary"
                        :key="chip.label"
                        class="doc-history-chip"
                      >
                        <span class="doc-history-chip-label">{{ chip.label }}</span>
                        {{ chip.value }}
                      </span>
                      <span v-if="document.entries.length > document.summary.length" class="doc-history-chip is-muted">
                        +{{ document.entries.length - document.summary.length }} more
                      </span>
                    </div>
                    <span v-else class="text-muted small">—</span>
                  </td>
                  <td>
                    <span v-if="document.signatoryName" class="text-dark small">{{ document.signatoryName }}</span>
                    <span v-else class="text-muted small">—</span>
                  </td>
                  <td class="text-muted small text-nowrap">{{ formatDateTime(document.createdAt) }}</td>
                  <td class="text-end">
                    <button
                      type="button"
                      class="btn btn-sm btn-link doc-history-toggle"
                      :aria-expanded="String(isExpanded(document.id))"
                      :aria-label="isExpanded(document.id) ? 'Hide audit details' : 'Show audit details'"
                      @click.stop="toggleExpanded(document.id)"
                    >
                      <i :class="isExpanded(document.id) ? 'fas fa-chevron-up' : 'fas fa-chevron-down'"></i>
                    </button>
                  </td>
                </tr>

                <tr v-if="isExpanded(document.id)" :key="`${document.id}-details`" class="doc-history-expanded">
                  <td colspan="6">
                    <div class="doc-history-panel">
                      <div class="doc-history-panel-grid">
                        <div>
                          <span class="doc-history-panel-label">Record ID</span>
                          <span class="doc-history-panel-value">#{{ document.id }}</span>
                        </div>
                        <div>
                          <span class="doc-history-panel-label">Intern</span>
                          <span class="doc-history-panel-value">
                            {{ document.internName || 'Unknown intern' }}
                            <template v-if="document.internId"> (#{{ document.internId }})</template>
                          </span>
                        </div>
                        <div>
                          <span class="doc-history-panel-label">Issued date</span>
                          <span class="doc-history-panel-value">{{ formatDisplayDate(document.issuedDate) }}</span>
                        </div>
                        <div>
                          <span class="doc-history-panel-label">Recorded at</span>
                          <span class="doc-history-panel-value">{{ formatDateTime(document.createdAt) }}</span>
                        </div>
                        <div>
                          <span class="doc-history-panel-label">Signatory</span>
                          <span class="doc-history-panel-value">{{ document.signatoryName || '—' }}</span>
                        </div>
                        <div>
                          <span class="doc-history-panel-label">File reference</span>
                          <span class="doc-history-panel-value">{{ document.filePath || '—' }}</span>
                        </div>
                      </div>

                      <div v-if="document.entries.length" class="doc-history-panel-details">
                        <span class="doc-history-panel-label">Captured details</span>
                        <dl class="doc-history-definition-list">
                          <template v-for="entry in document.entries" :key="entry.label">
                            <dt>{{ entry.label }}</dt>
                            <dd>{{ entry.value }}</dd>
                          </template>
                        </dl>
                      </div>

                      <details v-if="document.raw" class="doc-history-raw">
                        <summary>Raw audit payload</summary>
                        <pre>{{ document.raw }}</pre>
                      </details>
                    </div>
                  </td>
                </tr>
              </template>

              <tr v-if="filteredDocuments.length === 0">
                <td colspan="6" class="text-center text-muted py-4">
                  <i class="far fa-folder-open me-2"></i>
                  {{ documents.length
                    ? 'No documents match your filters.'
                    : 'No documents have been generated yet.' }}
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { documentsAPI, getApiErrorMessage } from '@/services/api'
import { formatDisplayDate } from '@/utils/datetime'

/* Fields we know how to label. Anything else is still shown in the
   expanded panel so no audit information is hidden. */
const DETAIL_LABELS = {
  internName: 'Intern',
  internId: 'Intern #',
  idNumber: 'ID number',
  office: 'Office',
  school: 'School',
  email: 'Email',
  validUntil: 'Valid until',
  hoursRendered: 'Hours rendered',
  requiredHours: 'Required hours',
  dateStarted: 'Date started',
  dateCompleted: 'Date completed',
  paper: 'Paper size',
  fileName: 'File name'
}

/* Preferred chip order for the Details column. */
const SUMMARY_ORDER = [
  'idNumber',
  'validUntil',
  'school',
  'office',
  'email',
  'hoursRendered',
  'dateStarted',
  'dateCompleted',
  'paper',
  'fileName'
]

const SUMMARY_LIMIT = 3

const documents = ref([])
const search = ref('')
const documentType = ref('')
const isLoading = ref(false)
const loadError = ref('')
const expandedIds = ref(new Set())

const cocCount = computed(
  () => documents.value.filter((item) => item.documentType === 'Certificate of Completion').length
)
const idCount = computed(
  () => documents.value.filter((item) => item.documentType === 'Intern ID').length
)

/* Parse each record once so the template never re-parses the same JSON. */
const decoratedDocuments = computed(() =>
  documents.value.map((document) => {
    const entries = detailEntries(document)
    return {
      ...document,
      entries,
      summary: summaryFromEntries(entries),
      raw: rawDetails(document)
    }
  })
)

const filteredDocuments = computed(() => {
  const term = search.value.toLowerCase()

  return decoratedDocuments.value.filter((document) => {
    const typeOk = !documentType.value || document.documentType === documentType.value
    if (!typeOk) return false
    if (!term) return true

    const haystack = [
      document.documentType,
      document.internName,
      document.internId,
      document.signatoryName,
      document.filePath,
      ...document.entries.map((entry) => `${entry.label} ${entry.value}`)
    ]
      .join(' ')
      .toLowerCase()

    return haystack.includes(term)
  })
})

function normalizeText(value) {
  return String(value ?? '').trim()
}

/* Details is stored as text: usually a JSON object, but older rows may
   hold a plain sentence. */
function parseDetails(document) {
  const raw = normalizeText(document?.details)
  if (!raw) return null
  if (!raw.startsWith('{') && !raw.startsWith('[')) return null

  try {
    return JSON.parse(raw)
  } catch {
    return null
  }
}

function formatDetailValue(key, value) {
  if (value === null || value === undefined || value === '') return ''

  if (key === 'hoursRendered' || key === 'requiredHours') {
    return String(value)
  }

  if (key === 'validUntil' || key === 'dateStarted' || key === 'dateCompleted') {
    return formatDisplayDate(value)
  }

  if (Array.isArray(value)) return value.join(', ')
  if (typeof value === 'object') return JSON.stringify(value)

  return String(value)
}

function detailEntries(document) {
  const parsed = parseDetails(document)

  if (!parsed) {
    const raw = normalizeText(document?.details)
    return raw ? [{ label: 'Details', value: raw }] : []
  }

  const entries = []

  for (const key of Object.keys(parsed)) {
    const value = formatDetailValue(key, parsed[key])
    if (!value) continue

    entries.push({ key, label: DETAIL_LABELS[key] || key, value })
  }

  /* Pair the hours into one readable chip when both values exist. */
  const hoursEntry = entries.find((entry) => entry.key === 'hoursRendered')
  const requiredEntry = entries.find((entry) => entry.key === 'requiredHours')
  if (hoursEntry && requiredEntry) {
    hoursEntry.label = 'Hours'
    hoursEntry.value = `${hoursEntry.value} / ${requiredEntry.value} hrs`
    const index = entries.indexOf(requiredEntry)
    if (index >= 0) entries.splice(index, 1)
  }

  return entries
}

function summaryFromEntries(entries) {
  const byKey = new Map(entries.map((entry) => [entry.key, entry]))

  const ordered = []
  for (const key of SUMMARY_ORDER) {
    const entry = byKey.get(key)
    if (entry) {
      ordered.push(entry)
      byKey.delete(key)
    }
  }

  for (const entry of byKey.values()) {
    if (ordered.length >= SUMMARY_LIMIT) break
    ordered.push(entry)
  }

  return ordered.slice(0, SUMMARY_LIMIT)
}

function rawDetails(document) {
  const raw = normalizeText(document?.details)
  if (!raw) return ''

  const parsed = parseDetails(document)
  if (!parsed) return raw

  try {
    return JSON.stringify(parsed, null, 2)
  } catch {
    return raw
  }
}

function documentIcon(type) {
  if (type === 'Intern ID') return 'fas fa-id-card'
  if (type === 'Certificate of Completion') return 'fas fa-award'
  return 'far fa-file-alt'
}

function iconClass(type) {
  if (type === 'Intern ID') return 'is-id'
  if (type === 'Certificate of Completion') return 'is-coc'
  return 'is-other'
}

function formatDateTime(value) {
  if (!value) return '—'

  const text = String(value).trim().replace(/(\.\d{3})\d+/, '$1')
  const parsed = new Date(text)
  if (Number.isNaN(parsed.getTime())) return '—'

  return parsed.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function isExpanded(id) {
  return expandedIds.value.has(Number(id))
}

function toggleExpanded(id) {
  const next = new Set(expandedIds.value)
  const key = Number(id)

  if (next.has(key)) next.delete(key)
  else next.add(key)

  expandedIds.value = next
}

async function loadHistory() {
  isLoading.value = true
  loadError.value = ''

  try {
    const data = await documentsAPI.history({ take: 1000 })
    documents.value = Array.isArray(data) ? data : []
    /* Drop expansion state for records that are no longer listed. */
    const visible = new Set(documents.value.map((item) => Number(item.id)))
    expandedIds.value = new Set([...expandedIds.value].filter((id) => visible.has(id)))
  } catch (error) {
    loadError.value = getApiErrorMessage(error, 'Could not load the document history.')
  } finally {
    isLoading.value = false
  }
}

onMounted(loadHistory)
</script>

<style scoped>
.doc-history-subtitle {
  margin-top: 0.15rem;
  color: #8a7a71;
  font-size: 0.78rem;
}

.doc-history-count {
  color: #8a7a71;
  font-size: 0.78rem;
  font-weight: 600;
}

.doc-history-summary {
  display: flex;
  flex-wrap: wrap;
  gap: 0.4rem;
}

.doc-history-pill {
  display: inline-flex;
  align-items: center;
  padding: 0.25rem 0.6rem;
  border: 1px solid #efe3d6;
  border-radius: 999px;
  background: #fffaf2;
  color: #6b4a1c;
  font-size: 0.72rem;
  font-weight: 600;
}

.doc-history-table td,
.doc-history-table th {
  vertical-align: middle;
}

.doc-history-row {
  cursor: pointer;
}

.doc-history-row.is-expanded {
  background: #fffaf2;
}

.doc-history-doc {
  display: flex;
  align-items: center;
  gap: 0.6rem;
}

.doc-history-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  flex: 0 0 auto;
  width: 2.15rem;
  height: 2.15rem;
  border-radius: 0.55rem;
  font-size: 0.9rem;
}

.doc-history-icon.is-coc {
  background: #fff1d6;
  color: #b26900;
}

.doc-history-icon.is-id {
  background: #e3f0ff;
  color: #1d5fa8;
}

.doc-history-icon.is-other {
  background: #f0ece7;
  color: #6b5c52;
}

.doc-history-chips {
  display: flex;
  flex-wrap: wrap;
  gap: 0.3rem;
}

.doc-history-chip {
  display: inline-flex;
  align-items: baseline;
  gap: 0.28rem;
  max-width: 100%;
  padding: 0.16rem 0.45rem;
  border: 1px solid #efe3d6;
  border-radius: 0.4rem;
  background: #fffdf9;
  color: #4a3a31;
  font-size: 0.68rem;
  line-height: 1.3;
  overflow-wrap: anywhere;
}

.doc-history-chip.is-muted {
  border-style: dashed;
  background: transparent;
  color: #8a7a71;
}

.doc-history-chip-label {
  color: #a08d80;
  font-size: 0.6rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}

.doc-history-toggle {
  color: #b26900;
  text-decoration: none;
}

.doc-history-toggle:hover,
.doc-history-toggle:focus-visible {
  color: #8a4f00;
  text-decoration: none;
}

.doc-history-expanded > td {
  background: #fffaf2;
  border-top: 0;
}

.doc-history-panel {
  padding: 0.85rem 0.9rem;
  border: 1px solid #f0e2d1;
  border-radius: 0.6rem;
  background: #fffdf9;
}

.doc-history-panel-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
  gap: 0.65rem 1rem;
}

.doc-history-panel-label {
  display: block;
  margin-bottom: 0.1rem;
  color: #a08d80;
  font-size: 0.6rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.doc-history-panel-value {
  color: #3a2a22;
  font-size: 0.78rem;
  overflow-wrap: anywhere;
}

.doc-history-panel-details {
  margin-top: 0.85rem;
}

.doc-history-definition-list {
  display: grid;
  grid-template-columns: minmax(120px, max-content) 1fr;
  gap: 0.25rem 0.9rem;
  margin: 0.3rem 0 0;
  font-size: 0.76rem;
}

.doc-history-definition-list dt {
  color: #8a7a71;
  font-weight: 600;
}

.doc-history-definition-list dd {
  margin: 0;
  color: #3a2a22;
  overflow-wrap: anywhere;
}

.doc-history-raw {
  margin-top: 0.85rem;
  font-size: 0.75rem;
}

.doc-history-raw summary {
  color: #b26900;
  font-weight: 600;
  cursor: pointer;
}

.doc-history-raw pre {
  max-height: 220px;
  margin: 0.45rem 0 0;
  padding: 0.6rem 0.7rem;
  overflow: auto;
  border-radius: 0.45rem;
  background: #f7f1e8;
  color: #3a2a22;
  font-size: 0.7rem;
  white-space: pre-wrap;
  overflow-wrap: anywhere;
}
</style>
