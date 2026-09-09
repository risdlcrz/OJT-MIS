<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title mb-0">List of Schools</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="openCreate">
            <i class="fas fa-plus"></i> Add a School
          </button>
        </div>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered schools-table">
            <thead>
              <tr class="text-center">
                <th>SCHOOL</th>
                <th>ABBREVIATION</th>
                <th class="school-address-column">ADDRESS</th>
                <th class="school-action-column">ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="school in schools" :key="school.id" class="text-center">
                <td>{{ school.name }}</td>
                <td>{{ school.abbreviation }}</td>
                <td class="school-address-column">{{ school.address }}</td>
                <td class="school-action-column action-cell">
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action mr-1" @click="openEdit(school)">
                    <i class="fas fa-edit"></i><span class="action-label">Edit</span>
                  </button>
                  <button type="button" class="btn btn-outline-danger btn-sm modern-action" @click="removeSchool(school)">
                    <i class="fa fa-times"></i><span class="action-label">Remove</span>
                  </button>
                </td>
              </tr>
              <tr v-if="schools.length === 0">
                <td colspan="4" class="text-center text-muted py-4">No schools found.</td>
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
            <h5 class="modal-title text-white">{{ editingSchool ? 'Edit School' : 'Add School' }}</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeModal">&times;</button>
          </div>
          <form @submit.prevent="saveSchool">
            <div class="modal-body p-4">
              <div class="form-group row">
                <label class="col-sm-4 col-form-label font-weight-bold">School</label>
                <div class="col-sm-8">
                  <input v-model="form.name" type="text" class="form-control" placeholder="FEU Institute of Technology" required>
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-4 col-form-label font-weight-bold">Abbreviation</label>
                <div class="col-sm-8">
                  <input v-model="form.abbreviation" type="text" class="form-control" placeholder="FEUTECH" required>
                </div>
              </div>
              <div class="form-group row">
                <label class="col-sm-4 col-form-label font-weight-bold">Address</label>
                <div class="col-sm-8">
                  <input v-model="form.address" type="text" class="form-control" placeholder="School address" required>
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

const SCHOOLS_KEY = 'ojt_schools_v1'
const defaultSchools = [{
  id: 1,
  name: 'AMA Computer Learning Center College of Commonwealth',
  abbreviation: 'ACLC',
  address: 'Block 11 Lot 31, Lirio 1 Building, Commonwealth Ave, Quezon City, 1126 Metro Manila'
}]
const schools = ref([])
const isModalOpen = ref(false)
const editingSchool = ref(null)
const form = ref({ name: '', abbreviation: '', address: '' })

function loadSchools() {
  try {
    const raw = localStorage.getItem(SCHOOLS_KEY)
    schools.value = raw ? JSON.parse(raw) : defaultSchools.map((school) => ({ ...school }))
  } catch (error) {
    console.error('Failed to load schools:', error)
    schools.value = defaultSchools.map((school) => ({ ...school }))
  }
}

function persistSchools() {
  localStorage.setItem(SCHOOLS_KEY, JSON.stringify(schools.value))
}

function openCreate() {
  editingSchool.value = null
  form.value = { name: '', abbreviation: '', address: '' }
  isModalOpen.value = true
}

function openEdit(school) {
  editingSchool.value = school.id
  form.value = { name: school.name, abbreviation: school.abbreviation, address: school.address }
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
}

function saveSchool() {
  if (editingSchool.value === null) {
    schools.value.push({ id: Date.now(), ...form.value })
  } else {
    const index = schools.value.findIndex((school) => school.id === editingSchool.value)
    if (index !== -1) schools.value[index] = { id: editingSchool.value, ...form.value }
  }
  persistSchools()
  closeModal()
}

function removeSchool(school) {
  if (!window.confirm(`Are you sure you want to remove "${school.name}"?`)) return
  schools.value = schools.value.filter((item) => item.id !== school.id)
  persistSchools()
}

onMounted(loadSchools)
</script>

<style scoped>
.action-label {
  display: inline;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
}

.schools-table {
  min-width: 720px;
}

.school-address-column {
  min-width: 280px;
}

.school-action-column {
  min-width: 170px;
  white-space: nowrap;
}

.action-cell {
  vertical-align: middle;
}

.action-cell .modern-action {
  display: inline-flex;
  align-items: center;
  gap: 0.3rem;
  white-space: nowrap;
}
</style>
