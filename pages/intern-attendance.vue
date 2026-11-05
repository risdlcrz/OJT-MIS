<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header">
        <h3 class="card-title mb-0">Attendance</h3>
      </div>

      <div v-if="notice" class="alert alert-success m-3 mb-0">{{ notice }}</div>

      <div v-if="isLoading" class="card-body text-center">
        <div class="spinner-border text-primary" role="status"></div>
      </div>
      <div v-else-if="error" class="alert alert-danger m-3">
        {{ error }}
        <button type="button" class="btn btn-sm btn-outline-danger ms-2" @click="load()">Retry</button>
      </div>
      <div v-else class="table-responsive">
        <table class="table table-hover align-middle mb-0">
          <thead class="table-light">
            <tr>
              <th>Date</th>
              <th>AM Time In</th>
              <th>AM Time Out</th>
              <th>PM Time In</th>
              <th>PM Time Out</th>
              <th>Total Hours</th>
              <th>Status</th>
              <th class="text-center">Action</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="rows.length === 0">
              <td colspan="8" class="text-center text-muted py-4">No attendance records yet.</td>
            </tr>
            <tr v-for="row in rows" :key="row.date">
              <td>{{ formatDate(row.date) }}</td>
              <td>{{ formatTime(row.morning?.timeIn) }}</td>
              <td>{{ formatTime(row.morning?.timeOut) }}</td>
              <td>{{ formatTime(row.afternoon?.timeIn) }}</td>
              <td>{{ formatTime(row.afternoon?.timeOut) }}</td>
              <td>{{ formatHours(row.hours) }}</td>
              <td>
                <span class="badge" :class="row.status === 'Late' ? 'bg-warning text-dark' : 'bg-success'">
                  {{ row.status || '—' }}
                </span>
                <span v-if="hasPending(row)" class="badge bg-info text-dark ms-1">Pending Approval</span>
              </td>
              <td class="text-center">
                <button
                  type="button"
                  class="btn btn-sm btn-outline-primary"
                  :disabled="!canRequestAdjustment(row)"
                  :title="canRequestAdjustment(row) ? 'Request an early out or time-out correction' : 'No attendance log available for adjustment'"
                  @click="openAdjustment(row)"
                >
                  <i class="fas fa-clock me-1"></i> Request Early Out / Time Adjustment
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <Teleport to="body">
      <template v-if="isAdjustmentOpen">
        <div
          class="modal fade show d-block"
          tabindex="-1"
          role="dialog"
          aria-modal="true"
          aria-labelledby="adjustmentModalLabel"
          @click.self="closeAdjustment"
        >
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
              <form @submit.prevent="submitAdjustment">
                <div class="modal-header">
                  <h5 id="adjustmentModalLabel" class="modal-title">Request Early Out / Time Adjustment</h5>
                  <button type="button" class="btn-close" aria-label="Close" @click="closeAdjustment"></button>
                </div>
                <div class="modal-body">
                  <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>

                  <div class="mb-3">
                    <label class="form-label">Attendance Date</label>
                    <input class="form-control" :value="formatDate(selectedRow?.date)" disabled />
                  </div>

                  <div class="mb-3">
                    <label for="adjustmentShift" class="form-label">Shift</label>
                    <select
                      id="adjustmentShift"
                      v-model="form.attendanceLogId"
                      class="form-select"
                      required
                      @change="syncRequestedTime"
                    >
                      <option v-for="option in shiftOptions" :key="option.id" :value="option.id">
                        {{ option.label }}
                      </option>
                    </select>
                    <div v-if="shiftOptions.length === 0" class="form-text text-danger">
                      Every attendance log for this day already has a pending request.
                    </div>
                  </div>

                  <div class="mb-3">
                    <label for="adjustmentTimeOut" class="form-label">Requested Time Out</label>
                    <input
                      id="adjustmentTimeOut"
                      v-model="form.requestedTimeOut"
                      type="time"
                      class="form-control"
                      required
                    />
                  </div>

                  <div class="mb-3">
                    <label for="adjustmentReason" class="form-label">Reason</label>
                    <textarea
                      id="adjustmentReason"
                      v-model="form.reason"
                      class="form-control"
                      rows="3"
                      maxlength="500"
                      required
                      placeholder="Explain why this time-out needs to be adjusted"
                    ></textarea>
                    <div class="form-text text-end">{{ form.reason.length }}/500</div>
                  </div>
                </div>
                <div class="modal-footer">
                  <button type="button" class="btn btn-outline-secondary" :disabled="isSubmitting" @click="closeAdjustment">
                    Cancel
                  </button>
                  <button type="submit" class="btn btn-primary" :disabled="isSubmitting || shiftOptions.length === 0">
                    <span v-if="isSubmitting" class="spinner-border spinner-border-sm me-2" role="status"></span>
                    Submit Request
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
        <div class="modal-backdrop fade show"></div>
      </template>
    </Teleport>
  </section>
</template>

<script setup>
import { computed, onMounted, reactive, ref } from 'vue'
import { getApiErrorMessage, internPortalAPI } from '@/services/api'

const rows = ref([])
const adjustments = ref([])
const isLoading = ref(true)
const error = ref('')
const notice = ref('')

const isAdjustmentOpen = ref(false)
const isSubmitting = ref(false)
const formError = ref('')
const selectedRow = ref(null)
const form = reactive({
  attendanceLogId: null,
  requestedTimeOut: '',
  reason: ''
})

const pendingLogIds = computed(() => new Set(
  adjustments.value
    .filter((item) => item.status === 'Pending')
    .map((item) => Number(item.attendanceLogId))
))

const shiftOptions = computed(() => buildShiftOptions(selectedRow.value))

function buildShiftOptions(row) {
  if (!row) return []

  const options = []
  if (row.morning?.id && !pendingLogIds.value.has(Number(row.morning.id))) {
    options.push({ id: Number(row.morning.id), label: 'AM Time Out', targetShift: 'AM_Out' })
  }
  if (row.afternoon?.id && !pendingLogIds.value.has(Number(row.afternoon.id))) {
    options.push({ id: Number(row.afternoon.id), label: 'PM Time Out', targetShift: 'PM_Out' })
  }
  return options
}

function hasPending(row) {
  const morningPending = row.morning?.id
    ? pendingLogIds.value.has(Number(row.morning.id))
    : false
  const afternoonPending = row.afternoon?.id
    ? pendingLogIds.value.has(Number(row.afternoon.id))
    : false
  return morningPending || afternoonPending
}

function canRequestAdjustment(row) {
  return buildShiftOptions(row).length > 0
}

function shiftRecord(id) {
  if (!selectedRow.value) return null
  if (selectedRow.value.morning?.id && Number(selectedRow.value.morning.id) === Number(id)) {
    return selectedRow.value.morning
  }
  if (selectedRow.value.afternoon?.id && Number(selectedRow.value.afternoon.id) === Number(id)) {
    return selectedRow.value.afternoon
  }
  return null
}

function timeInputValue(value) {
  const date = value ? new Date(value) : null
  if (!date || Number.isNaN(date.getTime())) return ''
  return `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`
}

function syncRequestedTime() {
  const record = shiftRecord(form.attendanceLogId)
  form.requestedTimeOut = timeInputValue(record?.timeOut)
}

function openAdjustment(row) {
  selectedRow.value = row
  formError.value = ''
  form.reason = ''

  const options = buildShiftOptions(row)
  form.attendanceLogId = options[0]?.id ?? null
  syncRequestedTime()

  isAdjustmentOpen.value = true
}

function closeAdjustment() {
  if (isSubmitting.value) return
  isAdjustmentOpen.value = false
}

async function submitAdjustment() {
  formError.value = ''

  const option = shiftOptions.value.find((item) => Number(item.id) === Number(form.attendanceLogId))
  if (!option) {
    formError.value = 'Choose a shift that has an attendance log.'
    return
  }
  if (!form.requestedTimeOut) {
    formError.value = 'Choose the requested time out.'
    return
  }

  const reason = form.reason.trim()
  if (!reason) {
    formError.value = 'Enter a reason for the adjustment.'
    return
  }

  isSubmitting.value = true
  try {
    await internPortalAPI.requestAdjustment({
      attendanceLogId: option.id,
      requestedTimeOut: form.requestedTimeOut,
      targetShift: option.targetShift,
      reason
    })
    notice.value = `Adjustment request for ${option.label} was submitted for approval.`
    isAdjustmentOpen.value = false
    await load({ silent: true })
  } catch (e) {
    formError.value = getApiErrorMessage(e, 'Unable to submit the adjustment request.')
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : '—'
}

function formatTime(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
    : '—'
}

function formatHours(hours) {
  return Number.isFinite(Number(hours)) && Number(hours) > 0 ? Number(hours).toFixed(2) : '—'
}

async function load({ silent = false } = {}) {
  if (!silent) isLoading.value = true
  error.value = ''
  try {
    const data = await internPortalAPI.getPortal()
    rows.value = data.attendance || []
    adjustments.value = data.adjustments || []
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load attendance.')
  } finally {
    if (!silent) isLoading.value = false
  }
}

onMounted(() => load())
</script>
