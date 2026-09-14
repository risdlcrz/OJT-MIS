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
        <!-- Error Alert -->
        <div v-if="errorMessage" class="alert alert-danger alert-dismissible fade show" role="alert">
          <strong><i class="fas fa-exclamation-triangle"></i> {{ errorMessage }}</strong>
          <button type="button" class="close" aria-label="Close" @click="errorMessage = ''">
            <span aria-hidden="true">&times;</span>
          </button>
          <div class="mt-2">
            <button type="button" class="btn btn-sm btn-danger" @click="loadPrograms">
              <i class="fas fa-sync-alt"></i> Retry
            </button>
          </div>
        </div>

        <!-- Loading Spinner -->
        <div v-if="isLoading" class="text-center py-4">
          <div class="spinner-border" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>

        <!-- Table (only show when not loading) -->
        <div v-if="!isLoading" class="table-responsive">
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
import { programsAPI } from '../src/services/api'

const programs = ref([])
const isModalOpen = ref(false)
const isLoading = ref(false)
const errorMessage = ref('')
const editingProgram = ref(null)
const form = ref({ name: '', abbrev: '', level: 'Senior High School' })

// Fetch programs from backend API
async function loadPrograms() {
  isLoading.value = true
  errorMessage.value = ''
  try {
    const response = await programsAPI.getAll()
    // Handle both array response and paginated response
    programs.value = Array.isArray(response) ? response : (response.data || [])
    console.log('Programs loaded:', programs.value)
  } catch (error) {
    console.error('Failed to load programs:', error)
    if (error.code === 'ECONNABORTED') {
      errorMessage.value = 'The request took too long to complete. Please try again.'
    } else if (error.response?.status === 404) {
      errorMessage.value = 'The programs endpoint could not be found. Please check the API configuration.'
    } else if (error.response?.status >= 500) {
      errorMessage.value = 'The server encountered an error. Please try again later.'
    } else {
      errorMessage.value = `Failed to load programs: ${error.message}`
    }
  } finally {
    isLoading.value = false
  }
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
  form.value = { name: '', abbrev: '', level: 'Senior High School' }
}

async function saveProgram() {
  try {
    if (!form.value.name.trim() || !form.value.abbrev.trim()) {
      alert('Please fill in all fields')
      return
    }

    if (editingProgram.value === null) {
      // Create new program
      await programsAPI.create(form.value)
    } else {
      // Update existing program
      await programsAPI.update(editingProgram.value, form.value)
    }

    await loadPrograms()
    closeModal()
  } catch (error) {
    console.error('Failed to save program:', error)
    alert(`Failed to save program: ${error.message}`)
  }
}

async function removeProgram(program) {
  if (!window.confirm(`Are you sure you want to remove "${program.name}"?`)) return

  try {
    await programsAPI.delete(program.id)
    await loadPrograms()
  } catch (error) {
    console.error('Failed to delete program:', error)
    alert(`Failed to delete program: ${error.message}`)
  }
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
