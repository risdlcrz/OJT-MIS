<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">Issuance of COC</h3>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered modern-table">
            <thead>
              <tr>
                <th>OJT NO.</th>
                <th>LAST NAME</th>
                <th>FIRST NAME</th>
                <th>OFFICE</th>
                <th>TOTAL HOURS</th>
                <th>RENDERED HOURS</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="applicant in eligibleApplicants" :key="applicant.applicantNo">
                <td>{{ applicant.applicantNo }}</td>
                <td>{{ applicant.lastName || '' }}</td>
                <td>{{ applicant.firstName || '' }}</td>
                <td>{{ applicant.orientation?.office || applicant.office || '' }}</td>
                <td>{{ applicant.requiredHours }}</td>
                <td>{{ renderedHours(applicant) }}</td>
                <td>
                  <div class="d-flex justify-content-center" style="gap: 5px">
                    <button type="button" class="btn btn-outline-primary btn-sm modern-action" @click="openCoc(applicant)">
                      <i class="fas fa-eye"></i>
                      <div class="action-label">View</div>
                    </button>
                    <button type="button" class="btn btn-outline-success btn-sm modern-action" @click="printCoc(applicant)">
                      <i class="fa fa-print"></i>
                      <div class="action-label">Print</div>
                    </button>
                  </div>
                </td>
              </tr>
              <tr v-if="eligibleApplicants.length === 0">
                <td colspan="7" class="text-center text-muted py-4">No interns eligible for COC issuance.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="selectedApplicant" class="modal-backdrop fade show" @click="closeCoc"></div>
    <div v-if="selectedApplicant" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeCoc">
      <div class="modal-dialog modal-dialog-centered modal-md">
        <div class="modal-content">
          <div class="modal-header bg-orange text-white">
            <h5 class="modal-title">Certificate of Completion</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeCoc">&times;</button>
          </div>
          <form @submit.prevent="saveCoc">
            <div class="modal-body">
              <div class="form-group">
                <label>OJT No.</label>
                <input :value="selectedApplicant.applicantNo" class="form-control" readonly>
              </div>
              <div class="form-row">
                <div class="form-group col-md-6">
                  <label>Last Name</label>
                  <input :value="selectedApplicant.lastName || ''" class="form-control" readonly>
                </div>
                <div class="form-group col-md-6">
                  <label>First Name</label>
                  <input :value="selectedApplicant.firstName || ''" class="form-control" readonly>
                </div>
              </div>
              <div class="form-group">
                <label>Office</label>
                <input :value="selectedApplicant.orientation?.office || selectedApplicant.office || ''" class="form-control" readonly>
              </div>
              <div class="form-row">
                <div class="form-group col-md-6">
                  <label>Total Required Hours</label>
                  <input :value="selectedApplicant.requiredHours || ''" class="form-control" readonly>
                </div>
                <div class="form-group col-md-6">
                  <label>Rendered Hours</label>
                  <input :value="renderedHours(selectedApplicant)" class="form-control" readonly>
                </div>
              </div>
              <div class="form-group">
                <label>Date Issued</label>
                <input v-model="cocForm.dateIssued" type="date" class="form-control">
              </div>
              <div class="form-group">
                <label>Certificate No.</label>
                <input v-model="cocForm.number" class="form-control">
              </div>
              <div class="form-group">
                <label>Remarks</label>
                <textarea v-model="cocForm.remarks" class="form-control" rows="2"></textarea>
              </div>
            </div>
            <div class="modal-footer">
              <button type="submit" class="btn btn-success modern-action">Save</button>
              <button type="button" class="btn btn-primary modern-action" @click="printCoc(selectedApplicant)">Print</button>
              <button type="button" class="btn btn-outline-secondary modern-action" @click="closeCoc">Close</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import '../assets/css/Forms.css'

const APPLICANTS_KEY = 'ojt_applicants_v1'
const DTR_PREFIX = 'dtr_'
const COC_KEY_PREFIX = 'coc_'
const applicants = ref([])
const selectedApplicant = ref(null)
const cocForm = ref({ dateIssued: '', number: '', remarks: '' })

const eligibleApplicants = computed(() =>
  applicants.value.filter((applicant) => {
    const required = Number(applicant.requiredHours || 0)
    return required > 0 && renderedHours(applicant) >= required
  })
)

function readJson(key, fallback) {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch (error) {
    console.error(`Failed to read ${key}:`, error)
    return fallback
  }
}

function loadApplicants() {
  applicants.value = readJson(APPLICANTS_KEY, [])
}

function toMinutes(time) {
  if (!time) return null
  const parts = String(time).split(':')
  if (parts.length < 2) return null
  const hours = Number.parseInt(parts[0], 10)
  const minutes = Number.parseInt(parts[1], 10)
  return Number.isNaN(hours) || Number.isNaN(minutes) ? null : hours * 60 + minutes
}

function sumRenderedHours(records) {
  if (!Array.isArray(records)) return 0
  const total = records.reduce((sum, record) => {
    if (record.total !== undefined && record.total !== null && record.total !== '') {
      const value = Number.parseFloat(record.total)
      return Number.isNaN(value) ? sum : sum + value
    }

    const amIn = toMinutes(record.am_in)
    const amOut = toMinutes(record.am_out)
    const pmIn = toMinutes(record.pm_in)
    const pmOut = toMinutes(record.pm_out)
    let minutes = 0
    if (amIn !== null && amOut !== null && amOut >= amIn) minutes += Math.max(0, Math.min(amOut, 720) - amIn)
    if (pmIn !== null && pmOut !== null && pmOut >= pmIn) minutes += Math.max(0, pmOut - Math.max(pmIn, 780))
    return sum + minutes / 60
  }, 0)
  return Math.round(total * 100) / 100
}

function renderedHours(applicant) {
  return sumRenderedHours(readJson(`${DTR_PREFIX}${applicant.applicantNo}`, []))
}

function loadCoc(applicantNo) {
  return readJson(`${COC_KEY_PREFIX}${applicantNo}`, {})
}

function openCoc(applicant) {
  selectedApplicant.value = applicant
  const coc = loadCoc(applicant.applicantNo)
  cocForm.value = {
    dateIssued: coc.dateIssued || new Date().toISOString().slice(0, 10),
    number: coc.number || '',
    remarks: coc.remarks || ''
  }
}

function closeCoc() {
  selectedApplicant.value = null
}

function saveCoc() {
  if (!selectedApplicant.value) return
  localStorage.setItem(`${COC_KEY_PREFIX}${selectedApplicant.value.applicantNo}`, JSON.stringify(cocForm.value))
  closeCoc()
}

function printCoc(applicant) {
  const coc = applicant === selectedApplicant.value ? cocForm.value : loadCoc(applicant.applicantNo)
  const dateIssued = coc.dateIssued || new Date().toISOString().slice(0, 10)
  const certNo = coc.number || ''
  const office = applicant.orientation?.office || applicant.office || ''
  const printWindow = window.open('', '', 'width=800,height=700')
  if (!printWindow) return
  printWindow.addEventListener('load', () => {
    printWindow.print()
    window.setTimeout(() => printWindow.close(), 500)
  }, { once: true })
  printWindow.document.write(`
    <html><head><title>Certificate of Completion - ${applicant.applicantNo}</title>
    <style>body{font-family:Arial,Helvetica,sans-serif;margin:30px}.header{text-align:center;margin-bottom:20px}.label{font-weight:bold}.value{margin-left:10px}.section{margin:12px 0}</style>
    </head><body>
    <div class="header"><h2>Certificate of Completion</h2><div>OJT No: <strong>${applicant.applicantNo}</strong></div><div>Certificate No: <strong>${certNo}</strong></div></div>
    <div class="section"><span class="label">Name:</span> <span class="value">${applicant.lastName || ''}, ${applicant.firstName || ''}</span></div>
    <div class="section"><span class="label">Office:</span> <span class="value">${office}</span></div>
    <div class="section"><span class="label">Required Hours:</span> <span class="value">${applicant.requiredHours || ''}</span></div>
    <div class="section"><span class="label">Rendered Hours:</span> <span class="value">${renderedHours(applicant)}</span></div>
    <div class="section"><span class="label">Date Issued:</span> <span class="value">${dateIssued}</span></div>
    <div class="section"><span class="label">Remarks:</span> <span class="value">${coc.remarks || ''}</span></div>
    <div style="margin-top:40px;text-align:center"><div>__________________________</div><div>Authorized Signature</div></div>
    </body></html>`)
  printWindow.document.close()
}

onMounted(() => {
  loadApplicants()
  window.addEventListener('storage', loadApplicants)
})

onBeforeUnmount(() => window.removeEventListener('storage', loadApplicants))
</script>

<style scoped>
.action-label {
  font-size: 0.7rem;
  font-weight: bold;
  text-transform: uppercase;
}
</style>
