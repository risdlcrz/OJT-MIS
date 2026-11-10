<template>
  <section class="content">
    <div class="card shadow-sm mb-3">
      <div class="card-header">
        <h3 class="card-title mb-0">Request documents</h3>
      </div>
      <div class="card-body">
        <p class="text-muted">Request an ID or Certificate of Completion. HR is notified through the same document workflow.</p>
        <div v-if="message" class="alert" :class="ok ? 'alert-success' : 'alert-danger'">{{ message }}</div>
        <div class="d-flex flex-wrap gap-2 mb-3">
          <button type="button" class="btn btn-primary" :disabled="busy" @click="requestDoc('ID')">Request ID</button>
          <button type="button" class="btn btn-outline-primary" :disabled="busy" @click="requestDoc('Certificate of Completion')">
            Request Certificate of Completion
          </button>
        </div>
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th>Document</th>
                <th>Status</th>
                <th>Requested</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="rows.length === 0">
                <td colspan="3" class="text-center text-muted py-4">No document requests yet.</td>
              </tr>
              <tr v-for="row in rows" :key="row.id">
                <td>{{ row.documentType }}</td>
                <td>{{ row.status }}</td>
                <td>{{ formatDate(row.createdAt) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Mga dokumentong nai-issue na (permanenteng record ng issuance). -->
    <div class="card shadow-sm">
      <div class="card-header">
        <h3 class="card-title mb-0">Issued documents</h3>
      </div>
      <div class="card-body">
        <p class="text-muted">Documents HR has already generated and released to you, with the details as issued.</p>
        <div class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th>Document</th>
                <th>Issued</th>
                <th>Details</th>
                <th>Signatory</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="issued.length === 0">
                <td colspan="4" class="text-center text-muted py-4">No documents have been issued to you yet.</td>
              </tr>
              <tr v-for="row in issued" :key="row.id">
                <td>
                  <span class="badge" :class="row.documentType === 'ID' ? 'bg-primary' : 'bg-info text-dark'">{{ row.documentType }}</span>
                </td>
                <td>{{ formatDate(row.dateIssued) }}</td>
                <td class="small">
                  <template v-if="row.documentType === 'ID'">
                    {{ row.office || 'Intern ID' }}
                  </template>
                  <template v-else>
                    {{ row.hoursRendered }} of {{ row.requiredHours }} hrs
                    <span v-if="row.recipientEmail"> &middot; sent to {{ row.recipientEmail }}</span>
                  </template>
                </td>
                <td class="small">{{ row.signatoryName || '—' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const rows = ref([])
const issued = ref([])
const busy = ref(false)
const message = ref('')
const ok = ref(false)

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : '—'
}

async function load() {
  const data = await internPortalAPI.getPortal()
  rows.value = data.documents || []
  issued.value = data.issuedDocuments || []
}

async function requestDoc(type) {
  busy.value = true
  message.value = ''
  try {
    await internPortalAPI.requestDocument(type)
    ok.value = true
    message.value = `${type} request sent to HR.`
    await load()
  } catch (e) {
    ok.value = false
    message.value = getApiErrorMessage(e, 'Unable to submit the request.')
  } finally {
    busy.value = false
  }
}

onMounted(async () => {
  try {
    await load()
  } catch (e) {
    ok.value = false
    message.value = getApiErrorMessage(e, 'Unable to load document requests.')
  }
})
</script>
