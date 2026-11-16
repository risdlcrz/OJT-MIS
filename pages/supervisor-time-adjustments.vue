<template>
  <section class="content">
    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Time Adjustment Requests</h3>
          <p class="ojt-table-subtitle">
            Punch corrections from interns in <strong>{{ department || 'your department' }}</strong>. Approving updates the intern's DTR and hours.
          </p>
        </div>
        <div class="ojt-table-tools">
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="load">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
        </div>
      </div>

      <div v-if="notice" class="alert alert-success d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-check-circle me-2"></i>{{ notice }}</span>
        <button type="button" class="btn-close" aria-label="Dismiss" @click="notice = ''"></button>
      </div>
      <div v-if="scopeError || error" class="alert alert-danger d-flex justify-content-between align-items-center gap-2">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ scopeError || error }}</span>
        <button v-if="!scopeError" type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="search" type="search" class="form-control form-control-sm"
                 placeholder="Search intern or reason" aria-label="Search requests" />
        </div>
        <div class="ojt-segment" role="tablist" aria-label="Filter by status">
          <button v-for="tab in tabs" :key="tab" type="button" role="tab"
                  :class="{ active: statusFilter === tab }" :aria-selected="statusFilter === tab"
                  @click="statusFilter = tab">
            {{ tab }}<span class="ojt-segment-count">{{ countFor(tab) }}</span>
          </button>
        </div>
        <span class="ojt-toolbar-count">{{ filtered.length }} request(s)</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Intern</th>
              <th class="col-date">Log Date</th>
              <th>Field</th>
              <th class="col-center">Current</th>
              <th class="col-center">Requested</th>
              <th>Reason</th>
              <th>Status</th>
              <th class="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="8" :loading="isLoading" :empty="!isLoading && filtered.length === 0"
                        icon="fas fa-user-clock"
                        :title="statusFilter === 'Pending' ? 'You\'re all caught up' : 'No requests found'"
                        :text="statusFilter === 'Pending' ? 'No time adjustments are waiting for your review.' : 'Try another status or search.'" />
            <template v-if="!isLoading">
              <tr v-for="req in filtered" :key="req.id">
                <td>
                  <span class="ojt-cell-primary">{{ req.internName }}</span>
                  <span class="ojt-cell-secondary">Requested {{ formatDisplayDate(req.requestedAt) }}</span>
                </td>
                <td class="col-date">{{ formatDisplayDate(req.requestedTime) }}</td>
                <td>{{ adjustmentFieldLabel(req.targetField) }}</td>
                <td class="col-center">
                  <span v-if="currentTime(req)" class="font-monospace">{{ formatClock(currentTime(req)) }}</span>
                  <span v-else class="ojt-na">No punch</span>
                </td>
                <td class="col-center"><strong class="font-monospace">{{ formatClock(req.requestedTime) }}</strong></td>
                <td class="col-wrap">
                  <span v-if="req.reason">{{ req.reason }}</span>
                  <span v-else class="ojt-na">No reason given</span>
                </td>
                <td>
                  <StatusPill :status="req.status" />
                  <span v-if="req.reviewedAt" class="ojt-cell-secondary">{{ formatDisplayDate(req.reviewedAt) }}</span>
                </td>
                <td class="col-actions">
                  <template v-if="req.status === 'Pending'">
                    <button type="button" class="ojt-icon-btn ojt-icon-btn--approve" title="Approve"
                            :aria-label="`Approve ${req.internName}'s request`" :disabled="busyId === req.id"
                            @click="review(req, 'Approve')">
                      <span v-if="busyId === req.id" class="spinner-border spinner-border-sm"></span>
                      <i v-else class="fas fa-check"></i>
                    </button>
                    <button type="button" class="ojt-icon-btn ojt-icon-btn--danger" title="Reject"
                            :aria-label="`Reject ${req.internName}'s request`" :disabled="busyId === req.id"
                            @click="review(req, 'Reject')">
                      <i class="fas fa-times"></i>
                    </button>
                  </template>
                  <span v-else class="ojt-na">Reviewed</span>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, inject, onMounted, ref } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { getApiErrorMessage, supervisorAPI } from '@/services/api'
import { formatClock, formatDisplayDate } from '@/utils/datetime'
import { adjustmentFieldLabel, useDepartment } from '@/utils/supervisor'

const dialog = inject('dialog', null)
const { department, scopeError, loadDepartment } = useDepartment()

const tabs = ['Pending', 'Approved', 'Rejected', 'All']
const requests = ref([])
const statusFilter = ref('Pending')
const search = ref('')
const isLoading = ref(true)
const error = ref('')
const notice = ref('')
const busyId = ref(null)

function countFor(tab) {
  return tab === 'All' ? requests.value.length : requests.value.filter((r) => r.status === tab).length
}

const filtered = computed(() => {
  const term = search.value.trim().toLowerCase()
  return requests.value
    .filter((r) => statusFilter.value === 'All' || r.status === statusFilter.value)
    .filter((r) => !term || [r.internName, r.reason].some((v) => String(v || '').toLowerCase().includes(term)))
})

function currentTime(req) {
  return /_In$/.test(req.targetField || '') ? req.currentTimeIn : req.currentTimeOut
}

async function review(req, decision) {
  const verb = decision === 'Approve' ? 'Approve' : 'Reject'
  const message = decision === 'Approve'
    ? `Approve ${req.internName}'s ${adjustmentFieldLabel(req.targetField)} change to ${formatClock(req.requestedTime)}? Their DTR and rendered hours will be updated.`
    : `Reject ${req.internName}'s ${adjustmentFieldLabel(req.targetField)} adjustment?`
  const ok = dialog?.showConfirm ? await dialog.showConfirm(message, `${verb} Request`) : window.confirm(message)
  if (!ok) return

  busyId.value = req.id
  error.value = ''
  try {
    const result = await supervisorAPI.reviewTimeAdjustment(req.id, decision)
    notice.value = result?.completed
      ? `${result.message} ${req.internName} has now completed the required hours.`
      : (result?.message || `Request ${verb.toLowerCase()}d.`)
    await load(false)
  } catch (e) {
    notice.value = ''
    error.value = getApiErrorMessage(e, `Failed to ${verb.toLowerCase()} the request.`)
  } finally {
    busyId.value = null
  }
}

async function load(showSpinner = true) {
  if (showSpinner) isLoading.value = true
  error.value = ''
  await loadDepartment()
  if (scopeError.value) {
    isLoading.value = false
    return
  }
  try {
    const data = await supervisorAPI.timeAdjustments('All')
    requests.value = (Array.isArray(data) ? data : [])
      .sort((a, b) => String(b.requestedAt).localeCompare(String(a.requestedAt)))
  } catch (e) {
    requests.value = []
    error.value = getApiErrorMessage(e, 'Unable to load time adjustment requests.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
