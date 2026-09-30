<template>
  <section class="content">
    <!-- ================= API-backed module ================= -->
    <div v-if="config" class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">{{ config.title }}</h3>
        <button type="button" class="btn btn-primary btn-sm" :disabled="isLoading" @click="openCreate">
          <i class="fas fa-plus me-1"></i> Add {{ config.singular }}
        </button>
      </div>

      <div class="card-body">
        <div v-if="isLoading" class="text-center my-4">
          <div class="spinner-border text-primary" role="status">
            <span class="visually-hidden">Loading...</span>
          </div>
        </div>

        <div v-else-if="loadError" class="alert alert-danger d-flex justify-content-between align-items-center">
          <span><i class="fas fa-exclamation-triangle me-2"></i>{{ loadError }}</span>
          <button type="button" class="btn btn-sm btn-outline-danger" @click="load">Retry</button>
        </div>

        <div v-else class="table-responsive">
          <table class="table table-hover align-middle mb-0">
            <thead class="table-light">
              <tr>
                <th scope="col" class="text-center" style="width: 70px;">#</th>
                <th v-for="col in config.columns" :key="col.prop" scope="col">{{ col.label }}</th>
                <th scope="col" style="width: 170px;">Last Updated</th>
                <th scope="col" class="text-center" style="width: 120px;">Actions</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, index) in rows" :key="row.id">
                <td class="text-center">{{ index + 1 }}</td>
                <td v-for="col in config.columns" :key="col.prop">{{ row[col.prop] }}</td>
                <td class="text-muted small">{{ formatDate(row.updatedAt) }}</td>
                <td class="text-center text-nowrap">
                  <button type="button" class="btn btn-sm btn-outline-primary me-1" title="Edit"
                          :disabled="isLoading || isSaving" @click="openEdit(row)">
                    <i class="fas fa-pen"></i>
                  </button>
                  <button type="button" class="btn btn-sm btn-outline-danger" title="Delete"
                          :disabled="isLoading || isSaving" @click="confirmDelete(row)">
                    <i class="fas fa-trash"></i>
                  </button>
                </td>
              </tr>
              <tr v-if="rows.length === 0">
                <td :colspan="config.columns.length + 3" class="text-center text-muted py-4">
                  No {{ config.title.toLowerCase() }} yet.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <!-- ================= Not built yet ================= -->
    <div v-else class="card shadow-sm">
      <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
        <h3 class="card-title mb-0">{{ title }}</h3>
        <button type="button" class="btn btn-outline-secondary btn-sm" @click="goHome">
          <i class="fas fa-arrow-left me-1"></i> Back to Dashboard
        </button>
      </div>
      <div class="card-body">
        <div class="text-center py-5">
          <i class="fas fa-tools fa-3x text-muted mb-3"></i>
          <h5 class="mb-2">{{ title }}</h5>
          <p class="text-muted mb-0">This module is registered and reachable, but its screen has not been built yet.</p>
        </div>
      </div>
    </div>

    <!-- ================= Add / Edit modal ================= -->
    <div ref="formModalEl" class="modal fade" id="moduleFormModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered">
        <div class="modal-content">
          <form novalidate @submit.prevent="save">
            <div class="modal-header">
              <h5 class="modal-title">{{ isEditing ? `Edit ${config.singular}` : `Add ${config.singular}` }}</h5>
              <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeForm"></button>
            </div>
            <div class="modal-body">
              <div v-if="formError" class="alert alert-danger py-2">{{ formError }}</div>
              <div v-for="col in config.columns" :key="col.prop" class="mb-3">
                <label :for="`m-${col.prop}`" class="form-label fw-semibold">{{ col.label }}</label>
                <input :id="`m-${col.prop}`" v-model.trim="form[col.prop]" type="text" class="form-control"
                       :class="{ 'is-invalid': submitted && !form[col.prop] }" :maxlength="col.max"
                       :placeholder="`Input ${col.label}`" :disabled="isSaving" />
                <div class="invalid-feedback">{{ col.label }} is required.</div>
              </div>
            </div>
            <div class="modal-footer">
              <button type="submit" class="btn btn-primary" :disabled="isSaving">
                <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
                {{ isEditing ? 'Update' : 'Submit' }}
              </button>
              <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeForm">Close</button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- ================= Delete modal ================= -->
    <div ref="deleteModalEl" class="modal fade" id="moduleDeleteModal" tabindex="-1" aria-hidden="true">
      <div class="modal-dialog modal-dialog-centered modal-sm">
        <div class="modal-content">
          <div class="modal-header">
            <h5 class="modal-title">Delete</h5>
            <button type="button" class="btn-close" aria-label="Close" :disabled="isSaving" @click="closeDelete"></button>
          </div>
          <div class="modal-body">
            <p class="mb-0">Delete <strong class="text-danger">{{ pending?.id }}</strong>? This cannot be undone.</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-danger" :disabled="isSaving" @click="doDelete">
              <span v-if="isSaving" class="spinner-border spinner-border-sm me-1" aria-hidden="true"></span>
              Delete
            </button>
            <button type="button" class="btn btn-outline-secondary" :disabled="isSaving" @click="closeDelete">Cancel</button>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= Toast ================= -->
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div ref="toastEl" class="toast align-items-center text-bg-success border-0" role="alert" aria-live="assertive" aria-atomic="true">
        <div class="d-flex">
          <div class="toast-body">{{ message }}</div>
          <button type="button" class="btn-close btn-close-white me-2 m-auto" aria-label="Close" @click="toast.hide()"></button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, reactive, ref } from 'vue'
import { Modal } from 'bootstrap'
import { getApiErrorMessage, internsAPI, programsAPI, signatoriesAPI } from '../src/services/api'

/* page -> module config. Ang hindi nakalist dito ay "not built yet" pa. */
const MODULES = {
  'internlist.html': {
    title: 'Intern List', singular: 'Intern', api: internsAPI,
    columns: [{ prop: 'fullName', label: 'Full Name', max: 200 }, { prop: 'school', label: 'School', max: 200 }]
  },
  'programs.html': {
    title: 'Programs / Strands', singular: 'Program', api: programsAPI,
    columns: [{ prop: 'programCode', label: 'Program Code', max: 50 }, { prop: 'programName', label: 'Program Name', max: 200 }]
  },
  'signatories.html': {
    title: 'Signatories', singular: 'Signatory', api: signatoriesAPI,
    columns: [{ prop: 'name', label: 'Name', max: 200 }, { prop: 'position', label: 'Position', max: 150 }]
  },
  'forms.html': { title: 'Issuance of COC' },
  'dtr.html': { title: 'Daily Time Records' },
  'idgenerate.html': { title: 'ID Generation' },
  'utility.html': { title: 'Utility' }
}

const page = (window.location.pathname.split('/').pop() || '').toLowerCase()
const entry = MODULES[page] || {}

const config = computed(() => entry.api ? entry : null)
const title = computed(() => entry.title || 'Module')

const rows = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const formError = ref('')
const submitted = ref(false)
const pending = ref(null)
const message = ref('')

const form = reactive({ id: 0 })
const isEditing = computed(() => form.id > 0)

const formModalEl = ref(null)
const deleteModalEl = ref(null)
const toastEl = ref(null)
let formModal = null
let deleteModal = null
let toast = null
let toastTimer = null

function blankForm() {
  form.id = 0
  if (config.value) config.value.columns.forEach((c) => { form[c.prop] = '' })
  submitted.value = false
  formError.value = ''
}

function notify(text, variant = 'success') {
  message.value = text
  const el = toastEl.value
  el.classList.remove('text-bg-success', 'text-bg-danger')
  el.classList.add(`text-bg-${variant}`)
  toast.show()
  window.clearTimeout(toastTimer)
  toastTimer = window.setTimeout(() => toast.hide(), 4000)
}

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleString(undefined, { year: 'numeric', month: 'short', day: '2-digit', hour: '2-digit', minute: '2-digit' })
    : '-'
}

async function load() {
  if (!config.value) return
  isLoading.value = true
  loadError.value = ''
  try {
    rows.value = await config.value.api.getAll()
  } catch (e) {
    loadError.value = getApiErrorMessage(e, `Failed to load ${title.value}.`)
  } finally {
    isLoading.value = false
  }
}

function openCreate() { blankForm(); formModal.show() }
function openEdit(row) {
  blankForm()
  form.id = row.id
  config.value.columns.forEach((c) => { form[c.prop] = row[c.prop] ?? '' })
  formModal.show()
}
function closeForm() { if (!isSaving.value) formModal.hide() }

async function save() {
  submitted.value = true
  formError.value = ''
  const missing = config.value.columns.some((c) => !form[c.prop])
  if (missing) { formError.value = 'Please fill in all required fields.'; return }

  isSaving.value = true
  const api = config.value.api
  try {
    if (isEditing.value) { await api.update(form.id, { ...form }); notify('Record updated.') }
    else { await api.create({ ...form }); notify('Record added.') }
    formModal.hide()
    await load()
  } catch (e) {
    formError.value = getApiErrorMessage(e, 'Failed to save.')
  } finally {
    isSaving.value = false
  }
}

function confirmDelete(row) { pending.value = row; deleteModal.show() }
function closeDelete() { if (!isSaving.value) { deleteModal.hide(); pending.value = null } }

async function doDelete() {
  if (!pending.value) return
  isSaving.value = true
  try {
    await config.value.api.delete(pending.value.id)
    notify('Record deleted.')
    deleteModal.hide()
    pending.value = null
    await load()
  } catch (e) {
    notify(getApiErrorMessage(e, 'Failed to delete.'), 'danger')
  } finally {
    isSaving.value = false
  }
}

function goHome() {
  window.history.pushState({}, '', '/dashboard.html')
  window.dispatchEvent(new PopStateEvent('popstate'))
}

onMounted(() => {
  formModal = new Modal(formModalEl.value)
  deleteModal = new Modal(deleteModalEl.value)
  toast = new Modal(toastEl.value, { autohide: false })
  load()
})

onBeforeUnmount(() => {
  formModal?.dispose(); deleteModal?.dispose(); toast?.dispose()
  window.clearTimeout(toastTimer)
})
</script>
