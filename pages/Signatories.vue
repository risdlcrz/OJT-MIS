<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title mb-0">List of Signatories</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="openCreate">
            <i class="fas fa-plus"></i> Add Signatory
          </button>
        </div>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered">
            <thead>
              <tr class="text-center">
                <th>NAME</th>
                <th>OFFICE</th>
                <th>POSITION</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="signatory in signatories" :key="signatory.id" class="text-center">
                <td>{{ signatory.name }}</td>
                <td>{{ signatory.office }}</td>
                <td>{{ signatory.position }}</td>
                <td>
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action mr-1" @click="openEdit(signatory)">
                    <i class="fas fa-edit"></i><span class="action-label">Edit</span>
                  </button>
                  <button type="button" class="btn btn-outline-danger btn-sm modern-action" @click="removeSignatory(signatory)">
                    <i class="fa fa-times"></i><span class="action-label">Remove</span>
                  </button>
                </td>
              </tr>
              <tr v-if="signatories.length === 0">
                <td colspan="4" class="text-center text-muted py-4">No signatories found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
    <div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeModal">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header bg-orange py-2">
            <h5 class="modal-title text-white">{{ editingSignatory ? 'Edit Employee' : 'Add Employee' }}</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeModal">&times;</button>
          </div>
          <form @submit.prevent="saveSignatory">
            <div class="modal-body p-4">
              <div class="form-group row mb-3">
                <label class="col-sm-4 col-form-label font-weight-bold">Employee's Name</label>
                <div class="col-sm-8">
                  <input v-model="form.name" type="text" class="form-control" placeholder="Input Employee's Name" required>
                </div>
              </div>
              <div class="form-group row mb-3">
                <label class="col-sm-4 col-form-label font-weight-bold">Office</label>
                <div class="col-sm-8">
                  <select v-model="form.office" class="form-control signatory-office-select" required>
                    <option value="">Select an office</option>
                    <option v-for="office in offices" :key="office.code" :value="office.name">{{ office.name }}</option>
                  </select>
                </div>
              </div>
              <div class="form-group row mb-3">
                <label class="col-sm-4 col-form-label font-weight-bold">Position</label>
                <div class="col-sm-8">
                  <input v-model="form.position" type="text" class="form-control" placeholder="Input Position" required>
                </div>
              </div>
            </div>
            <div class="modal-footer border-0 justify-content-end">
              <button type="submit" class="btn btn-primary modern-action px-4"><i class="fas fa-plus"></i> Submit</button>
              <button type="button" class="btn btn-outline-secondary modern-action px-4" @click="closeModal"><i class="fas fa-times"></i> Close</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'

const SIGNATORIES_KEY = 'ojt_signatories_v1'
const defaultSignatories = [{
  id: 1,
  name: 'Marwin De Guzman',
  office: 'AS - INFORMATION TECHNOLOGY OFFICE',
  position: 'OJT Supervisor'
}]
const signatories = ref([])
const offices = ref([])
const isModalOpen = ref(false)
const editingSignatory = ref(null)
const form = ref({ name: '', office: '', position: '' })

function loadSignatories() {
  try {
    const raw = localStorage.getItem(SIGNATORIES_KEY)
    signatories.value = raw ? JSON.parse(raw) : defaultSignatories.map((signatory) => ({ ...signatory }))
  } catch (error) {
    console.error('Failed to load signatories:', error)
    signatories.value = defaultSignatories.map((signatory) => ({ ...signatory }))
  }
}

function persistSignatories() {
  localStorage.setItem(SIGNATORIES_KEY, JSON.stringify(signatories.value))
}

function openCreate() {
  editingSignatory.value = null
  form.value = { name: '', office: '', position: '' }
  isModalOpen.value = true
}

function openEdit(signatory) {
  editingSignatory.value = signatory.id
  const selectedOffice = offices.value.find((office) => office.code === signatory.office || office.name === signatory.office)
  form.value = {
    name: signatory.name,
    office: selectedOffice?.name || signatory.office,
    position: signatory.position
  }
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
}

function saveSignatory() {
  if (editingSignatory.value === null) {
    signatories.value.push({ id: Date.now(), ...form.value })
  } else {
    const index = signatories.value.findIndex((signatory) => signatory.id === editingSignatory.value)
    if (index !== -1) signatories.value[index] = { id: editingSignatory.value, ...form.value }
  }
  persistSignatories()
  closeModal()
}

function removeSignatory(signatory) {
  if (!window.confirm(`Are you sure you want to remove "${signatory.name}"?`)) return
  signatories.value = signatories.value.filter((item) => item.id !== signatory.id)
  persistSignatories()
}

async function loadOffices() {
  try {
    const response = await fetch('/assets/json/offices.json')
    if (!response.ok) throw new Error(`Failed to load offices.json: ${response.status}`)
    const data = await response.json()
    offices.value = data.offices || []
  } catch (error) {
    console.error('Failed to load offices:', error)
    offices.value = []
  }
}

onMounted(() => {
  loadSignatories()
  loadOffices()
})
</script>

<style scoped>
.signatory-office-select {
  min-height: 50px;
  height: 50px;
  padding-top: 0.7rem;
  padding-bottom: 0.7rem;
  padding-left: 0.75rem;
  text-indent: 0;
}

.action-label {
  display: block;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
}
</style>
