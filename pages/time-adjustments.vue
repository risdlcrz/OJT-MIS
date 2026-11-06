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

      <div class="card-body p-0">
        <div class="table-responsive">
          <table id="adjustmentRequestsTable" class="table table-hover modern-table mb-0">
            <thead>
              <tr class="text-center">
                <th>#</th>
                <th>Intern</th>
                <th>Date</th>
                <th>Target Field</th>
                <th>Current Time</th>
                <th>Requested Time</th>
                <th>Reason</th>
                <th>Status</th>
                <th>Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-if="isLoading">
                <td colspan="9" class="text-center text-muted py-4">Loading requests...</td>
              </tr>
              <tr v-else-if="adjustmentRequests.length === 0">
                <td colspan="9" class="text-center text-muted py-4">No pending adjustment requests.</td>
              </tr>
              <tr v-for="req in adjustmentRequests" :key="req.id" class="text-center">
                <td>{{ req.id }}</td>
                <td>
                  <div v-if="req.internName">{{ req.internName }}</div>
                  <div v-else>{{ req.internEmail || 'N/A' }}</div>
                </td>
                <td>{{ formatDate(req.date) }}</td>
                <td>{{ shiftLabel(req) }}</td>
                <td>{{ formatTime(currentTimeOf(req)) }}</td>
                <td>{{ formatTime(requestedTimeOf(req)) }}</td>
                <td class="text-start">{{ req.reason || '—' }}</td>
                <td>
                  <span v-if="req.status === 'Approved'" class="badge bg-success">Approved</span>
                  <span v-else-if="req.status === 'Rejected'" class="badge bg-danger">Rejected</span>
                  <span v-else class="badge bg-warning text-dark">Pending</span>
                </td>
                <td>
                  <template v-if="req.status === 'Pending'">
                    <button
                      type="button"
                      class="btn btn-sm btn-primary"
                      title="Approve adjustment"
                      @click="approveRequest(req.id)"
                    >
                      <i class="fas fa-check"></i>
                    </button>
                    <button
                      type="button"
                      class="btn btn-sm btn-danger ms-1"
                      title="Reject adjustment"
                      @click="rejectRequest(req.id)"
                    >
                      <i class="fas fa-times"></i>
                    </button>
                  </template>
                  <span v-else class="text-muted">&mdash;</span>
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
import { inject, onMounted, ref } from 'vue'
import { getApiErrorMessage, hrAttendanceAPI } from '@/services/api'
import { formatClock, formatDisplayDate } from '@/utils/datetime'
import '../assets/css/Request.css'

const dialog = inject('dialog')
const { showConfirm } = dialog || {}

const adjustmentRequests = ref([])
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
  return req.shiftType || '—'
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
