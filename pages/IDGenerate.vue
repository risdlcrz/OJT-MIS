<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">Generate OJT ID</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="openCreate">
            <i class="fas fa-plus"></i> Add OJT
          </button>
        </div>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered modern-table">
            <thead>
              <tr class="text-center">
                <th>OJT NO.</th>
                <th>NAME</th>
                <th>ASSIGNMENT</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="record in records" :key="record.ojtNo">
                <td>{{ record.ojtNo }}</td>
                <td>{{ record.name }}</td>
                <td>{{ record.assignedOffice }}</td>
                <td>
                  <button type="button" class="btn btn-outline-success btn-sm modern-action mr-1" @click="openEdit(record)">
                    <i class="fas fa-edit"></i> Edit
                  </button>
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action" @click="printRecord(record)">
                    <i class="fas fa-print"></i> Print
                  </button>
                </td>
              </tr>
              <tr v-if="records.length === 0">
                <td colspan="4" class="text-center text-muted py-4">No OJT IDs generated yet.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
    <div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeModal">
      <div class="modal-dialog modal-xl modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header bg-orange py-2">
            <h5 class="modal-title text-white">{{ editingRecord ? 'Edit OJT ID' : 'Generate OJT ID' }}</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeModal">&times;</button>
          </div>
          <form @submit.prevent="saveRecord">
            <div class="modal-body p-4 id-generate-modal-body">
              <div class="form-group">
                <label class="font-weight-bold">Name</label>
                <input v-model="form.name" list="applicantsList" class="form-control" placeholder="Full Name" required @input="autofillApplicant">
                <datalist id="applicantsList">
                  <option v-for="applicant in applicants" :key="applicant.applicantNo" :value="`${applicant.applicantNo} - ${applicant.lastName}, ${applicant.firstName}`" />
                </datalist>
              </div>
              <div class="form-row">
                <div class="form-group col-md-4">
                  <label class="font-weight-bold">Date Started</label>
                  <input v-model="form.dateStarted" type="date" class="form-control" required>
                </div>
                <div class="form-group col-md-4">
                  <label class="font-weight-bold">Valid Until</label>
                  <input v-model="form.validUntil" type="date" class="form-control" required>
                </div>
                <div class="form-group col-md-4">
                  <label class="font-weight-bold">Date Issued</label>
                  <input v-model="form.dateIssued" type="date" class="form-control" required>
                </div>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Assigned Office</label>
                <select v-model="form.assignedOffice" class="form-control assigned-office-select" required>
                  <option value="">-- Select Office --</option>
                  <option v-for="office in offices" :key="office.code" :value="office.name">{{ office.name }}</option>
                </select>
              </div>
              <small class="text-muted text-uppercase font-weight-bold">School Information</small>
              <hr class="mt-1 mb-3">
              <div class="form-group">
                <label class="font-weight-bold">School Name</label>
                <input v-model="form.schoolName" class="form-control" placeholder="School Name" required>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Program / Strand</label>
                <input v-model="form.schoolProgram" class="form-control" placeholder="Program or Strand" required>
              </div>
              <small class="text-muted text-uppercase font-weight-bold">In Case of Emergency, Please Notify</small>
              <hr class="mt-1 mb-3">
              <div class="form-group">
                <label class="font-weight-bold">Name</label>
                <input v-model="form.emergencyName" class="form-control" placeholder="Contact Name" required>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Address</label>
                <input v-model="form.emergencyAddress" class="form-control" placeholder="Address" required>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Contact No.</label>
                <input v-model="form.emergencyContact" class="form-control" placeholder="Contact Number" required>
              </div>
            </div>
            <div class="modal-footer border-0">
              <button type="button" class="btn btn-outline-secondary modern-action" @click="closeModal">Cancel</button>
              <button type="submit" class="btn btn-success modern-action"><i class="fas fa-save mr-1"></i> {{ editingRecord ? 'Save' : 'Generate' }}</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import '../assets/css/Forms.css'

const ID_RECORDS_KEY = 'ojt_id_records_v1'
const APPLICANTS_KEY = 'ojt_applicants_v1'
const emptyForm = () => ({
  name: '', dateStarted: '', validUntil: '', dateIssued: '', assignedOffice: '',
  schoolName: '', schoolProgram: '', emergencyName: '', emergencyAddress: '', emergencyContact: ''
})
const records = ref([])
const applicants = ref([])
const offices = ref([])
const isModalOpen = ref(false)
const editingRecord = ref(null)
const form = ref(emptyForm())

function readJson(key, fallback) {
  try {
    const raw = localStorage.getItem(key)
    return raw ? JSON.parse(raw) : fallback
  } catch (error) {
    console.error(`Failed to read ${key}:`, error)
    return fallback
  }
}

function loadRecords() {
  const saved = readJson(ID_RECORDS_KEY, {})
  records.value = Object.entries(saved).map(([ojtNo, data]) => ({ ...data, ojtNo }))
}

function saveRecords() {
  const data = Object.fromEntries(records.value.map((record) => [record.ojtNo, { ...record }]))
  records.value.forEach((record) => delete record.ojtNo)
  records.value.forEach((record, index) => { record.ojtNo = Object.keys(data)[index] })
  localStorage.setItem(ID_RECORDS_KEY, JSON.stringify(data))
}

function openCreate() {
  editingRecord.value = null
  form.value = emptyForm()
  isModalOpen.value = true
}

function openEdit(record) {
  editingRecord.value = record.ojtNo
  form.value = { ...record }
  delete form.value.ojtNo
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
}

function nextOjtNo() {
  return `2024-${String(records.value.length + 1).padStart(3, '0')}`
}

function autofillApplicant() {
  const applicantNo = form.value.name.split(' - ')[0]?.trim()
  const applicant = applicants.value.find((item) => item.applicantNo === applicantNo)
  if (!applicant) return
  form.value.name = `${applicant.lastName || ''}, ${applicant.firstName || ''}`.trim()
  form.value.dateStarted = applicant.dateStarted || form.value.dateStarted
  form.value.validUntil = applicant.validUntil || form.value.validUntil
  form.value.schoolName = applicant.schoolName || form.value.schoolName
  form.value.schoolProgram = applicant.program || form.value.schoolProgram
  form.value.assignedOffice = applicant.orientation?.office || form.value.assignedOffice
}

function saveRecord() {
  const ojtNo = editingRecord.value || nextOjtNo()
  const record = { ...form.value, ojtNo }
  const index = records.value.findIndex((item) => item.ojtNo === ojtNo)
  if (index === -1) records.value.push(record)
  else records.value[index] = record
  saveRecords()
  closeModal()
}

function printRecord(record) {
  const printWindow = window.open('', '', 'height=600,width=800')
  if (!printWindow) return
  printWindow.document.write(`<html><head><title>OJT ID - ${record.ojtNo}</title><style>body{font-family:Arial,sans-serif;margin:20px}h2{text-align:center}.section{margin:15px 0;border-bottom:1px solid #ccc;padding-bottom:10px}.label{font-weight:bold}.value{margin-left:20px}table{width:100%;margin-top:10px}td{padding:5px}</style></head><body><h2>OJT ID Generation</h2><div class="section"><div class="label">OJT No.:</div><div class="value">${record.ojtNo}</div></div><div class="section"><div class="label">Name:</div><div class="value">${record.name}</div></div><div class="section"><table><tr><td><div class="label">Date Started:</div><div class="value">${record.dateStarted}</div></td><td><div class="label">Valid Until:</div><div class="value">${record.validUntil}</div></td><td><div class="label">Date Issued:</div><div class="value">${record.dateIssued}</div></td></tr></table></div><div class="section"><div class="label">Assigned Office:</div><div class="value">${record.assignedOffice}</div></div><div class="section"><h3>School Information</h3><div class="label">School Name:</div><div class="value">${record.schoolName}</div><div class="label">Program / Strand:</div><div class="value">${record.schoolProgram}</div></div><div class="section"><h3>In Case of Emergency, Please Notify</h3><div class="label">Name:</div><div class="value">${record.emergencyName}</div><div class="label">Address:</div><div class="value">${record.emergencyAddress}</div><div class="label">Contact No.:</div><div class="value">${record.emergencyContact}</div></div></body></html>`)
  printWindow.document.close()
  printWindow.addEventListener('load', () => printWindow.print(), { once: true })
}

onMounted(async () => {
  applicants.value = readJson(APPLICANTS_KEY, [])
  loadRecords()
  try {
    const response = await fetch('/assets/json/offices.json')
    if (!response.ok) throw new Error(`Failed to load offices: ${response.status}`)
    offices.value = (await response.json()).offices || []
  } catch (error) {
    console.error('Failed to load offices:', error)
  }
})
</script>

<style scoped>
.id-generate-modal-body {
  max-height: min(70vh, 620px);
  overflow-y: auto;
  scrollbar-width: thin;
  scrollbar-color: #0b5d57 #e2e8f0;
}

.id-generate-modal-body::-webkit-scrollbar {
  width: 8px;
}

.id-generate-modal-body::-webkit-scrollbar-track {
  background: #e2e8f0;
  border-radius: 999px;
}

.id-generate-modal-body::-webkit-scrollbar-thumb {
  background: #0b5d57;
  border-radius: 999px;
}

.assigned-office-select {
  min-height: 50px;
  height: 50px;
  padding-left: 0.75rem;
  text-indent: 0;
}
</style>
