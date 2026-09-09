<template>
 
        <section class="content requests-content"> 
            <div class="card modern-card requests-card"> 
<div class="card-header modern-card-header">
    <div>
        <h3 class="card-title mb-1">List of Requests</h3>
    </div>
    <div class="card-tools">
        <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="isRequestModalOpen = true">
            <i class="fas fa-plus"></i> Intern Request
        </button>
    </div>
</div>
    <div class="card-body"> 
    <div class="table-responsive requests-table-wrapper">
    <table id="example1" class="table table-hover modern-table requests-table"> 
            <thead> 
                <tr class="text-center"> 
                    <th>REQUEST NO.</th> 
                    <th>REQUESTING OFFICE</th> 
                    <th>NO. OF OJT NEEDED</th> 
                    <th>SKILLS AND DESCRIPTION</th> 
                    <th>STATUS</th> 
                    <th>ACTIONS</th> 
                </tr> 
           </thead> 
    <tbody id="requestsTbody">
        <tr v-for="request in requests" :key="request.requestNo">
            <td>{{ request.requestNo }}</td>
            <td>{{ request.officeName || request.officeCode }}</td>
            <td>{{ request.count }}</td>
            <td>{{ request.skills }}<span v-if="request.description"> - {{ request.description }}</span></td>
            <td><span class="badge badge-info">{{ request.status || 'pending' }}</span></td>
            <td>
                <button type="button" class="btn btn-success btn-sm" title="Mark done" @click="removeRequest(request.requestNo)">
                    <i class="fas fa-check"></i>
                </button>
            </td>
        </tr>
        <tr v-if="requests.length === 0">
            <td colspan="6" class="text-center text-muted py-4">No requests saved yet.</td>
        </tr>
</tbody>
</table>
</div>
</div> 
</div> 
    <div
        id="modalInternRequest"
        class="modal fade"
        :class="{ show: isRequestModalOpen }"
        :style="{ display: isRequestModalOpen ? 'block' : 'none' }"
        tabindex="-1"
        role="dialog"
        :aria-hidden="!isRequestModalOpen"
        @click.self="isRequestModalOpen = false"
    >
        <div class="modal-dialog modal-dialog-centered" role="document" @click.stop>
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
                            <label class="col-sm-4 col-form-label font-weight-bold">Select Office</label>
                            <div class="col-sm-8">
                                <select class="form-control" id="requestOffice" v-model="requestForm.officeCode" required>
                                    <option selected disabled value="">Select an office</option>
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
                    </div>
                    <div class="modal-footer border-0 justify-content-end">
                        <button type="submit" class="btn btn-primary px-4 modern-btn">
                            <i class="fas fa-plus"></i> Submit
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
</section> 


</template>

<script setup>
import { onMounted, ref } from 'vue'
import '../assets/css/Request.css'

const isRequestModalOpen = ref(false)
const requests = ref([])
const offices = ref([])
const requestForm = ref({ officeCode: '', count: 1, skills: '', description: '' })

function loadRequests() {
    try {
        requests.value = JSON.parse(localStorage.getItem('ojt_intern_requests_v1') || '[]')
    } catch (error) {
        console.error('Failed to load saved requests:', error)
        requests.value = []
    }
}

function saveRequest() {
    const office = offices.value.find((item) => item.code === requestForm.value.officeCode)
    requests.value.push({
        requestNo: String(requests.value.length + 1),
        officeCode: requestForm.value.officeCode,
        officeName: office?.name || requestForm.value.officeCode,
        count: requestForm.value.count,
        skills: requestForm.value.skills.trim(),
        description: requestForm.value.description.trim(),
        status: 'pending',
        createdAt: new Date().toISOString()
    })
    localStorage.setItem('ojt_intern_requests_v1', JSON.stringify(requests.value))
    requestForm.value = { officeCode: '', count: 1, skills: '', description: '' }
    isRequestModalOpen.value = false
}

function removeRequest(requestNo) {
    requests.value = requests.value.filter((request) => request.requestNo !== requestNo)
    localStorage.setItem('ojt_intern_requests_v1', JSON.stringify(requests.value))
}

onMounted(async () => {
    loadRequests()
    const response = await fetch('/assets/json/offices.json')
    offices.value = (await response.json()).offices || []
})
</script>
