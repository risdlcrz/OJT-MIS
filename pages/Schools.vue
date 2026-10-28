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
      <div class="modal-dialog modal-dialog-centered modal-lg" style="overflow: visible;">
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
                <label class="form-label fw-semibold">Program Type</label>
                <div class="btn-group w-100" role="group" aria-label="Program type">
                  <input id="progCollege" v-model="programType" type="radio" class="btn-check" value="college" :disabled="isSaving" />
                  <label class="btn btn-outline-primary" for="progCollege">College</label>
                  <input id="progShs" v-model="programType" type="radio" class="btn-check" value="shs" :disabled="isSaving" />
                  <label class="btn btn-outline-primary" for="progShs">Senior High</label>
                </div>
              </div>

              <div v-if="programType === 'college'" class="mb-3">
                <label class="form-label fw-semibold">College Courses (Programs)</label>
                <div class="input-group input-group-sm mb-2">
                  <span class="input-group-text"><i class="fas fa-search"></i></span>
                  <input
                    v-model.trim="courseSearch"
                    type="search"
                    class="form-control"
                    placeholder="Search courses..."
                    aria-label="Search college courses"
                    :disabled="isSaving"
                  />
                </div>
                <div class="border rounded" style="max-height: 260px; overflow-y: auto;">
                  <div v-for="group in filteredCollegeCourseGroups" :key="group.name" class="course-group">
                    <div class="course-group-header">{{ group.name }}</div>
                    <div class="px-2 pt-1 pb-1">
                      <label v-for="course in group.courses" :key="course" class="form-check">
                        <input type="checkbox" class="form-check-input" :value="course" v-model="form.collegeCourses" :disabled="isSaving" />
                        <span class="form-check-label">{{ course }}</span>
                      </label>
                    </div>
                  </div>
                  <p v-if="!filteredCollegeCourseGroups.length" class="text-muted small mb-0 p-2">
                    No courses match "{{ courseSearch }}".
                  </p>
                </div>
                <div class="input-group input-group-sm mt-2">
                  <input
                    id="collegeCourseSpecialization"
                    v-model.trim="specializationInput"
                    type="text"
                    class="form-control"
                    maxlength="150"
                    placeholder="Specialization / Major (Optional)"
                    :disabled="isSaving"
                    @keydown.enter.prevent="addSpecializationCourse"
                  />
                  <button type="button" class="btn btn-outline-primary" :disabled="isSaving || !specializationInput" @click="addSpecializationCourse">
                    Add
                  </button>
                </div>
                <p class="form-text mb-0">Adds a custom entry, e.g. BSIT - Business Analytics.</p>
              </div>

              <div v-else class="mb-3">
                <label class="form-label fw-semibold">Senior High School Strands</label>
                <div class="border rounded p-2" style="max-height: 220px; overflow-y: auto;">
                  <label v-for="strand in seniorHighSchoolStrandOptions" :key="strand" class="form-check">
                    <input type="checkbox" class="form-check-input" :value="strand" v-model="form.seniorHighSchoolStrands" :disabled="isSaving" />
                    <span class="form-check-label">{{ strand }}</span>
                  </label>
                </div>
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
                <label class="form-label fw-semibold">Address</label>
                <div class="row g-2">
                  <div class="col-md-6">
                    <select
                      v-model="addr.region"
                      class="form-select"
                      :class="{ 'is-invalid': submitted && !addr.region }"
                      :disabled="isSaving || addrLoading.regions"
                      @change="onRegionChange"
                    >
                      <option value="">{{ addrLoading.regions ? 'Loading regions...' : 'Region' }}</option>
                      <option v-for="item in addrOptions.regions" :key="item.code" :value="item.code">{{ item.name }}</option>
                    </select>
                  </div>
                  <div class="col-md-6">
                    <select
                      v-model="addr.province"
                      class="form-select"
                      :class="{ 'is-invalid': submitted && !addr.province }"
                      :disabled="isSaving || !addr.region || addrLoading.provinces"
                      @change="onProvinceChange"
                    >
                      <option value="">{{ addrLoading.provinces ? 'Loading...' : 'Province / District' }}</option>
                      <option v-for="item in addrOptions.provinces" :key="item.code" :value="item.code">{{ item.name }}</option>
                    </select>
                  </div>
                  <div class="col-md-6">
                    <select
                      v-model="addr.city"
                      class="form-select"
                      :class="{ 'is-invalid': submitted && !addr.city }"
                      :disabled="isSaving || !addr.province || addrLoading.cities"
                      @change="onCityChange"
                    >
                      <option value="">{{ addrLoading.cities ? 'Loading...' : 'City / Municipality' }}</option>
                      <option v-for="item in addrOptions.cities" :key="item.code" :value="item.code">{{ item.name }}</option>
                    </select>
                  </div>
                  <div class="col-md-6">
                    <select
                      v-model="addr.barangay"
                      class="form-select"
                      :disabled="isSaving || !addr.city || addrLoading.barangays"
                    >
                      <option value="">{{ addrLoading.barangays ? 'Loading...' : 'Barangay (optional)' }}</option>
                      <option v-for="item in addrOptions.barangays" :key="item.code" :value="item.code">{{ item.name }}</option>
                    </select>
                  </div>
                  <div class="col-12">
                    <input
                      v-model.trim="addr.street"
                      type="text"
                      class="form-control"
                      maxlength="200"
                      placeholder="Street / Building (optional)"
                      :disabled="isSaving"
                    />
                  </div>
                </div>
                <div v-if="submitted && !composedAddress" class="invalid-feedback d-block">Region, province, and city are required.</div>
                <p v-if="composedAddress" class="form-text mb-0">{{ composedAddress }}</p>
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
                <input
                  id="schoolMoaExpiry"
                  type="date"
                  class="form-control"
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
import { Modal, Toast } from 'bootstrap'
import { getApiErrorMessage, schoolsAPI } from '@/services/api'

const schools = ref([])
const isLoading = ref(false)
const isSaving = ref(false)
const loadError = ref('')
const formError = ref('')
const submitted = ref(false)
const notification = reactive({ message: '', variant: 'success' })
const notificationTimeout = ref(null)

const collegeCourseGroups = [
  {
    name: 'Information Technology & Computing',
    courses: [
      'BS Information Technology (BSIT)',
      'BS Computer Science (BSCS)',
      'BS Information Systems (BSIS)',
      'BS Computer Engineering (BSCpE)',
      'BS Entertainment and Multimedia Computing (BSEMC)',
      'BS Data Science / Analytics'
    ]
  },
  {
    name: 'Business & Accountancy',
    courses: [
      'BS Accountancy (BSA)',
      'BS Business Administration (BSBA) - Financial Management',
      'BS Business Administration (BSBA) - Marketing Management',
      'BS Business Administration (BSBA) - Human Resource Management',
      'BS Business Administration (BSBA) - Operations Management',
      'BS Entrepreneurship (BSEntrep)',
      'BS Real Estate Management (BSREM)',
      'BS Office Administration (BSOA)',
      'BS Hospitality Management (BSHM)',
      'BS Tourism Management (BSTM)'
    ]
  },
  {
    name: 'Engineering & Architecture',
    courses: [
      'BS Civil Engineering (BSCE)',
      'BS Mechanical Engineering (BSME)',
      'BS Electrical Engineering (BSEE)',
      'BS Electronics Engineering (BSEC)',
      'BS Industrial Engineering (BSIE)',
      'BS Chemical Engineering (BSChE)',
      'BS Geodetic Engineering (BSGE)',
      'BS Architecture (BSArchi)'
    ]
  },
  {
    name: 'Health & Allied Sciences',
    courses: [
      'BS Nursing (BSN)',
      'BS Medical Technology / Medical Laboratory Science (BSMT)',
      'BS Pharmacy (BSPharma)',
      'BS Physical Therapy (BSPT)',
      'BS Radiologic Technology (BSRT)',
      'BS Nutrition and Dietetics (BSND)'
    ]
  },
  {
    name: 'Teacher Education',
    courses: [
      'Bachelor of Elementary Education (BEEd)',
      'Bachelor of Secondary Education (BSEd) - Major in English',
      'Bachelor of Secondary Education (BSEd) - Major in Mathematics',
      'Bachelor of Secondary Education (BSEd) - Major in Science',
      'Bachelor of Secondary Education (BSEd) - Major in Filipino',
      'Bachelor of Secondary Education (BSEd) - Major in Social Studies',
      'Bachelor of Special Needs Education (BSNEd)',
      'Bachelor of Physical Education (BPEd)'
    ]
  },
  {
    name: 'Arts, Sciences & Social Sciences',
    courses: [
      'BA / BS Psychology',
      'BA Communication / Mass Communication',
      'BA Political Science / Public Administration',
      'BS Criminology (BSCrim)',
      'BS Biology',
      'BS Chemistry',
      'BS Custom Administration (BSCA)'
    ]
  }
]

const collegeCourseOptions = collegeCourseGroups.flatMap((group) => group.courses)

const seniorHighSchoolStrandOptions = [
  'STEM',
  'ABM',
  'HUMSS',
  'GAS',
  'TVL',
  'Arts and Design Track',
  'Sports Track'
]

const form = reactive({
  id: 0,
  name: '',
  abbreviation: '',
  address: '',
  collegeCourses: [],
  seniorHighSchoolStrands: [],
  moaStatus: false,
  moaExpiry: ''
})
const isEditing = computed(() => form.id > 0)
const programType = ref('college')

const customCollegeCourses = ref([])
const courseSearch = ref('')
const specializationInput = ref('')

const PSGC = 'https://psgc.gitlab.io/api'
const addr = reactive({ region: '', province: '', city: '', barangay: '', street: '' })
const addrOptions = reactive({ regions: [], provinces: [], cities: [], barangays: [] })
const addrLoading = reactive({ regions: false, provinces: false, cities: false, barangays: false })

function psgcName(code, list) {
  return list.find((item) => item.code === code)?.name || ''
}

const composedAddress = computed(() => {
  const parts = [
    addr.street,
    psgcName(addr.barangay, addrOptions.barangays),
    psgcName(addr.city, addrOptions.cities),
    psgcName(addr.province, addrOptions.provinces),
    psgcName(addr.region, addrOptions.regions)
  ].filter(Boolean)
  return parts.join(', ')
})

async function psgcGet(path) {
  const res = await fetch(`${PSGC}${path}`)
  if (!res.ok) return []
  return res.json()
}

async function loadRegions() {
  if (addrOptions.regions.length) return
  addrLoading.regions = true
  try {
    addrOptions.regions = await psgcGet('/regions/')
  } finally {
    addrLoading.regions = false
  }
}

async function onRegionChange() {
  addr.province = ''
  addr.city = ''
  addr.barangay = ''
  addrOptions.provinces = []
  addrOptions.cities = []
  addrOptions.barangays = []
  if (!addr.region) return
  addrLoading.provinces = true
  try {
    const provinces = await psgcGet(`/regions/${addr.region}/provinces/`)
    addrOptions.provinces = provinces.length
      ? provinces
      : await psgcGet(`/regions/${addr.region}/districts/`)
  } finally {
    addrLoading.provinces = false
  }
}

async function onProvinceChange() {
  addr.city = ''
  addr.barangay = ''
  addrOptions.cities = []
  addrOptions.barangays = []
  if (!addr.province) return
  addrLoading.cities = true
  try {
    const cities = await psgcGet(`/provinces/${addr.province}/cities-municipalities/`)
    addrOptions.cities = cities.length
      ? cities
      : await psgcGet(`/districts/${addr.province}/cities-municipalities/`)
  } finally {
    addrLoading.cities = false
  }
}

async function onCityChange() {
  addr.barangay = ''
  addrOptions.barangays = []
  if (!addr.city) return
  addrLoading.barangays = true
  try {
    addrOptions.barangays = await psgcGet(`/cities-municipalities/${addr.city}/barangays/`)
  } finally {
    addrLoading.barangays = false
  }
}

function resetAddress() {
  addr.region = ''
  addr.province = ''
  addr.city = ''
  addr.barangay = ''
  addr.street = ''
  addrOptions.provinces = []
  addrOptions.cities = []
  addrOptions.barangays = []
}

function findByName(list, name) {
  const needle = String(name || '').trim().toLowerCase()
  if (!needle) return null
  return list.find((item) => item.name.toLowerCase() === needle)
    || list.find((item) => item.name.toLowerCase().includes(needle) || needle.includes(item.name.toLowerCase()))
}

async function hydrateAddress(address) {
  resetAddress()
  await loadRegions()
  const parts = String(address || '').split(',').map((part) => part.trim()).filter(Boolean)
  if (!parts.length) return

  const region = findByName(addrOptions.regions, parts[parts.length - 1])
  if (!region) {
    addr.street = address
    return
  }

  addr.region = region.code
  await onRegionChange()
  const province = findByName(addrOptions.provinces, parts[parts.length - 2])
  if (!province) {
    addr.street = parts.slice(0, -1).join(', ')
    return
  }

  addr.province = province.code
  await onProvinceChange()
  const city = findByName(addrOptions.cities, parts[parts.length - 3])
  if (!city) {
    addr.street = parts.slice(0, -2).join(', ')
    return
  }

  addr.city = city.code
  await onCityChange()
  const maybeBarangay = parts.length >= 4 ? findByName(addrOptions.barangays, parts[parts.length - 4]) : null
  if (maybeBarangay) {
    addr.barangay = maybeBarangay.code
    addr.street = parts.slice(0, -4).join(', ')
  } else {
    addr.street = parts.slice(0, -3).join(', ')
  }
}

const filteredCollegeCourseGroups = computed(() => {
  const query = courseSearch.value.trim().toLowerCase()

  const groups = collegeCourseGroups.map((group) => {
    const categoryMatches = group.name.toLowerCase().includes(query)

    return {
      name: group.name,
      courses: !query || categoryMatches
        ? group.courses
        : group.courses.filter((course) => course.toLowerCase().includes(query))
    }
  })

  const customCourses = [...customCollegeCourses.value, ...form.collegeCourses]
    .map((course) => String(course ?? '').trim())
    .filter((course, index, list) =>
      course && list.indexOf(course) === index && !collegeCourseOptions.includes(course)
    )
    .filter((course) => !query || course.toLowerCase().includes(query))

  if (customCourses.length) {
    groups.push({
      name: 'Specializations / Additional Courses',
      courses: customCourses
    })
  }

  return groups.filter((group) => group.courses.length)
})

function addSpecializationCourse() {
  const value = specializationInput.value.trim()
  if (!value) return

  if (!customCollegeCourses.value.includes(value)) {
    customCollegeCourses.value.push(value)
  }
  if (!form.collegeCourses.includes(value)) {
    form.collegeCourses.push(value)
  }

  courseSearch.value = ''
  specializationInput.value = ''
}

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
  form.collegeCourses = []
  form.seniorHighSchoolStrands = []
  form.moaStatus = false
  form.moaExpiry = ''
  programType.value = 'college'
  customCollegeCourses.value = []
  courseSearch.value = ''
  specializationInput.value = ''
  submitted.value = false
  formError.value = ''
  resetAddress()
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
  loadRegions()
  formModal.show()
}

/**
 * Normalises a school's stored course/strand value into a plain string array.
 *
 * The EF value converter normally deserialises the JSON column, so the API
 * returns real arrays. Older/raw records, however, may arrive as a JSON
 * string ('["BSIT","BSN"]') or a CSV string ('BSIT,BSN'). This always returns
 * a fresh array so the checkbox v-model never mutates the source object, and
 * trims each entry so it strictly matches the option values Vue compares
 * against when deciding which checkboxes to tick.
 */
function toArray(value) {
  if (Array.isArray(value)) {
    return value.map(item => String(item).trim()).filter(Boolean)
  }

  if (typeof value === 'string') {
    const text = value.trim()
    if (!text) return []

    if (text.startsWith('[')) {
      try {
        const parsed = JSON.parse(text)
        if (Array.isArray(parsed)) {
          return parsed.map(item => String(item).trim()).filter(Boolean)
        }
      } catch {
        /* Not valid JSON; fall through and treat it as CSV. */
      }
    }

    return text.split(',').map(item => item.trim()).filter(Boolean)
  }

  return []
}

function openEditModal(school) {
  emptyForm()
  form.id = school.id
  form.name = school.name
  form.abbreviation = school.abbreviation || ''
  form.address = school.address
  form.collegeCourses = toArray(school.collegeCourses)
  customCollegeCourses.value = form.collegeCourses.filter(
    (course) => !collegeCourseOptions.includes(course)
  )
  form.seniorHighSchoolStrands = toArray(school.seniorHighSchoolStrands)
  programType.value = form.collegeCourses.length || !form.seniorHighSchoolStrands.length ? 'college' : 'shs'
  form.moaStatus = school.moaStatus || false
  form.moaExpiry = school.moaExpiry ? school.moaExpiry.slice(0, 10) : ''
  hydrateAddress(school.address)
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

  form.address = composedAddress.value

  if (!form.name || !form.address || !addr.region || !addr.province || !addr.city) {
    formError.value = 'Please fill in all required fields.'
    return
  }

  isSaving.value = true

  const payload = {
    id: form.id,
    name: form.name,
    abbreviation: form.abbreviation,
    address: form.address,
    collegeCourses: form.collegeCourses,
    seniorHighSchoolStrands: form.seniorHighSchoolStrands,
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
  // Toast, hindi Modal — walang `.modal-dialog` ang `.toast` kaya magta-throw
  // ng "'querySelector' called on an object that does not implement interface Element."
  toast = new Toast(document.getElementById('schoolToast'), { autohide: false })
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

<style scoped>
.course-group + .course-group {
  border-top: 1px solid #dee2e6;
}

.course-group-header {
  position: sticky;
  top: 0;
  z-index: 1;
  padding: 0.35rem 0.5rem;
  border-bottom: 1px solid #dee2e6;
  background: #f8f9fa;
  color: #495057;
  font-size: 0.68rem;
  font-weight: 700;
  letter-spacing: 0.04em;
  text-transform: uppercase;
}
</style>
