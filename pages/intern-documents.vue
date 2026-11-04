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
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const rows = ref([])
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
