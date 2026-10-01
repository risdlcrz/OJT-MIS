<template>
  <section class="content">
    <!-- ================= API-backed module ================= -->
    <div v-if="config" class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">{{ config.title }}</h3>
        <button type="button" class="btn btn-primary btn-sm" :disabled="isLoading || (config.hiring && availableRequests.length === 0)" :title="config.hiring && availableRequests.length === 0 ? 'No available intern request' : ''" @click="openCreate">
          <i class="fas fa-plus me-1"></i> Add {{ config.singular }}
        </button>
      </div>

      <div class="card-body">
        <div v-if="isLoading" class="text-center my-4">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>

        <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center">
          <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
        </div>

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th v-for="col in displayColumns" :key="col.prop" scope="col">{{ col.label }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, index) in rows" :key="row.id">
                <td v-for="col in displayColumns" :key="col.prop">
                  <span v-if="col.clickable" class="text-dark" style="cursor: pointer; transition: all 0.2s ease; user-select: none;" @click="openView(row)" @mouseenter="$event.target.style.fontWeight='600'; $event.target.style.color='#ff6b00'" @mouseleave="$event.target.style.fontWeight='normal'; $event.target.style.color=''">{{ cell(row, col) }}</span>
                  <span v-else class="text-dark">{{ cell(row, col) }}</span>
                </td>
              </tr>
              <tr v-if="rows.length === 0">
                <td :colspan="displayColumns.length" class="text-center text-muted py-4">
                  No {{ config.title.toLowerCase() }} yet.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ================= Not built yet ================= -->
    <div v-else class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">{{ title }}</h3>
        <button type="button" class="btn btn-outline-secondary btn-sm" @click="goHome">
          <i class="fas fa-arrow-left me-1"></i> Back to Dashboard
        </button>
      </div>
      <div class="card-body">
        <div class="text-center py-5">
          <i class="fas fa-tools fa-3x text-muted mb-3"></i>
          <h5 class="mb-2">{{ title }}</h5>
          <p class="text-muted mb-0">This module is registered and reachable, but its screen has not been built yet.</p>
        </div>
      </div>
    </div>

    <!-- ================= Add / Edit modal ================= -->
    <div ref="formModalEl" class="modal fade" id="moduleFormModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <form novalidate @submit.prevent="save">
            <div class="modal-header">
              <h5 class="modal-title">{{ isEditing ? `Edit ${config.singular}` : `Add ${config.singular}` }}</h5>
              <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeForm"></button>
            </div>
            <div class="modal-body">
              <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>
              <div v-for="col in config.columns" :key="col.prop" class="mb-3">
                <label :for="`m-${col.prop}`" class="form-label fw-semibold">{{ col.label }}</label>

                <!-- Dropdown ng request: ang mga may natitirang slot lamang. -->
                <select v-if="col.prop === 'requestId'" :id="`m-${col.prop}`" v-model="form.requestId"
                        class="form-select" :class="{ 'is-invalid': submitted && !form.requestId }"
                        :disabled="isSaving || isEditing">
                  <option value="">Select an intern request</option>
                  <option v-for="r in availableRequests" :key="r.id" :value="r.id">
                    #{{ r.id }} &mdash; {{ r.officeName }} ({{ r.remaining }} slot{{ r.remaining === 1 ? '' : 's' }} left)
                  </option>
                </select>
                <div v-if="col.prop === 'requestId'" class="invalid-feedback">Please select an intern request.</div>

                <template v-else-if="col.prop === 'status'">
                  <!-- Status field: uneditable when editing, defaults to 'Hired' when creating -->
                  <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" type="text" class="form-control"
                         :class="{ 'is-invalid': submitted && !form[col.prop] }" :maxlength="col.max"
                         :placeholder="`Input ${col.label}`" :disabled="isSaving || isEditing" />
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                  <div v-if="isEditing" class="form-text text-muted">Status cannot be changed after hiring.</div>
                </template>

                <template v-else-if="col.prop === 'department'">
                  <select :id="`m-${col.prop}`" v-model.trim="form.department"
                          class="form-select" :disabled="isSaving">
                    <option value="">Select Department (optional)</option>
                    <option v-for="name in departmentList" :key="name" :value="name">
                      {{ name }}
                    </option>
                  </select>
                  <div class="form-text text-muted">Optional</div>
                </template>

                <template v-else>
                  <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" type="text" class="form-control"
                         :class="{ 'is-invalid': submitted && !form[col.prop] }" :maxlength="col.max"
                         :placeholder="`Input ${col.label}`" :disabled="isSaving" />
                  <div class="invalid-feedback">{{ col.label }} is required.</div>
                </template>
              </div>

              <!-- Last updated field (read-only, only shown when editing) -->
              <div v-if="isEditing && form.updatedAt" class="mb-3">
                <label class="form-label fw-semibold">Last Updated</label>
                <input type="text" class="form-control form-control-plaintext bg-light" readonly
                       :value="formatDate(form.updatedAt)" />
              </div>
            </div>
            <div class="modal-footer">
              <button type="submit" class="btn btn-primary" :disabled="isSaving">
                <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
                {{ isEditing ? 'Update' : 'Submit' }}
              </button>
              <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeForm">Close</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- ================= Delete modal ================= -->
    <div ref="deleteModalEl" class="modal fade" id="moduleDeleteModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Delete</h5>
            <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeDelete"></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">Delete <strong class="text-danger">{{ pending?.id }}</strong>? This cannot be undone.</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-danger" :disabled="isSaving" @click="doDelete">
              <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
              Delete
            </button>
            <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeDelete">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= View Intern Modal ================= -->
    <Teleport to="body">
      <div v-if="viewIntern"
           style="position: fixed; top: 0; right: 0; bottom: 0; left: 0; z-index: 1055; overflow: hidden; background: rgba(26, 26, 46, 0.55); display: flex; align-items: center; justify-content: center; padding: 0.75rem;"
           role="dialog" aria-modal="true" aria-label="Intern Details" @click.self="closeView">
        <div class="modal-content shadow-lg border-0" style="background: linear-gradient(135deg, #f97316 0%, #ffffff 100%); border-radius: 12px; width: 720px; max-width: 100%; max-height: calc(100vh - 1.5rem); display: flex; flex-direction: column; box-shadow: 0 1rem 3rem rgba(0,0,0,0.175);">
          <div class="modal-header bg-transparent border-0 px-4 pt-4 pb-2">
            <h5 class="modal-title fw-bold" style="color: #1a1a2e;">Intern Details</h5>
            <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeView"></button>
          </div>
          <div class="modal-body p-4 overflow-auto">
            <div class="bg-white rounded-3 shadow-sm p-4 border" style="border-color: rgba(33,37,41,0.08) !important;">
              <div class="row g-3">
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Applicant No.</label>
                <div class="fw-bold fs-5" style="color: #1a1a2e;">{{ viewIntern.applicantNo || '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Full Name</label>
                <div class="fw-bold fs-5" style="color: #1a1a2e;">{{ viewIntern.fullName || '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">School</label>
                <div style="color: #212529;">{{ viewIntern.school || '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Program</label>
                <div style="color: #212529;">{{ viewIntern.program || '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Contact No.</label>
                <div style="color: #212529;">{{ viewIntern.contactNumber || '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Department</label>
                <div style="color: #212529;">{{ viewIntern.requestOffice || (viewIntern.requestId ? `#${viewIntern.requestId}` : '-') }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Hours Rendered</label>
                <div class="fw-bold" style="color: #1a1a2e;">{{ viewIntern.durationDays != null ? viewIntern.durationDays * 8 : '-' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">OJT Period</label>
                <div style="color: #212529;">{{ formatOJTPeriod(viewIntern) }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Fridays</label>
                <div style="color: #212529;">{{ viewIntern.excludeFriday ? 'Excluded' : 'Included' }}</div>
              </div>
              <div class="col-md-6">
                <label class="text-secondary small text-uppercase fw-semibold">Status</label>
                <div>
                  <span class="badge" :class="statusBadge(viewIntern.status)">{{ viewIntern.status || '-' }}</span>
                </div>
              </div>
            </div>
            </div>
          </div>
          <div class="modal-footer bg-transparent border-0 px-4 pt-2 pb-4 d-flex justify-content-end gap-3">
            <button type="button" class="btn px-4 py-2 rounded-pill shadow-sm" style="background: #ff6b00; color: #fff; border: none;" :disabled="isSaving" @click="editFromView">
              <i class="fas fa-pen me-1"></i> Edit
            </button>
            <button type="button" class="btn btn-outline-danger px-4 py-2 rounded-pill shadow-sm" :disabled="isSaving" @click="deleteFromView">
              <i class="fas fa-trash me-1"></i> Delete
            </button>
            <button type="button" class="btn btn-outline-secondary px-4 py-2 rounded-pill shadow-sm" @click="closeView">
              <i class="fas fa-times me-1"></i> Close
            </button>
          </div>
        </div>
      </div>
    </Teleport>

    <!-- ================= Toast ================= -->
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div ref="toastEl" class="toast align-items-center text-bg-success border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
          <div class="toast-body">{{ message }}</div>
          <button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close" @click="toast.hide()"></button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref, watch } from 'vue'
import { Modal } from 'bootstrap'
import { getApiErrorMessage, internRequestsAPI, internsAPI, programsAPI, signatoriesAPI } from '@/services/api'

/* page -> module config. Ang hindi nakalist dito ay "not built yet" pa. */
const MODULES = {
  'internlist.html': {
    title: 'Intern List', singular: 'Intern', api: internsAPI, hiring: true,
    columns: [
      { prop: 'fullName', label: 'Full Name', max: 200 },
      { prop: 'school', label: 'School', max: 200 },
      { prop: 'requestId', label: 'Request ID', type: 'text' },
      { prop: 'status', label: 'Status', max: 20 }
    ],
    /* Karagdagang column na hango sa profile: hindi ito lumalabas sa form. */
    display: [
      { prop: 'fullName', label: 'Full Name', clickable: true },
      { prop: 'school', label: 'School' },
      { prop: 'requestId', label: 'Department', format: (_v, row) => row.requestOffice || (row.requestId ? `#${row.requestId}` : null) },
      { prop: 'status', label: 'Status' }
    ]
  },
  'programs.html': {
    title: 'Programs / Strands', singular: 'Program', api: programsAPI,
    columns: [{ prop: 'programCode', label: 'Program Code', max: 50 }, { prop: 'programName', label: 'Program Name', max: 200 }]
  },
  'signatories.html': {
    title: 'Signatories', singular: 'Signatory', api: signatoriesAPI, needsDepartment: true,
    columns: [
      { prop: 'name', label: 'Name', max: 200 },
      { prop: 'position', label: 'Position', max: 150 },
      { prop: 'department', label: 'Department', max: 200 }
    ]
  },
  'forms.html': { title: 'Issuance of COC' },
  'dtr.html': { title: 'Daily Time Records' },
  'idgenerate.html': { title: 'ID Generation' },
  'utility.html': { title: 'Utility' }
}

/* Ang page ay ipinapasa bilang prop (mula sa App.vue) para mag-re-render
   kapag nagpalit ng module ang user. Kung babasahin ito sa
   window.location, mananatili ang lumang module dahil nire-reuse
   ng Vue ang parehong instance. */
const props = defineProps({
  modulePage: { type: String, default: '' }
})

const entry = computed(() => MODULES[props.modulePage] || {})
const config = computed(() => (entry.value.api ? entry.value : null))
const title = computed(() => entry.value.title || 'Module')

/* Mga column na ipinapakita sa table. Default: lahat ng config.columns.
   Puwedeng may sariling "display" ang module para magdagdag ng
   computed column (hal. OJT period) na wala sa form. */
const displayColumns = computed(() => entry.value.display || entry.value.columns || [])

/* Papuntahan sa cell. Kung may "format" ang column, gamitin iyon
   (hal. petsa, o "#id - Office Name"). */
function cell(row, col) {
  const value = row[col.prop]
  if (col.format) return col.format(value, row)
  if (value === null || value === undefined || value === '') return '\u2014'
  return value
}

const rows = ref([])
const availableRequests = ref([])
const departments = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const formError = ref('')
const submitted = ref(false)
const pending = ref(null)
const viewIntern = ref(null)
const message = ref('')

const form = reactive({ id: 0 })
const isEditing = computed(() => form.id > 0)

const formModalEl = ref(null)
const deleteModalEl = ref(null)
const toastEl = ref(null)
let formModal = null
let deleteModal = null
let toast = null
let toastTimer = null

function blankForm() {
  form.id = 0
  form.requestId = ''
  form.updatedAt = null
  if (config.value) config.value.columns.forEach((c) => { form[c.prop] = '' })
  submitted.value = false
  formError.value = ''
}

function notify(text, variant = 'success') {
  message.value = text
  const el = toastEl.value
  el.classList.remove('text-bg-success', 'text-bg-danger')
  el.classList.add(`text-bg-${variant}`)
  toast.show()
  window.clearTimeout(toastTimer)
  toastTimer = window.setTimeout(() => toast.hide(), 4000)
}

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleString(undefined, { year: 'numeric', month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })
    : '-'
}

async function load() {
  if (!config.value) return
  isLoading.value = true
  loadError.value = ''
  try {
    rows.value = await config.value.api.getAll()
    if (config.value.hiring || config.value.needsDepartment) {
      availableRequests.value = await internRequestsAPI.available()
      // Fetch all requests to map requestId -> full office name for Department column
      const allRequests = await internRequestsAPI.getAll()
      const requestMap = new Map(allRequests.map(r => [r.id, r.officeName]))
      rows.value.forEach(row => {
        row.requestOffice = requestMap.get(row.requestId) || null
      })
    }
    // Load departments from shared source for Signatory modal dropdown
    if (config.value.needsDepartment) {
      try {
        const response = await fetch('/assets/json/offices.json')
        const data = await response.json()
        departments.value = data.offices || []
      } catch {
        departments.value = []
      }
    }
  } catch (e) {
    loadError.value = getApiErrorMessage(e, `Failed to load ${title.value}.`)
  } finally {
    isLoading.value = false
  }
}

const departmentList = computed(() => {
  const seen = new Set()
  return departments.value
    .filter(d => d.name && !seen.has(d.name) && seen.add(d.name))
    .map(d => d.name)
})

function openCreate() {
  blankForm()
  form.status = 'Hired'
  formModal.show()
}
function openEdit(row) {
  blankForm()
  form.id = row.id
  config.value.columns.forEach((c) => { form[c.prop] = row[c.prop] ?? '' })
  // Copy updatedAt for display in the modal (not in config.columns)
  form.updatedAt = row.updatedAt
  formModal.show()
}
function closeForm() { if (!isSaving.value) formModal.hide() }

async function save() {
  submitted.value = true
  formError.value = ''

  if (config.value.hiring && availableRequests.value.length === 0) {
    formError.value = 'There is no available intern request. Please create a request first.'
    return
  }

  const requiredColumns = config.value.columns.filter(c => c.prop !== 'department')
  const missing = requiredColumns.some((c) => !form[c.prop])
  if (missing) { formError.value = 'Please fill in all required fields.'; return }

  isSaving.value = true
  const api = config.value.api

  // Only send properties the backend model expects; omit updatedAt (server-set)
  const payload = {
    name: form.name,
    position: form.position,
    department: form.department || ''
  }
  if (config.value.hiring) {
    payload.status = 'Hired'
    payload.requestId = Number(form.requestId) || null
  }

  try {
    if (isEditing.value) { await api.update(form.id, payload); notify('Record updated.') }
    else { await api.create(payload); notify('Record created.') }
    formModal.hide()
    await load()
  } catch (e) {
    formError.value = getApiErrorMessage(e, 'Failed to save.')
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(row) { pending.value = row; deleteModal.show() }
function closeDelete() { if (!isSaving.value) { deleteModal.hide(); pending.value = null } }

function openView(row) {
  viewIntern.value = row
  document.body.style.overflow = 'hidden'
}
function closeView() {
  if (isSaving.value) return
  viewIntern.value = null
  document.body.style.overflow = ''
}
function editFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  openEdit(row)
}
function deleteFromView() {
  const row = viewIntern.value
  if (!row) return
  closeView()
  confirmDelete(row)
}

function formatOJTPeriod(row) {
  const s = formatDate(row.startDate)
  const e = formatDate(row.endDate)
  return s || e ? `${s} \u2013 ${e}` : '-'
}
function statusBadge(status) {
  switch (status) {
    case 'Hired': return 'bg-success'
    case 'Pending': return 'bg-warning text-dark'
    case 'Completed': return 'bg-info text-dark'
    case 'Terminated': return 'bg-danger'
    default: return 'bg-secondary'
  }
}

async function doDelete() {
  if (!pending.value) return
  isSaving.value = true
  try {
    await config.value.api.delete(pending.value.id)
    notify('Record deleted.')
    deleteModal.hide()
    pending.value = null
    await load()
  } catch (e) {
    notify(getApiErrorMessage(e, 'Failed to delete.'), 'danger')
  } finally {
    isSaving.value = false
  }
}

function goHome() {
  window.history.pushState({}, '', '/dashboard.html')
  window.dispatchEvent(new PopStateEvent('popstate'))
}

onMounted(() => {
  formModal = new Modal(formModalEl.value)
  deleteModal = new Modal(deleteModalEl.value)
  toast = new Modal(toastEl.value, { autohide: false })
  load()
})

watch(() => props.modulePage, () => {
  load()
})

onBeforeUnmount(() => {
  formModal?.dispose(); deleteModal?.dispose(); toast?.dispose()
  window.clearTimeout(toastTimer)
  document.body.style.overflow = ''
})
</script>

