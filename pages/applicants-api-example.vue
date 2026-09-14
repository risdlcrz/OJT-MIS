<template>
  <section class="content">
    <!-- Loading State -->
    <div v-if="loading" class="alert alert-info">
      <i class="fas fa-spinner fa-spin"></i> Loading data...
    </div>

    <!-- Error State -->
    <div v-if="error" class="alert alert-danger alert-dismissible fade show">
      <strong>Error:</strong> {{ error }}
      <button type="button" class="btn-close" @click="error = null"></button>
    </div>

    <div class="card modern-card">
      <!-- Card Header -->
      <div class="card-header modern-card-header">
        <div>
          <h3 class="card-title">Applicants (Connected to API)</h3>
        </div>
        <div class="card-tools">
          <button
            type="button"
            class="btn btn-primary btn-sm modern-btn"
            @click="isApplicantModalOpen = true"
          >
            <i class="fas fa-plus"></i> Add Applicant
          </button>
        </div>
      </div>

      <!-- Card Body -->
      <div class="card-body">
        <table class="table table-hover modern-table">
          <thead class="text-center">
            <tr>
              <th>APPLICANT NO.</th>
              <th>LAST NAME</th>
              <th>FIRST NAME</th>
              <th>PROGRAM</th>
              <th>STATUS</th>
              <th>ACTIONS</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="applicant in applicants" :key="applicant.id">
              <td>{{ applicant.applicantNo }}</td>
              <td>{{ applicant.lastName }}</td>
              <td>{{ applicant.firstName }}</td>
              <td>{{ applicant.program || applicant.educationLevel || '' }}</td>
              <td>
                <span class="badge" :class="getStatusBadgeClass(applicant.status)">
                  {{ applicant.status || 'Pending' }}
                </span>
              </td>
              <td>
                <div class="btn-group">
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-secondary"
                    @click="viewApplicantDetails(applicant)"
                  >
                    <i class="fas fa-eye"></i>
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-primary"
                    @click="editApplicant(applicant)"
                  >
                    <i class="fas fa-edit"></i>
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-danger"
                    @click="deleteApplicant(applicant.id)"
                  >
                    <i class="fas fa-trash"></i>
                  </button>
                </div>
              </td>
            </tr>

            <!-- Empty State -->
            <tr v-if="applicants.length === 0">
              <td colspan="6" class="text-center text-muted py-4">
                No applicants found. Click "Add Applicant" to create one.
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Add/Edit Modal -->
    <div
      v-if="isApplicantModalOpen"
      class="modal fade show"
      style="display: block"
      tabindex="-1"
      role="dialog"
    >
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">
              {{ editingApplicant ? 'Edit Applicant' : 'Add New Applicant' }}
            </h5>
            <button
              type="button"
              class="btn-close"
              @click="isApplicantModalOpen = false"
            ></button>
          </div>
          <div class="modal-body">
            <form @submit.prevent="saveApplicant">
              <div class="row">
                <div class="col-md-6 mb-3">
                  <label for="firstName" class="form-label">First Name *</label>
                  <input
                    id="firstName"
                    v-model="formData.firstName"
                    type="text"
                    class="form-control"
                    required
                  />
                </div>
                <div class="col-md-6 mb-3">
                  <label for="lastName" class="form-label">Last Name *</label>
                  <input
                    id="lastName"
                    v-model="formData.lastName"
                    type="text"
                    class="form-control"
                    required
                  />
                </div>
              </div>
              <div class="row">
                <div class="col-md-6 mb-3">
                  <label for="email" class="form-label">Email</label>
                  <input
                    id="email"
                    v-model="formData.emailAddress"
                    type="email"
                    class="form-control"
                  />
                </div>
                <div class="col-md-6 mb-3">
                  <label for="phone" class="form-label">Phone Number</label>
                  <input
                    id="phone"
                    v-model="formData.phoneNumber"
                    type="tel"
                    class="form-control"
                  />
                </div>
              </div>
              <div class="row">
                <div class="col-md-6 mb-3">
                  <label for="program" class="form-label">Program</label>
                  <input
                    id="program"
                    v-model="formData.program"
                    type="text"
                    class="form-control"
                  />
                </div>
                <div class="col-md-6 mb-3">
                  <label for="status" class="form-label">Status</label>
                  <select v-model="formData.status" class="form-select">
                    <option value="">Select Status</option>
                    <option value="Pending">Pending</option>
                    <option value="Approved">Approved</option>
                    <option value="Rejected">Rejected</option>
                  </select>
                </div>
              </div>
            </form>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              @click="isApplicantModalOpen = false"
            >
              Cancel
            </button>
            <button
              type="button"
              class="btn btn-primary"
              @click="saveApplicant"
              :disabled="savingApplicant"
            >
              <span v-if="savingApplicant">
                <i class="fas fa-spinner fa-spin"></i> Saving...
              </span>
              <span v-else>Save Applicant</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Details Modal -->
    <div
      v-if="isDetailsModalOpen && selectedApplicant"
      class="modal fade show"
      style="display: block"
      tabindex="-1"
      role="dialog"
    >
      <div class="modal-dialog modal-lg">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Applicant Details</h5>
            <button
              type="button"
              class="btn-close"
              @click="isDetailsModalOpen = false"
            ></button>
          </div>
          <div class="modal-body">
            <dl class="row">
              <dt class="col-sm-3">Applicant No:</dt>
              <dd class="col-sm-9">{{ selectedApplicant.applicantNo }}</dd>

              <dt class="col-sm-3">Name:</dt>
              <dd class="col-sm-9">
                {{ selectedApplicant.firstName }} {{ selectedApplicant.lastName }}
              </dd>

              <dt class="col-sm-3">Email:</dt>
              <dd class="col-sm-9">{{ selectedApplicant.emailAddress || 'N/A' }}</dd>

              <dt class="col-sm-3">Phone:</dt>
              <dd class="col-sm-9">{{ selectedApplicant.phoneNumber || 'N/A' }}</dd>

              <dt class="col-sm-3">Program:</dt>
              <dd class="col-sm-9">{{ selectedApplicant.program || 'N/A' }}</dd>

              <dt class="col-sm-3">Status:</dt>
              <dd class="col-sm-9">
                <span class="badge" :class="getStatusBadgeClass(selectedApplicant.status)">
                  {{ selectedApplicant.status || 'Pending' }}
                </span>
              </dd>

              <dt class="col-sm-3">Date Applied:</dt>
              <dd class="col-sm-9">
                {{ formatDate(selectedApplicant.dateApplied) }}
              </dd>
            </dl>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-secondary"
              @click="isDetailsModalOpen = false"
            >
              Close
            </button>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { applicantsAPI } from '@/services/api'

// State
const applicants = ref([])
const loading = ref(false)
const savingApplicant = ref(false)
const error = ref(null)
const isApplicantModalOpen = ref(false)
const isDetailsModalOpen = ref(false)
const editingApplicant = ref(null)
const selectedApplicant = ref(null)

const formData = ref({
  firstName: '',
  lastName: '',
  emailAddress: '',
  phoneNumber: '',
  program: '',
  status: ''
})

// Lifecycle
onMounted(async () => {
  await fetchApplicants()
})

// Methods
async function fetchApplicants() {
  loading.value = true
  error.value = null
  try {
    const response = await applicantsAPI.getAll()
    applicants.value = response.data
  } catch (err) {
    error.value = `Failed to load applicants: ${err.message}`
    console.error('Error fetching applicants:', err)
  } finally {
    loading.value = false
  }
}

async function saveApplicant() {
  if (!formData.value.firstName || !formData.value.lastName) {
    error.value = 'First name and last name are required'
    return
  }

  savingApplicant.value = true
  error.value = null

  try {
    const data = { ...formData.value }

    if (editingApplicant.value) {
      // Update existing
      await applicantsAPI.update(editingApplicant.value.id, data)
      const index = applicants.value.findIndex(a => a.id === editingApplicant.value.id)
      if (index !== -1) {
        applicants.value[index] = { ...applicants.value[index], ...data }
      }
    } else {
      // Create new
      data.applicantNo = `A-${new Date().getFullYear()}-${String(applicants.value.length + 1).padStart(3, '0')}`
      data.dateApplied = new Date().toISOString()
      const response = await applicantsAPI.create(data)
      applicants.value.push(response.data)
    }

    isApplicantModalOpen.value = false
    resetForm()
  } catch (err) {
    error.value = `Failed to save applicant: ${err.message}`
    console.error('Error saving applicant:', err)
  } finally {
    savingApplicant.value = false
  }
}

async function deleteApplicant(id) {
  if (!confirm('Are you sure you want to delete this applicant?')) {
    return
  }

  error.value = null

  try {
    await applicantsAPI.delete(id)
    applicants.value = applicants.value.filter(a => a.id !== id)
  } catch (err) {
    error.value = `Failed to delete applicant: ${err.message}`
    console.error('Error deleting applicant:', err)
  }
}

function editApplicant(applicant) {
  editingApplicant.value = applicant
  formData.value = { ...applicant }
  isApplicantModalOpen.value = true
}

function viewApplicantDetails(applicant) {
  selectedApplicant.value = applicant
  isDetailsModalOpen.value = true
}

function resetForm() {
  formData.value = {
    firstName: '',
    lastName: '',
    emailAddress: '',
    phoneNumber: '',
    program: '',
    status: ''
  }
  editingApplicant.value = null
}

function getStatusBadgeClass(status) {
  switch (status) {
    case 'Approved':
      return 'bg-success'
    case 'Rejected':
      return 'bg-danger'
    case 'Pending':
      return 'bg-warning text-dark'
    default:
      return 'bg-secondary'
  }
}

function formatDate(dateString) {
  if (!dateString) return 'N/A'
  return new Date(dateString).toLocaleDateString('en-US', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}
</script>

<style scoped>
.modal {
  background-color: rgba(0, 0, 0, 0.5);
}

.modal.show {
  display: block;
}
</style>
