<template>
  <section class="content">
    <div class="ojt-stat-grid">
      <div class="ojt-stat">
        <span class="ojt-stat-label">Total slots</span>
        <span class="ojt-stat-value">{{ totals.count }}</span>
        <span class="ojt-stat-hint">Across open requests</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Filled</span>
        <span class="ojt-stat-value">{{ totals.filled }}</span>
        <span class="ojt-stat-hint">Interns hired</span>
      </div>
      <div class="ojt-stat" :class="{ 'ojt-stat--alert': totals.remaining > 0 }">
        <span class="ojt-stat-label">Available</span>
        <span class="ojt-stat-value">{{ totals.remaining }}</span>
        <span class="ojt-stat-hint">Open for hiring</span>
      </div>
      <div class="ojt-stat">
        <span class="ojt-stat-label">Awaiting HR</span>
        <span class="ojt-stat-value">{{ totals.pending }}</span>
        <span class="ojt-stat-hint">Slot requests pending</span>
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

    <!-- ============ Slot allocations ============ -->
    <div class="ojt-table-card mb-4">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Slot Allocations</h3>
          <p class="ojt-table-subtitle">Intern requests for <strong>{{ department || 'your department' }}</strong>. New requests are reviewed by HR before they open.</p>
        </div>
        <div class="ojt-table-tools">
          <button type="button" class="btn btn-outline-primary btn-sm" :disabled="isLoading" @click="load">
            <i class="fas fa-rotate me-1"></i> Refresh
          </button>
          <button type="button" class="btn btn-primary btn-sm" :disabled="!!scopeError" @click="openCreate">
            <i class="fas fa-plus me-1"></i> Request Slots
          </button>
        </div>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Request</th>
              <th>Skills / Workplan</th>
              <th>Slot Usage</th>
              <th class="col-num">Applicants</th>
              <th>Status</th>
              <th class="col-actions">Actions</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="6" :loading="isLoading" :empty="!isLoading && slots.length === 0"
                        icon="fas fa-layer-group" title="No slot allocations yet"
                        text="Request slots to let HR know how many interns your office can take.">
              <button type="button" class="btn btn-primary btn-sm mt-3" :disabled="!!scopeError" @click="openCreate">
                <i class="fas fa-plus me-1"></i> Request Slots
              </button>
            </TableState>
            <template v-if="!isLoading">
              <tr v-for="s in slots" :key="s.id">
                <td>
                  <span class="ojt-cell-primary">#{{ s.id }}</span>
                  <span class="ojt-cell-secondary">{{ formatDisplayDate(s.createdAt) }}</span>
                </td>
                <td class="col-wrap">
                  <span v-if="s.skills" class="d-block">{{ s.skills }}</span>
                  <span v-else class="ojt-na d-block">No skills listed</span>
                  <span v-if="s.description" class="ojt-cell-secondary">{{ s.description }}</span>
                </td>
                <td>
                  <div class="ojt-progress">
                    <div class="ojt-progress-track">
                      <div class="ojt-progress-fill" :style="{ width: `${usage(s)}%` }"></div>
                    </div>
                    <span class="ojt-progress-label">{{ s.filled }} / {{ s.count }}</span>
                  </div>
                  <span class="ojt-cell-secondary">{{ s.remainingSlots }} available</span>
                </td>
                <td class="col-num">{{ s.assignedApplicants }}</td>
                <td>
                  <StatusPill :status="slotStatus(s)" :label="slotStatus(s) === 'Pending' ? 'Awaiting HR' : ''" />
                </td>
                <td class="col-actions">
                  <button type="button" class="ojt-icon-btn" title="Edit slots" :aria-label="`Edit request ${s.id}`" @click="openEdit(s)">
                    <i class="fas fa-pen"></i>
                  </button>
                  <button v-if="s.status === 'Open'" type="button" class="ojt-icon-btn ojt-icon-btn--danger" title="Close request"
                          :aria-label="`Close request ${s.id}`" :disabled="busyId === s.id" @click="setStatus(s, 'Closed')">
                    <i class="fas fa-lock"></i>
                  </button>
                  <button v-else-if="s.status === 'Closed'" type="button" class="ojt-icon-btn ojt-icon-btn--approve" title="Reopen request"
                          :aria-label="`Reopen request ${s.id}`" :disabled="busyId === s.id" @click="setStatus(s, 'Open')">
                    <i class="fas fa-lock-open"></i>
                  </button>
                </td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ============ Applicant requests ============ -->
    <div class="ojt-table-card">
      <div class="ojt-table-head">
        <div>
          <h3 class="ojt-table-title">Applicant Requests</h3>
          <p class="ojt-table-subtitle">Applicants who asked to be placed in your office.</p>
        </div>
      </div>

      <div class="ojt-toolbar">
        <div class="ojt-search">
          <i class="fas fa-search"></i>
          <input v-model="search" type="search" class="form-control form-control-sm"
                 placeholder="Search name, school, applicant no." aria-label="Search applicants" />
        </div>
        <div class="ojt-segment" role="tablist" aria-label="Filter by status">
          <button v-for="tab in applicantTabs" :key="tab" type="button" role="tab"
                  :class="{ active: applicantFilter === tab }" :aria-selected="applicantFilter === tab"
                  @click="applicantFilter = tab">
            {{ tab }}<span class="ojt-segment-count">{{ applicantCount(tab) }}</span>
          </button>
        </div>
        <span class="ojt-toolbar-count">{{ applicantRows.length }} applicant(s)</span>
      </div>

      <div class="table-responsive">
        <table class="table table-hover ojt-table">
          <thead>
            <tr>
              <th>Applicant</th>
              <th>School &amp; Program</th>
              <th>Level</th>
              <th class="col-num">Req. Hours</th>
              <th>Request</th>
              <th class="col-date">Applied</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            <TableState :colspan="7" :loading="isLoading" :empty="!isLoading && applicantRows.length === 0"
                        icon="fas fa-inbox"
                        :title="applicants.length ? 'No applicants match your filters' : 'No applicant requests yet'"
                        :text="applicants.length ? 'Try another status or search.' : 'Applicants routed to your office will appear here.'" />
            <template v-if="!isLoading">
              <tr v-for="a in applicantRows" :key="a.id">
                <td>
                  <span class="ojt-cell-primary">{{ a.fullName }}</span>
                  <span class="ojt-cell-secondary">{{ a.applicantNo || 'No applicant no.' }}</span>
                </td>
                <td>
                  <span v-if="a.schoolName" class="d-block">{{ a.schoolName }}</span>
                  <span v-else class="ojt-na d-block">School not set</span>
                  <span class="ojt-cell-secondary">{{ a.program || 'Program not set' }}</span>
                </td>
                <td>
                  <span v-if="a.educationLevel">{{ a.educationLevel }}</span>
                  <span v-else class="ojt-na">Not set</span>
                </td>
                <td class="col-num">{{ a.requiredHours || 0 }}</td>
                <td>
                  <span v-if="a.requestNoLabel || a.requestNo">{{ a.requestNoLabel || `#${a.requestNo}` }}</span>
                  <span v-else class="ojt-na">Unassigned</span>
                </td>
                <td class="col-date">{{ formatDisplayDate(a.createdAt) }}</td>
                <td><StatusPill :status="a.status" :label="a.status === 'Hired' ? 'Hired' : ''" /></td>
              </tr>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ============ Request / edit slots modal ============ -->
    <Teleport to="body">
      <template v-if="formOpen">
        <div class="modal fade show d-block" tabindex="-1" role="dialog" aria-modal="true" aria-labelledby="slotFormTitle" @keydown.esc="closeForm">
          <div class="modal-dialog modal-dialog-centered">
            <div class="modal-content">
              <form novalidate @submit.prevent="saveForm">
                <div class="modal-header">
                  <h5 id="slotFormTitle" class="modal-title">{{ form.id ? `Edit Request #${form.id}` : 'Request Intern Slots' }}</h5>
                  <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeForm"></button>
                </div>
                <div class="modal-body">
                  <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>
                  <p v-if="!form.id" class="small text-muted">
                    This request goes to HR for review. Once HR opens it, applicants can be hired into these slots.
                  </p>
                  <div class="mb-3">
                    <label for="slotCount" class="form-label fw-semibold">Number of interns</label>
                    <input id="slotCount" v-model.number="form.count" type="number" class="form-control"
                           :min="Math.max(1, form.filled)" max="9999" required :disabled="isSaving" />
                    <div v-if="form.filled" class="form-text">{{ form.filled }} slot(s) are already filled, so the count can't go below that.</div>
                  </div>
                  <div class="mb-3">
                    <label for="slotSkills" class="form-label fw-semibold">Skills needed / workplan</label>
                    <textarea id="slotSkills" v-model="form.skills" class="form-control" rows="3" maxlength="500" required :disabled="isSaving"></textarea>
                  </div>
                  <div class="mb-0">
                    <label for="slotDescription" class="form-label fw-semibold">Description <span class="text-muted fw-normal">(optional)</span></label>
                    <textarea id="slotDescription" v-model="form.description" class="form-control" rows="3" maxlength="1000" :disabled="isSaving"></textarea>
                  </div>
                </div>
                <div class="modal-footer">
                  <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeForm">Cancel</button>
                  <button type="submit" class="btn btn-primary" :disabled="isSaving">
                    <span v-if="isSaving" class="spinner-border spinner-border-sm me-1"></span>
                    {{ form.id ? 'Save Changes' : 'Submit Request' }}
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
        <div class="modal-backdrop fade show" @click="closeForm"></div>
      </template>
    </Teleport>
  </section>
</template>

<script setup>
import { computed, inject, onMounted, reactive, ref } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { getApiErrorMessage, supervisorAPI } from '@/services/api'
import { formatDisplayDate } from '@/utils/datetime'
import { useDepartment } from '@/utils/supervisor'

const dialog = inject('dialog', null)
const { department, scopeError, loadDepartment } = useDepartment()

const slots = ref([])
const applicants = ref([])
const isLoading = ref(true)
const error = ref('')
const notice = ref('')
const busyId = ref(null)
const search = ref('')
const applicantTabs = ['Pending', 'Accepted', 'Hired', 'Rejected', 'All']
const applicantFilter = ref('Pending')

const formOpen = ref(false)
const isSaving = ref(false)
const formError = ref('')
const form = reactive({ id: 0, count: 1, filled: 0, skills: '', description: '' })

const totals = computed(() => {
  const open = slots.value.filter((s) => s.status === 'Open')
  return {
    count: open.reduce((n, s) => n + (s.count || 0), 0),
    filled: open.reduce((n, s) => n + (s.filled || 0), 0),
    remaining: open.reduce((n, s) => n + (s.remainingSlots || 0), 0),
    pending: slots.value.filter((s) => s.status === 'Pending').length
  }
})

function usage(s) {
  return s.count > 0 ? Math.min(100, Math.round((s.filled / s.count) * 100)) : 0
}

function slotStatus(s) {
  if (s.status === 'Open' && s.remainingSlots === 0) return 'Full'
  return s.status
}

function applicantCount(tab) {
  return tab === 'All' ? applicants.value.length : applicants.value.filter((a) => a.status === tab).length
}

const applicantRows = computed(() => {
  const term = search.value.trim().toLowerCase()
  return applicants.value
    .filter((a) => applicantFilter.value === 'All' || a.status === applicantFilter.value)
    .filter((a) => !term || [a.fullName, a.schoolName, a.program, a.applicantNo]
      .some((v) => String(v || '').toLowerCase().includes(term)))
})

function openCreate() {
  Object.assign(form, { id: 0, count: 1, filled: 0, skills: '', description: '' })
  formError.value = ''
  formOpen.value = true
}

function openEdit(s) {
  Object.assign(form, { id: s.id, count: s.count, filled: s.filled || 0, skills: s.skills || '', description: s.description || '' })
  formError.value = ''
  formOpen.value = true
}

function closeForm() {
  if (!isSaving.value) formOpen.value = false
}

async function saveForm() {
  formError.value = ''
  const count = Number(form.count)
  if (!Number.isInteger(count) || count < 1) { formError.value = 'Enter at least 1 slot.'; return }
  if (count < form.filled) { formError.value = `The count can't go below the ${form.filled} filled slot(s).`; return }
  if (!form.skills.trim()) { formError.value = 'Skills needed / workplan is required.'; return }

  isSaving.value = true
  try {
    const payload = { count, skills: form.skills.trim(), description: form.description.trim() }
    const result = form.id
      ? await supervisorAPI.updateSlots(form.id, payload)
      : await supervisorAPI.requestSlots(payload)
    notice.value = result?.message || 'Saved.'
    formOpen.value = false
    await load(false)
  } catch (e) {
    formError.value = getApiErrorMessage(e, 'Failed to save the slot request.')
  } finally {
    isSaving.value = false
  }
}

async function setStatus(s, status) {
  const closing = status === 'Closed'
  const message = closing
    ? `Close request #${s.id}? HR won't be able to hire into its ${s.remainingSlots} remaining slot(s) until you reopen it.`
    : `Reopen request #${s.id} for hiring?`
  const ok = dialog?.showConfirm ? await dialog.showConfirm(message, closing ? 'Close Request' : 'Reopen Request') : window.confirm(message)
  if (!ok) return

  busyId.value = s.id
  error.value = ''
  try {
    const result = await supervisorAPI.updateSlots(s.id, { count: s.count, skills: s.skills, description: s.description, status })
    notice.value = result?.message || 'Request updated.'
    await load(false)
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Failed to update the request.')
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
    const [s, a] = await Promise.all([supervisorAPI.slots(), supervisorAPI.applicants()])
    slots.value = Array.isArray(s) ? s : []
    applicants.value = Array.isArray(a) ? a : []
  } catch (e) {
    error.value = getApiErrorMessage(e, 'Unable to load slot allocations.')
  } finally {
    isLoading.value = false
  }
}

onMounted(load)
</script>
