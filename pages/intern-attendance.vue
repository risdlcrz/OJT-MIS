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
                  :title="canRequestAdjustment(row) ? 'Request a time adjustment' : 'No attendance log available for adjustment'"
                  @click="openAdjustment(row)"
                >
                  <i class="fas fa-clock me-1"></i> Request Time Adjustment
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
                  <h5 id="adjustmentModalLabel" class="modal-title">Request Time Adjustment</h5>
                  <button type="button" class="btn-close" aria-label="Close" @click="closeAdjustment"></button>
                </div>
                <div class="modal-body">
                  <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>

                  <div class="mb-3">
                    <label class="form-label">Attendance Date</label>
                    <input class="form-control" :value="formatDate(selectedRow?.date)" disabled />
                  </div>

                  <div class="mb-3">
                    <label for="adjustmentField" class="form-label">Target field</label>
                    <select
                      id="adjustmentField"
                      v-model="form.targetField"
                      class="form-select"
                      required
                      @change="syncRequestedTime"
                    >
                      <option v-for="option in fieldOptions" :key="option.targetField" :value="option.targetField">
                        {{ option.label }}
                      </option>
                    </select>
                    <div v-if="fieldOptions.length === 0" class="form-text text-danger">
                      Every punch for this day already has a pending request.
                    </div>
                  </div>

                  <div class="mb-3">
                    <label for="adjustmentTime" class="form-label">{{ selectedField.label }}</label>
                    <input
                      id="adjustmentTime"
                      v-model="form.requestedTime"
                      type="time"
                      class="form-control"
                      required
                      :placeholder="selectedField.label"
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
                      :placeholder="`Explain why this ${selectedField.label} needs to be adjusted`"
                    ></textarea>
                    <div class="form-text text-end">{{ form.reason.length }}/500</div>
                  </div>
                </div>
                <div class="modal-footer">
                  <button type="button" class="btn btn-outline-secondary" :disabled="isSubmitting" @click="closeAdjustment">
                    Cancel
                  </button>
                  <button type="submit" class="btn btn-primary" :disabled="isSubmitting || fieldOptions.length === 0">
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
import { formatClock, formatDisplayDate } from '@/utils/datetime'

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
  targetField: '',
  requestedTime: '',
  reason: ''
})

const FIELD_DEFS = [
  { targetField: 'AM_In', label: 'AM Time In', shift: 'morning', punch: 'timeIn' },
  { targetField: 'AM_Out', label: 'AM Time Out', shift: 'morning', punch: 'timeOut' },
  { targetField: 'PM_In', label: 'PM Time In', shift: 'afternoon', punch: 'timeIn' },
  { targetField: 'PM_Out', label: 'PM Time Out', shift: 'afternoon', punch: 'timeOut' }
]

const pendingKeys = computed(() => new Set(
  adjustments.value
    .filter((item) => item.status === 'Pending')
    .map((item) => `${Number(item.attendanceLogId)}:${item.targetField}`)
))

const fieldOptions = computed(() => buildFieldOptions(selectedRow.value))

const selectedField = computed(() =>
  fieldOptions.value.find((item) => item.targetField === form.targetField) || {
    label: 'Requested time',
    targetField: ''
  }
)

function buildFieldOptions(row) {
  if (!row) return []

  return FIELD_DEFS.flatMap((def) => {
    const record = row[def.shift]
    if (!record?.id) return []
    if (pendingKeys.value.has(`${Number(record.id)}:${def.targetField}`)) return []
    return [{
      ...def,
      attendanceLogId: Number(record.id),
      current: record[def.punch]
    }]
  })
}

function hasPending(row) {
  const ids = [row.morning?.id, row.afternoon?.id].filter(Boolean).map(Number)
  return adjustments.value.some((item) =>
    item.status === 'Pending' && ids.includes(Number(item.attendanceLogId))
  )
}

function canRequestAdjustment(row) {
  return buildFieldOptions(row).length > 0
}

function timeInputValue(value) {
  if (!value) return ''
  const match = String(value).match(/(?:T|\s|^)(\d{1,2}):(\d{2})/)
  if (!match) return ''
  return `${match[1].padStart(2, '0')}:${match[2]}`
}

function syncRequestedTime() {
  const option = fieldOptions.value.find((item) => item.targetField === form.targetField)
  form.requestedTime = timeInputValue(option?.current)
}

function openAdjustment(row) {
  selectedRow.value = row
  formError.value = ''
  form.reason = ''
  form.targetField = buildFieldOptions(row)[0]?.targetField ?? ''
  syncRequestedTime()
  isAdjustmentOpen.value = true
}

function closeAdjustment() {
  if (isSubmitting.value) return
  isAdjustmentOpen.value = false
}

async function submitAdjustment() {
  formError.value = ''

  const option = fieldOptions.value.find((item) => item.targetField === form.targetField)
  if (!option) {
    formError.value = 'Choose AM Time In, AM Time Out, PM Time In, or PM Time Out.'
    return
  }
  if (!form.requestedTime) {
    formError.value = `Choose the requested ${option.label}.`
    return
  }

  const reason = form.reason.trim()
  if (!reason) {
    formError.value = `Enter a reason for this ${option.label} adjustment.`
    return
  }

  isSubmitting.value = true
  try {
    await internPortalAPI.requestAdjustment({
      attendanceLogId: option.attendanceLogId,
      requestedTime: form.requestedTime,
      targetField: option.targetField,
      reason
    })
    notice.value = `${option.label} adjustment was submitted for approval.`
    isAdjustmentOpen.value = false
    await load({ silent: true })
  } catch (e) {
    formError.value = getApiErrorMessage(e, `Unable to submit the ${option.label} request.`)
  } finally {
    isSubmitting.value = false
  }
}

function formatDate(v) {
  return formatDisplayDate(v)
}

function formatTime(v) {
  return formatClock(v)
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
