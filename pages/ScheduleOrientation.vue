<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">Schedule Orientation</h3>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered modern-table">
            <thead>
              <tr>
                <th>OJT NO.</th>
                <th>LAST NAME</th>
                <th>FIRST NAME</th>
                <th>SCHEDULE</th>
                <th>OFFICE</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="applicant in schedulableApplicants" :key="applicant.applicantNo">
                <td>{{ applicant.applicantNo }}</td>
                <td>{{ applicant.lastName }}</td>
                <td>{{ applicant.firstName }}</td>
                <td>{{ formatSchedule(applicant) }}</td>
                <td>{{ applicant.orientation?.office || '-' }}</td>
                <td>
                  <div class="d-flex justify-content-center" style="gap: 6px">
                    <button type="button" class="btn btn-outline-primary btn-sm modern-action" @click="openSchedule(applicant)">
                      <i class="fas fa-calendar-plus"></i>
                      <div class="action-label">Assign</div>
                    </button>
                    <button
                      v-if="applicant.orientation?.date"
                      type="button"
                      class="btn btn-outline-success btn-sm modern-action"
                      @click="confirmSchedule(applicant)"
                    >
                      <i class="fas fa-check"></i>
                      <div class="action-label">Confirm</div>
                    </button>
                    <button type="button" class="btn btn-outline-info btn-sm modern-action" @click="viewApplicant(applicant)">
                      <i class="fas fa-eye"></i>
                      <div class="action-label">View</div>
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="schedulableApplicants.length === 0">
                <td colspan="6" class="text-center text-muted py-4">No accepted applicants to schedule.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="scheduleApplicant" class="modal-backdrop fade show" @click="closeSchedule"></div>
    <div v-if="scheduleApplicant" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeSchedule">
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content modern-modal">
          <form @submit.prevent="saveSchedule">
            <div class="modal-header">
              <h5 class="modal-title">Assign Orientation Schedule</h5>
              <button type="button" class="close" aria-label="Close" @click="closeSchedule">&times;</button>
            </div>
            <div class="modal-body">
              <div class="form-row">
                <div class="form-group col-md-4">
                  <label for="orientationDate">Date</label>
                  <input id="orientationDate" v-model="scheduleForm.date" type="date" class="form-control" required>
                </div>
                <div class="form-group col-md-4">
                  <label for="orientationTime">Time</label>
                  <input id="orientationTime" v-model="scheduleForm.time" type="time" class="form-control">
                </div>
                <div class="form-group col-md-4">
                  <label for="orientationOffice">Office</label>
                  <select id="orientationOffice" v-model="scheduleForm.office" class="form-control" required>
                    <option value="">Select office</option>
                    <option v-for="office in offices" :key="office.code" :value="office.name">{{ office.name }}</option>
                  </select>
                </div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="submit" class="btn btn-primary modern-action">Save Schedule</button>
              <button type="button" class="btn btn-secondary modern-action" @click="closeSchedule">Cancel</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div v-if="viewedApplicant" class="modal-backdrop fade show" @click="viewedApplicant = null"></div>
    <div v-if="viewedApplicant" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="viewedApplicant = null">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content modern-modal">
          <div class="modal-header">
            <h5 class="modal-title">Applicant Details</h5>
            <button type="button" class="close" aria-label="Close" @click="viewedApplicant = null">&times;</button>
          </div>
          <div class="modal-body">
            <p><strong>{{ viewedApplicant.applicantNo }}</strong></p>
            <p>{{ viewedApplicant.lastName }}, {{ viewedApplicant.firstName }}</p>
            <p>Program: {{ viewedApplicant.program || '' }}</p>
            <hr>
            <p v-if="viewedApplicant.orientation">
              <strong>Orientation:</strong><br>
              Date: {{ viewedApplicant.orientation.date || '-' }}<br>
              Time: {{ viewedApplicant.orientation.time || '-' }}<br>
              Office: {{ viewedApplicant.orientation.office || '-' }}<br>
              Confirmed: {{ viewedApplicant.orientation.confirmed ? 'Yes' : 'No' }}
            </p>
            <p v-else><em>No schedule assigned</em></p>
          </div>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import '../assets/css/Applicants.css'

const APPLICANTS_KEY = 'ojt_applicants_v1'
const applicants = ref([])
const offices = ref([])
const scheduleApplicant = ref(null)
const viewedApplicant = ref(null)
const scheduleForm = ref({ date: '', time: '', office: '' })

const schedulableApplicants = computed(() =>
  applicants.value.filter((applicant) => applicant.accepted && !applicant.orientation?.confirmed)
)

function loadApplicants() {
  try {
    applicants.value = JSON.parse(localStorage.getItem(APPLICANTS_KEY) || '[]')
  } catch (error) {
    console.error('Failed to load saved applicants:', error)
    applicants.value = []
  }
}

function formatSchedule(applicant) {
  if (!applicant.orientation?.date) return '-'
  return `${applicant.orientation.date} ${applicant.orientation.time || ''}`.trim()
}

function openSchedule(applicant) {
  scheduleApplicant.value = applicant
  scheduleForm.value = {
    date: applicant.orientation?.date || '',
    time: applicant.orientation?.time || '',
    office: applicant.orientation?.office || ''
  }
}

function closeSchedule() {
  scheduleApplicant.value = null
}

function saveSchedule() {
  const index = applicants.value.findIndex((item) => item.applicantNo === scheduleApplicant.value?.applicantNo)
  if (index === -1) return

  applicants.value[index].orientation = {
    date: scheduleForm.value.date,
    time: scheduleForm.value.time,
    office: scheduleForm.value.office,
    confirmed: false
  }
  localStorage.setItem(APPLICANTS_KEY, JSON.stringify(applicants.value))
  closeSchedule()
}

function confirmSchedule(applicant) {
  if (!window.confirm(`Mark orientation as DONE for ${applicant.lastName}, ${applicant.firstName} on ${applicant.orientation.date}?`)) return
  const index = applicants.value.findIndex((item) => item.applicantNo === applicant.applicantNo)
  if (index === -1) return

  applicants.value[index].orientation.confirmed = true
  applicants.value[index].accepted = true
  applicants.value[index].rejected = false
  localStorage.setItem(APPLICANTS_KEY, JSON.stringify(applicants.value))
}

function viewApplicant(applicant) {
  viewedApplicant.value = applicant
}

async function loadOffices() {
  try {
    const response = await fetch('/assets/json/offices.json')
    if (!response.ok) throw new Error(`Failed to load offices: ${response.status}`)
    offices.value = (await response.json()).offices || []
  } catch (error) {
    console.error('Failed to load offices:', error)
    offices.value = []
  }
}

onMounted(() => {
  loadApplicants()
  loadOffices()
  window.addEventListener('storage', loadApplicants)
})

onBeforeUnmount(() => {
  window.removeEventListener('storage', loadApplicants)
})
</script>

<style scoped>
.action-label {
  font-size: 0.7rem;
}
</style>
