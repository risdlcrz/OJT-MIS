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
                                <option selected disabled value="">Select a department</option>
                                <option v-for="office in offices" :key="office.code" :value="office.code">{{ office.name }}</option>
                            </select>
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
    <div class="card-body"> 
    <div class="table-responsive">
        <table id="example1" class="table table-hover modern-table"> 
            <thead> 
                <tr class="text-center"> 
                    <th>REQUEST NO.</th> 
                    <th>REQUESTING DEPARTMENT</th> 
                    <th>NO. OF OJT NEEDED</th> 
                    <th>SKILLS AND DESCRIPTION</th> 
                    <th>STATUS</th> 
                    <th>ACTIONS</th> 
                </tr> 
           </thead> 
    <tbody id="requestsTbody">
        <tr v-for="request in requests" :key="request.id">
            <td class="text-dark">{{ request.id }}</td>
            <td class="text-dark">{{ request.officeName }}</td>
            <td class="text-dark">{{ request.count }} <span class="text-muted small">({{ request.remaining }} left)</span></td>
            <td class="text-dark">{{ request.skills }}<span v-if="request.description"> - {{ request.description }}</span></td>
            <td>
                <span class="badge badge-info">{{ request.status }}</span>
                <span v-if="request.hasSlots" class="badge badge-success">Has slots</span>
                <span v-else class="badge badge-secondary">Full</span>
            </td>
            <td>
                <button type="button" class="btn btn-danger btn-sm" title="Remove" :disabled="isLoading" @click="removeRequest(request.id)">
                    <i class="fas fa-trash"></i>
                </button>
            </td>
        </tr>
        <tr v-if="requests.length === 0">
            <td colspan="6" class="text-center text-muted py-4">{{ isLoading ? 'Loading requests...' : 'No requests saved yet.' }}</td>
        </tr>
        <tr v-if="loadError">
            <td colspan="6" class="text-center py-3"><span class="text-danger">{{ loadError }}</span></td>
        </tr>
</tbody>
</table>
</div>
</div> 
</div> 
</section> 


</template>

<script setup>
import { onMounted, ref, inject, computed, watch } from 'vue'
import { getApiErrorMessage, internRequestsAPI, signatoriesAPI } from '@/services/api'
import '../assets/css/Request.css'

const dialog = inject('dialog')
const { showAlert, showConfirm } = dialog || {}

const isRequestModalOpen = ref(false)
const requests = ref([])
const offices = ref([])
const signatories = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const requestForm = ref({ officeCode: '', count: 1, skills: '', description: '' })

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
</script>
