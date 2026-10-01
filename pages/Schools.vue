<template>
  <section class="content">
    <div class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">List of Schools</h3>
        <button
          type="button"
          class="btn btn-primary btn-sm"
          :disabled="isLoading"
          @click="openCreateModal"
        >
          <i class="fas fa-plus me-1"></i> Add School
        </button>
      </div>

      <div class="card-body">
        <div v-if="isLoading" class="text-center my-4">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
          <p class="text-muted mt-2 mb-0">Loading schools...</p>
        </div>

        <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center">
          <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="loadSchools">Retry</button>
        </div>

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th scope="col" class="text-center" style="width: 80px;">#</th>
                <th scope="col">School Name</th>
                <th scope="col">Abbreviation</th>
                <th scope="col">Address</th>
                <th scope="col">MOA Status</th>
                <th scope="col">MOA Expiry</th>
                <th scope="col" style="width: 190px;">Last Updated</th>
                <th scope="col" class="text-center" style="width: 130px;">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(school, index) in schools" :key="school.id">
                <td class="text-center text-dark">{{ index + 1 }}</td>
                <td class="fw-semibold text-dark">{{ school.name }}</td>
                <td class="text-dark">{{ school.abbreviation || '-' }}</td>
                <td class="text-dark">{{ school.address }}</td>
                <td>
                  <span :class="school.moaStatus ? 'badge badge-success text-dark' : 'badge badge-secondary'">
                    {{ school.moaStatus ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="text-dark">{{ school.moaExpiry ? formatDateOnly(school.moaExpiry) : '-' }}</td>
                <td class="text-muted small">{{ formatDate(school.updatedAt) }}</td>
                <td class="text-center text-nowrap">
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-primary me-1"
                    title="Edit"
                    :disabled="isLoading || isSaving"
                    @click="openEditModal(school)"
                  >
                    <i class="fas fa-pen"></i>
                  </button>
                  <button
                    type="button"
                    class="btn btn-sm btn-outline-danger"
                    title="Delete"
                    :disabled="isLoading || isSaving"
                    @click="openDeleteModal(school)"
                  >
                    <i class="fas fa-trash"></i>
                  </button>
                </td>
              </tr>
              <tr v-if="schools.length === 0">
                <td colspan="8" class="text-center text-muted py-4">
                  No schools saved yet.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- Add / Edit modal -->
    <div
      ref="formModalEl"
      class="modal fade"
      id="schoolFormModal"
      tabindex="-1"
      aria-labelledby="schoolFormModalLabel"
      aria-hidden="true"
    >
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <form novalidate @submit.prevent="saveSchool">
            <div class="modal-header">
              <h5 id="schoolFormModalLabel" class="modal-title">
                {{ isEditing ? 'Edit School' : 'Add School' }}
              </h5>
              <button
                type="button"
                class="btn-close"
                aria-label="Close"
                :disabled="isSaving"
                @click="closeFormModal"
              ></button>
            </div>

            <div class="modal-body">
              <div v-if="formError" class="alert alert-danger py-2">
                {{ formError }}
              </div>

              <div class="mb-3">
                <label for="schoolName" class="form-label fw-semibold">School Name</label>
                <input
                  id="schoolName"
                  v-model.trim="form.name"
                  type="text"
                  class="form-control"
                  :class="{ 'is-invalid': submitted && !form.name }"
                  maxlength="200"
                  placeholder="Input School Name"
                  :disabled="isSaving"
                />
                <div class="invalid-feedback">School name is required.</div>
              </div>

              <div class="mb-3">
                <label for="schoolAbbreviation" class="form-label fw-semibold">Abbreviation</label>
                <input
                  id="schoolAbbreviation"
                  v-model.trim="form.abbreviation"
                  type="text"
                  class="form-control"
                  maxlength="20"
                  placeholder="Input Abbreviation (e.g., DLSU, ADMU)"
                  :disabled="isSaving"
                />
              </div>

              <div class="mb-3">
                <label for="schoolAddress" class="form-label fw-semibold">Address</label>
                <textarea
                  id="schoolAddress"
                  v-model.trim="form.address"
                  class="form-control"
                  :class="{ 'is-invalid': submitted && !form.address }"
                  rows="3"
                  maxlength="500"
                  placeholder="Input Address"
                  :disabled="isSaving"
                ></textarea>
                <div class="invalid-feedback">Address is required.</div>
              </div>

              <div class="mb-3">
                <div class="form-check form-switch">
                  <input
                    id="schoolMoaStatus"
                    v-model="form.moaStatus"
                    type="checkbox"
                    class="form-check-input"
                    :disabled="isSaving"
                  />
                  <label class="form-check-label fw-semibold" for="schoolMoaStatus">
                    MOA Status (Active/Inactive)
                  </label>
                </div>
              </div>

              <div class="mb-0">
                <label for="schoolMoaExpiry" class="form-label fw-semibold">MOA Expiry Date</label>
                <DatePicker
                  id="schoolMoaExpiry"
                  v-model="form.moaExpiry"
                  :disabled="isSaving"
                />
              </div>
            </div>

            <div class="modal-footer">
              <button
                type="submit"
                class="btn btn-primary"
                :disabled="isSaving"
              >
                <span
                  v-if="isSaving"
                  class="spinner-border spinner-border-sm me-1"
                  aria-hidden="true"
                ></span>
                {{ isEditing ? 'Update' : 'Submit' }}
              </button>
              <button
                type="button"
                class="btn btn-outline-secondary"
                :disabled="isSaving"
                @click="closeFormModal"
              >
                Close
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Delete confirmation modal -->
    <div
      ref="deleteModalEl"
      class="modal fade"
      id="schoolDeleteModal"
      tabindex="-1"
      aria-labelledby="schoolDeleteModalLabel"
      aria-hidden="true"
    >
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header">
            <h5 id="schoolDeleteModalLabel" class="modal-title">Delete School</h5>
            <button
              type="button"
              class="btn-close"
              aria-label="Close"
              :disabled="isSaving"
              @click="closeDeleteModal"
            ></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">
              Are you sure you want to delete
              <strong class="text-danger">{{ pendingDelete?.name }}</strong>?
            </p>
          </div>
          <div class="modal-footer">
            <button
              type="button"
              class="btn btn-danger"
              :disabled="isSaving"
              @click="confirmDelete"
            >
              <span
                v-if="isSaving"
                class="spinner-border spinner-border-sm me-1"
                aria-hidden="true"
              ></span>
              Delete
            </button>
            <button
              type="button"
              class="btn btn-outline-secondary"
              :disabled="isSaving"
              @click="closeDeleteModal"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Notification toast -->
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div
        id="schoolToast"
        class="toast align-items-center text-bg-success border-0"
        role="alert"
        aria-live="assertive"
        aria-atomic="true"
      >
        <div class="d-flex">
          <div class="toast-body">{{ notification.message }}</div>
          <button
            type="button"
            class="btn-close btn-close-white me-2 m-auto"
            aria-label="Close"
            @click="hideToast"
          ></button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { Modal } from 'bootstrap'
import { getApiErrorMessage, schoolsAPI } from '@/services/api'
import DatePicker from '@/components/DatePicker.vue'

const schools = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const formError = ref('')
const submitted = ref(false)
const notification = reactive({ message: '', variant: 'success' })
const notificationTimeout = ref(null)

const form = reactive({ id: 0, name: '', abbreviation: '', address: '', moaStatus: false, moaExpiry: '' })
const isEditing = computed(() => form.id > 0)

const formModalEl = ref(null)
const deleteModalEl = ref(null)
const toastEl = ref(null)

let formModal = null
let deleteModal = null
let toast = null

const pendingDelete = ref(null)

const emptyForm = () => {
  form.id = 0
  form.name = ''
  form.abbreviation = ''
  form.address = ''
  form.moaStatus = false
  form.moaExpiry = ''
  submitted.value = false
  formError.value = ''
}

function showToast(message, variant = 'success') {
  notification.message = message
  notification.variant = variant

  // Re-apply the contextual variant class before showing the toast again.
  const element = toastEl.value
  element.classList.remove('text-bg-success', 'text-bg-danger', 'text-bg-warning')
  element.classList.add(`text-bg-${variant}`)

  toast.show()

  window.clearTimeout(notificationTimeout.value)
  notificationTimeout.value = window.setTimeout(() => toast.hide(), 4000)
}

function hideToast() {
  toast.hide()
}

function formatDate(value) {
  if (!value) return '-'
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return '-'
  return parsed.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

function formatDateOnly(value) {
  if (!value) return '-'
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return '-'
  return parsed.toLocaleString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  })
}

function removeModalBackdrops() {
  if (typeof document === 'undefined') return
  document.querySelectorAll?.('.modal-backdrop')?.forEach?.(el => el.remove?.())
  document.body?.classList?.remove('modal-open')
  document.body.style.overflow = ''
  document.body.style.paddingRight = ''
}

async function loadSchools() {
  isLoading.value = true
  loadError.value = ''

  try {
    schools.value = await schoolsAPI.getAll()
  } catch (error) {
    loadError.value = getApiErrorMessage(error, 'Failed to load schools.')
  } finally {
    isLoading.value = false
  }
}

function openCreateModal() {
  emptyForm()
  formModal.show()
}

function openEditModal(school) {
  emptyForm()
  form.id = school.id
  form.name = school.name
  form.abbreviation = school.abbreviation || ''
  form.address = school.address
  form.moaStatus = school.moaStatus || false
  form.moaExpiry = school.moaExpiry ? school.moaExpiry.slice(0, 10) : ''
  formModal.show()
}

function closeFormModal() {
  if (isSaving.value) return
  formModal.hide()
  removeModalBackdrops()
}

async function saveSchool() {
  submitted.value = true
  formError.value = ''

  if (!form.name || !form.address) {
    formError.value = 'Please fill in all required fields.'
    return
  }

  isSaving.value = true

  const payload = {
    id: form.id,
    name: form.name,
    abbreviation: form.abbreviation,
    address: form.address,
    moaStatus: form.moaStatus,
    moaExpiry: form.moaExpiry || null
  }

  try {
    if (isEditing.value) {
      const updated = await schoolsAPI.update(form.id, payload)
      const idx = schools.value.findIndex(s => s.id === form.id)
      if (idx !== -1) schools.value[idx] = updated
      showToast('School updated successfully.')
    } else {
      const created = await schoolsAPI.create(payload)
      schools.value.unshift(created)
      showToast('School added successfully.')
    }

    formModal.hide()
    removeModalBackdrops()
  } catch (error) {
    formError.value = getApiErrorMessage(error, 'Failed to save the school.')
    formModal.hide()
    removeModalBackdrops()
  } finally {
    isSaving.value = false
  }
}

function openDeleteModal(school) {
  pendingDelete.value = school
  deleteModal.show()
}

function closeDeleteModal() {
  if (isSaving.value) return
  deleteModal.hide()
  removeModalBackdrops()
  pendingDelete.value = null
}

async function confirmDelete() {
  if (!pendingDelete.value) return

  isSaving.value = true

  try {
    await schoolsAPI.delete(pendingDelete.value.id)
    schools.value = schools.value.filter(s => s.id !== pendingDelete.value.id)
    showToast('School deleted successfully.')
    deleteModal.hide()
    removeModalBackdrops()
    pendingDelete.value = null
  } catch (error) {
    showToast(getApiErrorMessage(error, 'Failed to delete the school.'), 'danger')
  } finally {
    isSaving.value = false
  }
}

onMounted(() => {
  formModal = new Modal(formModalEl.value)
  deleteModal = new Modal(deleteModalEl.value)
  toast = new Modal(document.getElementById('schoolToast'), { autohide: false })
  toastEl.value = document.getElementById('schoolToast')

  loadSchools()
})

onBeforeUnmount(() => {
  formModal?.dispose()
  deleteModal?.dispose()
  toast?.dispose()
  window.clearTimeout(notificationTimeout.value)
})
</script>
