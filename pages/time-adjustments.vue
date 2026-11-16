<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <div>
          <h3 class="card-title mb-1">Time Adjustment Requests</h3>
          <p class="text-muted small mb-0">Review and act on attendance time adjustment requests from interns.</p>
        </div>
        <div class="card-tools">
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="loadAdjustmentRequests">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
        </div>
      </div>

      <div v-if="notice" class="alert alert-success m-3 mb-0">{{ notice }}</div>
      <div v-if="error" class="alert alert-danger m-3 mb-0 d-flex justify-content-between align-items-center">
        <span>{{ error }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" @click="loadAdjustmentRequests">Retry</button>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="search" type="search" class="form-control form-control-sm"
                 placeholder="Search intern or reason" aria-label="Search requests" />
        </div>
        <span class="ojt-toolbar-count">{{ visibleRequests.length }} pending request(s)</span>
      </div>

      <div class="table-responsive">
        <table id="adjustmentRequestsTable" class="table table-hover ojt-table">
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
            <TableState :colspan="8" :loading="isLoading" :empty="!isLoading && visibleRequests.length === 0"
                        icon="fas fa-user-clock"
                        :title="adjustmentRequests.length ? 'No requests match your search' : 'You\'re all caught up'"
                        :text="adjustmentRequests.length ? 'Try a different search.' : 'No time adjustments are waiting for review.'" />
            <template v-if="!isLoading">
              <tr v-for="req in visibleRequests" :key="req.id">
                <td>
                  <span class="ojt-cell-primary">{{ req.internName || req.internEmail || 'Unknown intern' }}</span>
                  <span class="ojt-cell-secondary">Request #{{ req.id }}</span>
                </td>
                <td class="col-date">{{ formatDate(req.date) }}</td>
                <td>{{ shiftLabel(req) }}</td>
                <td class="col-center">
                  <span v-if="currentTimeOf(req)" class="font-monospace">{{ formatTime(currentTimeOf(req)) }}</span>
                  <span v-else class="ojt-na">No punch</span>
                </td>
                <td class="col-center"><strong class="font-monospace">{{ formatTime(requestedTimeOf(req)) }}</strong></td>
                <td class="col-wrap">
                  <span v-if="req.reason">{{ req.reason }}</span>
                  <span v-else class="ojt-na">No reason given</span>
                </td>
                <td><StatusPill :status="req.status || 'Pending'" /></td>
                <td class="col-actions">
                  <template v-if="!req.status || req.status === 'Pending'">
                    <button type="button" class="ojt-icon-btn ojt-icon-btn--approve" title="Approve adjustment"
                            :aria-label="`Approve request ${req.id}`" @click="approveRequest(req.id)">
                      <i class="fas fa-check"></i>
                    </button>
                    <button type="button" class="ojt-icon-btn ojt-icon-btn--danger" title="Reject adjustment"
                            :aria-label="`Reject request ${req.id}`" @click="rejectRequest(req.id)">
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
import { getApiErrorMessage, hrAttendanceAPI } from '@/services/api'
import { formatClock, formatDisplayDate } from '@/utils/datetime'
import '../assets/css/Request.css'

const dialog = inject('dialog')
const { showConfirm } = dialog || {}

const adjustmentRequests = ref([])
const search = ref('')
const visibleRequests = computed(() => {
  const term = search.value.trim().toLowerCase()
  if (!term) return adjustmentRequests.value
  return adjustmentRequests.value.filter((r) =>
    [r.internName, r.internEmail, r.reason].some((v) => String(v || '').toLowerCase().includes(term)))
})
const isLoading = ref(false)
const error = ref('')
const notice = ref('')

async function loadAdjustmentRequests() {
  isLoading.value = true
  error.value = ''
  try {
    const data = await hrAttendanceAPI.getPending()
    adjustmentRequests.value = Array.isArray(data) ? data : []
  } catch (e) {
    adjustmentRequests.value = []
    error.value = getApiErrorMessage(e, 'Failed to load adjustment requests.')
  } finally {
    isLoading.value = false
  }
}

async function approveRequest(id) {
  const ok = await showConfirm('Approve this attendance adjustment request? The selected punch and daily hours will be updated.', 'Confirm Approval')
  if (!ok) return
  try {
    const result = await hrAttendanceAPI.approve(id)
    notice.value = result?.message || 'Attendance adjustment approved.'
    await loadAdjustmentRequests()
  } catch (e) {
    notice.value = ''
    error.value = getApiErrorMessage(e, 'Failed to approve adjustment request.')
  }
}

async function rejectRequest(id) {
  const ok = await showConfirm('Reject this attendance adjustment request?', 'Confirm Rejection')
  if (!ok) return
  try {
    const result = await hrAttendanceAPI.reject(id)
    notice.value = result?.message || 'Attendance adjustment rejected.'
    await loadAdjustmentRequests()
  } catch (e) {
    notice.value = ''
    error.value = getApiErrorMessage(e, 'Failed to reject adjustment request.')
  }
}

function shiftLabel(req) {
  const field = req.targetField || req.targetShift
  if (field === 'AM_In') return 'AM Time In'
  if (field === 'AM_Out') return 'AM Time Out'
  if (field === 'PM_In') return 'PM Time In'
  if (field === 'PM_Out') return 'PM Time Out'
  return req.shiftType || 'Unspecified'
}

function isTimeInField(req) {
  const field = String(req.targetField || req.targetShift || '')
  return field === 'AM_In' || field === 'PM_In' || /in/i.test(field)
}

function firstPresent(req, keys) {
  for (const key of keys) {
    const value = req?.[key]
    if (value != null && String(value).trim() !== '') return value
  }
  return ''
}

function currentTimeOf(req) {
  const specific = firstPresent(req, ['currentTime', 'CurrentTime'])
  if (specific) return specific
  return isTimeInField(req)
    ? firstPresent(req, ['timeIn', 'TimeIn', 'timeOut', 'TimeOut'])
    : firstPresent(req, ['timeOut', 'TimeOut', 'timeIn', 'TimeIn'])
}

function requestedTimeOf(req) {
  return firstPresent(req, [
    'requestedTime',
    'RequestedTime',
    'requestedTimeOut',
    'RequestedTimeOut',
    'requestedTimeValue',
    'RequestedTimeValue'
  ])
}

function formatDate(v) {
  return formatDisplayDate(v)
}

function formatTime(v) {
  return formatClock(v)
}

onMounted(loadAdjustmentRequests)
</script>
