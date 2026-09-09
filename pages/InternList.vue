<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">List of Interns</h3>
      </div>

      <div class="card-body">
        <div class="table-responsive">
          <table id="internListTable" class="table table-hover table-bordered modern-table">
            <thead>
              <tr class="text-center">
                <th>OJT NO.</th>
                <th>LAST NAME</th>
                <th>FIRST NAME</th>
                <th>COURSE</th>
                <th>OFFICE</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="intern in interns" :key="intern.applicantNo" class="text-center">
                <td>{{ intern.applicantNo }}</td>
                <td>{{ intern.lastName }}</td>
                <td>{{ intern.firstName }}</td>
                <td>{{ intern.program || '' }}</td>
                <td>{{ intern.orientation?.office || '-' }}</td>
                <td>
                  <div class="d-flex justify-content-center" style="gap: 5px">
                    <button
                      type="button"
                      class="btn btn-outline-primary btn-sm modern-action d-inline-flex flex-column align-items-center p-2"
                      style="min-width: 60px"
                      @click="viewIntern(intern)"
                    >
                      <i class="fas fa-eye fa-lg mb-1"></i>
                      <span class="intern-action-label">View</span>
                    </button>
                    <button
                      type="button"
                      class="btn btn-outline-success btn-sm modern-action d-inline-flex flex-column align-items-center p-2"
                      style="min-width: 60px"
                      @click="printIntern(intern)"
                    >
                      <i class="fa fa-print fa-lg mb-1"></i>
                      <span class="intern-action-label">Print</span>
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="interns.length === 0">
                <td colspan="6" class="text-center text-muted py-4">No interns found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="selectedIntern" class="modal-backdrop fade show" @click="selectedIntern = null"></div>
    <div
      v-if="selectedIntern"
      class="modal fade show"
      style="display: block"
      tabindex="-1"
      role="dialog"
      aria-modal="true"
      @click.self="selectedIntern = null"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
          <div class="modal-header modern-modal-header">
            <h5 class="modal-title">Intern Details</h5>
            <button type="button" class="close" aria-label="Close" @click="selectedIntern = null">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body">
            <p><strong>{{ selectedIntern.applicantNo }}</strong></p>
            <p>{{ selectedIntern.lastName }}, {{ selectedIntern.firstName }}</p>
            <p>Program: {{ selectedIntern.program || '' }}</p>
            <hr>
            <p class="mb-1"><strong>Orientation:</strong></p>
            <p class="mb-1">Date: {{ selectedIntern.orientation?.date || '-' }}</p>
            <p class="mb-1">Time: {{ selectedIntern.orientation?.time || '-' }}</p>
            <p class="mb-1">Office: {{ selectedIntern.orientation?.office || '-' }}</p>
            <p class="mb-0">Confirmed: {{ selectedIntern.orientation?.confirmed ? 'Yes' : 'No' }}</p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import '../assets/css/InternList.css'

const APPLICANTS_KEY = 'ojt_applicants_v1'
const applicants = ref([])
const selectedIntern = ref(null)

const interns = computed(() =>
  applicants.value.filter((applicant) => applicant.accepted && applicant.orientation?.confirmed)
)

function loadApplicants() {
  try {
    applicants.value = JSON.parse(localStorage.getItem(APPLICANTS_KEY) || '[]')
  } catch (error) {
    console.error('Failed to load saved applicants:', error)
    applicants.value = []
  }
}

function viewIntern(intern) {
  selectedIntern.value = intern
}

function printIntern(intern) {
  const previousTitle = document.title
  document.title = `Intern-${intern.applicantNo}`
  window.print()
  document.title = previousTitle
}

onMounted(() => {
  loadApplicants()
  window.addEventListener('storage', loadApplicants)
})

onBeforeUnmount(() => {
  window.removeEventListener('storage', loadApplicants)
})
</script>

<style scoped>
.intern-action-label {
  font-size: 0.7rem;
  font-weight: bold;
  text-transform: uppercase;
}
</style>
