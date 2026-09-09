<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title mb-0">List of Programs / Strand</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="openCreate">
            <i class="fas fa-plus"></i> Add Program
          </button>
        </div>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered">
            <thead>
              <tr class="text-center">
                <th>PROGRAM / STRAND</th>
                <th>ABBREVIATION</th>
                <th>EDUCATIONAL LEVEL</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="program in programs" :key="program.id" class="text-center">
                <td>{{ program.name }}</td>
                <td>{{ program.abbrev }}</td>
                <td>{{ program.level }}</td>
                <td>
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action mr-1" @click="openEdit(program)">
                    <i class="fas fa-edit"></i><span class="action-label">Edit</span>
                  </button>
                  <button type="button" class="btn btn-outline-danger btn-sm modern-action" @click="removeProgram(program)">
                    <i class="fas fa-times"></i><span class="action-label">Remove</span>
                  </button>
                </td>
              </tr>
              <tr v-if="programs.length === 0">
                <td colspan="4" class="text-center text-muted py-4">No programs found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
    <div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeModal">
      <div class="modal-dialog modal-dialog-centered modal-md">
        <div class="modal-content">
          <div class="modal-header bg-orange py-2">
            <h5 class="modal-title text-white">{{ editingProgram ? 'Edit Program' : 'Add Program' }}</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeModal">&times;</button>
          </div>
          <form @submit.prevent="saveProgram">
            <div class="modal-body p-4">
              <div class="form-group">
                <label class="font-weight-bold">Program / Strand</label>
                <input v-model="form.name" class="form-control" required>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Abbreviation</label>
                <input v-model="form.abbrev" class="form-control" required>
              </div>
              <div class="form-group">
                <label class="font-weight-bold">Educational Level</label>
                <select v-model="form.level" class="form-control educational-level-select" required>
                  <option>Senior High School</option>
                  <option>College</option>
                  <option>Junior High School</option>
                  <option>Technical / Vocational</option>
                </select>
              </div>
            </div>
            <div class="modal-footer border-0">
              <button type="button" class="btn btn-outline-secondary modern-action" @click="closeModal"><i class="fas fa-times"></i> Close</button>
              <button type="submit" class="btn btn-primary modern-action"><i class="fas fa-save"></i> Save</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'

const PROGRAMS_KEY = 'ojt_programs_v1'
const defaultPrograms = [
  { id: 1, name: 'Science, Technology, Engineering and Mathematics', abbrev: 'STEM', level: 'Senior High School' },
  { id: 2, name: 'Humanities and Social Sciences', abbrev: 'HUMSS', level: 'Senior High School' },
  { id: 3, name: 'General Academic Strand', abbrev: 'GAS', level: 'Senior High School' },
  { id: 4, name: 'Technical-Vocational-Livelihood', abbrev: 'TVL', level: 'Senior High School' }
]
const programs = ref([])
const isModalOpen = ref(false)
const editingProgram = ref(null)
const form = ref({ name: '', abbrev: '', level: 'Senior High School' })

function loadPrograms() {
  try {
    const raw = localStorage.getItem(PROGRAMS_KEY)
    programs.value = raw ? JSON.parse(raw) : defaultPrograms.map((program) => ({ ...program }))
  } catch (error) {
    console.error('Failed to load programs:', error)
    programs.value = defaultPrograms.map((program) => ({ ...program }))
  }
}

function persistPrograms() {
  localStorage.setItem(PROGRAMS_KEY, JSON.stringify(programs.value))
}

function openCreate() {
  editingProgram.value = null
  form.value = { name: '', abbrev: '', level: 'Senior High School' }
  isModalOpen.value = true
}

function openEdit(program) {
  editingProgram.value = program.id
  form.value = { name: program.name, abbrev: program.abbrev, level: program.level }
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
}

function saveProgram() {
  if (editingProgram.value === null) {
    programs.value.push({ id: Date.now(), ...form.value })
  } else {
    const index = programs.value.findIndex((program) => program.id === editingProgram.value)
    if (index !== -1) programs.value[index] = { id: editingProgram.value, ...form.value }
  }
  persistPrograms()
  closeModal()
}

function removeProgram(program) {
  if (!window.confirm(`Are you sure you want to remove "${program.name}"?`)) return
  programs.value = programs.value.filter((item) => item.id !== program.id)
  persistPrograms()
}

onMounted(loadPrograms)
</script>

<style scoped>
.educational-level-select {
  min-height: 50px;
  height: 50px;
  padding-top: 0.7rem;
  padding-bottom: 0.7rem;
  padding-left: 0.75rem;
  text-indent: 0;
}
</style>

<style scoped>
.action-label {
  display: block;
  font-size: 0.72rem;
  font-weight: 700;
  text-transform: uppercase;
}
</style>
