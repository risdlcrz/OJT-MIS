<template>
 
        <section class="content"> 
            <div class="card modern-card"> 
<div class="card-header modern-card-header">
    <div>
        <h3 class="card-title mb-1">List of Requests</h3>
    </div>
    <div class="card-tools">
        <button type="button" class="btn btn-primary btn-sm modern-btn" @click="isRequestModalOpen = true">
            <i class="fas fa-plus"></i> Intern Request
        </button>
    </div>
</div>
<div class="modal fade" id="modalInternRequest" :class="{ show: isRequestModalOpen }" :style="{ display: isRequestModalOpen ? 'block' : 'none' }" tabindex="-1" role="dialog" :aria-hidden="!isRequestModalOpen">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
            <div class="modal-header modern-modal-header">
                <h5 class="modal-title font-weight-bold">Intern Request</h5>
                <button type="button" class="close" aria-label="Close" @click="isRequestModalOpen = false">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <form id="internRequestForm" @submit.prevent="saveRequest">
                <div class="modal-body p-4">
                    <div class="form-group row">
                        <label class="col-sm-4 col-form-label font-weight-bold">Select Department</label>
                        <div class="col-sm-8">
                            <select class="form-control" id="requestOffice" v-model="requestForm.officeCode" required>
                                <option selected disabled value="">{{ filteredOffices.length === 0 ? 'No departments available' : 'Select a department' }}</option>
                                <option v-for="office in filteredOffices" :key="office.code" :value="office.code">{{ office.name }}</option>
                            </select>
                            <small v-if="filteredOffices.length === 0" class="text-muted d-block mt-1">No supervisors are registered for any department.</small>
                        </div>
                    </div>
                    <div class="form-group row">
                        <label class="col-sm-4 col-form-label font-weight-bold">No. of OJT needed</label>
                        <div class="col-sm-8">
                            <input v-model.number="requestForm.count" type="number" class="form-control" id="requestNumber" min="1" placeholder="Input Number" required>
                        </div>
                    </div>
                    <div class="form-group row">
                        <label class="col-sm-4 col-form-label font-weight-bold">Skills Needed / Workplan</label>
                        <div class="col-sm-8">
                            <textarea v-model="requestForm.skills" class="form-control" id="requestSkills" rows="3" placeholder="Input Text" required></textarea>
                        </div>
                    </div>
                    <div class="form-group row">
                        <label class="col-sm-4 col-form-label font-weight-bold">Description</label>
                        <div class="col-sm-8">
                            <textarea v-model="requestForm.description" class="form-control" id="requestDescription" rows="3" placeholder="Input Text"></textarea>
                        </div>
                    </div>
                    <div class="form-group row" v-if="matchedSignatory" style="background: #fff7ed; border: 1px solid #f97316; border-radius: 0.5rem; padding: 1rem;">
                        <div class="col-12">
                            <label class="font-weight-bold" style="color: #f97316;">Auto-Assigned Supervisor</label>
                            <div class="row mt-2">
                                <div class="col-sm-6">
                                    <label class="col-sm-4 col-form-label text-muted small">Name</label>
                                    <div class="col-sm-8 font-weight-bold">{{ matchedSignatory.name }}</div>
                                </div>
                                <div class="col-sm-6">
                                    <label class="col-sm-4 col-form-label text-muted small">Position</label>
                                    <div class="col-sm-8">{{ matchedSignatory.position }}</div>
                                </div>
                                <div class="col-sm-6">
                                    <label class="col-sm-4 col-form-label text-muted small">Department</label>
                                    <div class="col-sm-8">{{ matchedSignatory.department }}</div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
<div class="modal-footer border-0 justify-content-end">
    <button type="submit" class="btn btn-primary px-4 modern-btn" :disabled="isSaving">
        <i class="fas fa-plus"></i> {{ isSaving ? 'Submitting...' : 'Submit' }}
    </button>
    <button type="button" class="btn btn-outline-secondary px-4" @click="isRequestModalOpen = false">
        <i class="fas fa-times"></i> Close
    </button>
</div>
            </form>
        </div>
    </div>
</div>
        <div v-if="isRequestModalOpen" class="modal-backdrop fade show" @click="isRequestModalOpen = false"></div>
    <div v-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center gap-2 m-3 mb-0">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" @click="loadRequests">Retry</button>
    </div>

    <div class="ojt-toolbar">
        <div class="ojt-search">
            <i class="fas fa-search"></i>
            <input v-model="search" type="search" class="form-control form-control-sm"
                   placeholder="Search department or skills" aria-label="Search requests" />
        </div>
        <div class="ojt-segment" role="tablist" aria-label="Filter by status">
            <button v-for="tab in statusTabs" :key="tab" type="button" role="tab"
                    :class="{ active: statusFilter === tab }" :aria-selected="statusFilter === tab"
                    @click="statusFilter = tab">
                {{ tab }}<span class="ojt-segment-count">{{ countFor(tab) }}</span>
            </button>
        </div>
        <span class="ojt-toolbar-count">{{ visibleRequests.length }} of {{ requests.length }} request(s)</span>
    </div>

    <div class="table-responsive">
        <table id="example1" class="table table-hover ojt-table">
            <thead>
                <tr>
                    <th>Request</th>
                    <th>Requesting Department</th>
                    <th>Slot Usage</th>
                    <th>Skills &amp; Description</th>
                    <th>Status</th>
                    <th class="col-actions">Actions</th>
                </tr>
            </thead>
            <tbody id="requestsTbody">
                <TableState :colspan="6" :loading="isLoading" :empty="!isLoading && visibleRequests.length === 0"
                            icon="far fa-file-alt"
                            :title="requests.length ? 'No requests match your filters' : 'No intern requests yet'"
                            :text="requests.length ? 'Try a different search or status.' : 'Create a request or wait for a department supervisor to submit one.'" />
                <template v-if="!isLoading">
                    <tr v-for="request in visibleRequests" :key="request.id">
                        <td>
                            <span class="ojt-cell-primary">#{{ request.id }}</span>
                            <span class="ojt-cell-secondary">{{ formatDisplayDate(request.createdAt) }}</span>
                        </td>
                        <td>
                            <span v-if="request.officeName" class="ojt-cell-primary">{{ request.officeName }}</span>
                            <span v-else class="ojt-na">No department</span>
                        </td>
                        <td>
                            <div class="ojt-progress">
                                <div class="ojt-progress-track">
                                    <div class="ojt-progress-fill" :style="{ width: `${usage(request)}%` }"></div>
                                </div>
                                <span class="ojt-progress-label">{{ request.filled || 0 }} / {{ request.count }}</span>
                            </div>
                            <span class="ojt-cell-secondary">{{ request.remaining }} available</span>
                        </td>
                        <td class="col-wrap">
                            <span v-if="request.skills" class="d-block">{{ request.skills }}</span>
                            <span v-else class="ojt-na d-block">No skills listed</span>
                            <span v-if="request.description" class="ojt-cell-secondary">{{ request.description }}</span>
                        </td>
                        <td>
                            <StatusPill :status="displayStatus(request)" :label="displayStatus(request) === 'Pending' ? 'Awaiting approval' : ''" />
                        </td>
                        <td class="col-actions">
                            <button v-if="request.status === 'Pending'" type="button" class="ojt-icon-btn ojt-icon-btn--approve"
                                    title="Open for hiring" :aria-label="`Open request ${request.id} for hiring`"
                                    :disabled="busyId === request.id" @click="openForHiring(request)">
                                <i class="fas fa-check"></i>
                            </button>
                            <button type="button" class="ojt-icon-btn ojt-icon-btn--danger" title="Remove"
                                    :aria-label="`Remove request ${request.id}`" :disabled="busyId === request.id"
                                    @click="removeRequest(request.id)">
                                <i class="fas fa-trash"></i>
                            </button>
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
import { onMounted, ref, inject, computed, watch } from 'vue'
import StatusPill from '@/components/StatusPill.vue'
import TableState from '@/components/TableState.vue'
import { getApiErrorMessage, internRequestsAPI, signatoriesAPI } from '@/services/api'
import { formatDisplayDate } from '@/utils/datetime'
import '../assets/css/Request.css'

const dialog = inject('dialog')
const { showConfirm } = dialog || {}

const isRequestModalOpen = ref(false)
const requests = ref([])
const search = ref('')
const statusTabs = ['All', 'Pending', 'Open', 'Full', 'Closed']
const statusFilter = ref('All')
const busyId = ref(null)

/* "Full" is derived: an Open request with no remaining slots. */
function displayStatus(request) {
  if ((request.status || '') === 'Open' && !request.hasSlots) return 'Full'
  return request.status || 'Open'
}

function countFor(tab) {
  return tab === 'All' ? requests.value.length : requests.value.filter((r) => displayStatus(r) === tab).length
}

const visibleRequests = computed(() => {
  const term = search.value.trim().toLowerCase()
  return requests.value
    .filter((r) => statusFilter.value === 'All' || displayStatus(r) === statusFilter.value)
    .filter((r) => !term || [r.officeName, r.officeCode, r.skills, r.description, String(r.id)]
      .some((v) => String(v || '').toLowerCase().includes(term)))
})

function usage(r) {
  return r.count > 0 ? Math.min(100, Math.round(((r.filled || 0) / r.count) * 100)) : 0
}

/* Supervisors submit slot requests as Pending; HR opens them for hiring. */
async function openForHiring(request) {
  const ok = await showConfirm(`Open request #${request.id} (${request.officeName}, ${request.count} slot(s)) for hiring?`, 'Open Request')
  if (!ok) return
  busyId.value = request.id
  loadError.value = ''
  try {
    await internRequestsAPI.setStatus(request, 'Open')
    await loadRequests()
  } catch (e) {
    loadError.value = getApiErrorMessage(e, 'Failed to open the request.')
  } finally {
    busyId.value = null
  }
}
const offices = ref([])
const signatories = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const requestForm = ref({ officeCode: '', count: 1, skills: '', description: '' })

const filteredOffices = computed(() => {
  return offices.value.filter(office => {
    return signatories.value.some(sig =>
      office.code.toLowerCase() === sig.department?.toLowerCase() ||
      office.name.toLowerCase() === sig.department?.toLowerCase() ||
      sig.department?.toLowerCase().includes(office.code.toLowerCase()) ||
      office.name.toLowerCase().includes(sig.department?.toLowerCase() || '')
    )
  })
})

const matchedSignatory = computed(() => {
  if (!requestForm.value.officeCode) return null
  const office = offices.value.find(o => o.code === requestForm.value.officeCode)
  if (!office) return null
  return signatories.value.find(s => 
    s.department?.toLowerCase().includes(office.code.toLowerCase()) ||
    office.name.toLowerCase().includes(s.department?.toLowerCase() || '')
  ) || null
})

/* Lahat ng datos ay galing sa backend, walang localStorage. */
async function loadRequests() {
    isLoading.value = true
    loadError.value = ''
    try {
        requests.value = await internRequestsAPI.getAll()
    } catch (e) {
        loadError.value = getApiErrorMessage(e, 'Failed to load requests.')
    } finally {
        isLoading.value = false
    }
}

async function saveRequest() {
    const form = requestForm.value
    if (!form.officeCode) { loadError.value = 'Please select a requesting office.'; return }
    if (!form.count || form.count < 1) { loadError.value = 'Slots must be at least 1.'; return }

    const office = offices.value.find((item) => item.code === form.officeCode)

    isSaving.value = true
    loadError.value = ''
    try {
        await internRequestsAPI.create({
            id: 0,
            officeCode: form.officeCode,
            officeName: office?.name || form.officeCode,
            count: Number(form.count),
            skills: form.skills.trim(),
            description: form.description.trim(),
            status: 'Open',
            signatoryId: matchedSignatory.value?.id || null
        })
        requestForm.value = { officeCode: '', count: 1, skills: '', description: '' }
        isRequestModalOpen.value = false
        await loadRequests()
    } catch (e) {
        loadError.value = getApiErrorMessage(e, 'Failed to save request.')
    } finally {
        isSaving.value = false
    }
}

async function removeRequest(id) {
    const ok = await showConfirm('Remove this intern request?', 'Confirm Delete')
    if (!ok) return
    loadError.value = ''
    try {
        await internRequestsAPI.delete(id)
        await loadRequests()
    } catch (e) {
        loadError.value = getApiErrorMessage(e, 'Failed to remove request.')
    }
}

onMounted(async () => {
    await loadRequests()
    try {
        const [officesRes, signatoriesRes] = await Promise.all([
            fetch('/assets/json/offices.json'),
            signatoriesAPI.getAll()
        ])
        offices.value = (await officesRes.json()).offices || []
        signatories.value = signatoriesRes || []
    } catch {
        offices.value = []
        signatories.value = []
    }
})

// Reset officeCode if selected office is no longer available
watch(() => filteredOffices.value, (newFilteredOffices) => {
  if (requestForm.value.officeCode && !newFilteredOffices.find(o => o.code === requestForm.value.officeCode)) {
    requestForm.value.officeCode = ''
  }
}, { deep: true })
</script>
