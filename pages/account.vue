<template>
  <section class="content">
    <!-- Tabs -->
    <div class="d-flex flex-wrap gap-2 mb-3 account-tabs" role="group" aria-label="Account view switcher">
      <button
        type="button"
        class="btn"
        :class="activeTab === 'profile' ? 'btn-primary' : 'btn-outline-primary'"
        @click="setTab('profile')"
      >
        <i class="far fa-user-circle me-1"></i> Profile
      </button>
      <button
        type="button"
        class="btn"
        :class="activeTab === 'security' ? 'btn-primary' : 'btn-outline-primary'"
        @click="setTab('security')"
      >
        <i class="fas fa-key me-1"></i> Security
      </button>
      <button
        v-if="isAdmin"
        type="button"
        class="btn"
        :class="activeTab === 'users' ? 'btn-primary' : 'btn-outline-primary'"
        @click="setTab('users')"
      >
        <i class="fas fa-users-cog me-1"></i> User Management
      </button>
    </div>

    <!-- ================= PROFILE TAB ================= -->
    <div v-if="activeTab === 'profile'">
      <div v-if="isLoadingProfile" class="text-center my-5">
        <div class="spinner-border text-primary" role="status">
          <span class="visually-hidden">Loading...</span>
        </div>
        <p class="text-muted mt-2 mb-0">Loading your profile...</p>
      </div>

      <div v-else-if="profileError" class="alert alert-danger d-flex justify-content-between align-items-center">
        <span><i class="fas fa-exclamation-triangle me-2"></i>{{ profileError }}</span>
        <button type="button" class="btn btn-sm btn-outline-danger" @click="loadProfile">Retry</button>
      </div>

      <div v-else class="row g-3">
        <div class="col-lg-4">
          <div class="card shadow-sm h-100">
            <div class="card-body text-center d-flex flex-column align-items-center justify-content-center">
              <div class="account-avatar mb-3">
                <img v-if="profileForm.avatarUrl" :src="profileForm.avatarUrl" alt="Profile picture" />
                <span v-else>{{ initials }}</span>
              </div>

              <h4 class="mb-1 text-dark">{{ profileForm.firstName }} {{ profileForm.lastName }}</h4>
              <p class="text-muted mb-2">{{ profileForm.email }}</p>

              <div class="d-flex flex-wrap gap-1 justify-content-center mb-3">
                <span
                  v-for="role in profileRoles"
                  :key="role"
                  class="badge"
                  :class="roleBadgeClass(role)"
                >
                  {{ role }}
                </span>
              </div>

              <div class="d-flex flex-wrap gap-2 justify-content-center">
                <label class="btn btn-primary btn-sm mb-0" :class="{ disabled: isSavingProfile }">
                  <i class="fas fa-camera me-1"></i>
                  {{ profileForm.avatarUrl ? 'Change Photo' : 'Upload Photo' }}
                  <input
                    ref="avatarInput"
                    type="file"
                    accept="image/*"
                    class="d-none"
                    :disabled="isSavingProfile"
                    @change="handleAvatarChange"
                  />
                </label>
                <button
                  v-if="profileForm.avatarUrl"
                  type="button"
                  class="btn btn-outline-secondary btn-sm"
                  :disabled="isSavingProfile"
                  @click="profileForm.avatarUrl = ''"
                >
                  Remove
                </button>
              </div>

              <p class="text-muted small mt-3 mb-0">
                JPG or PNG, up to 5 MB. The photo is resized to 256&times;256.
              </p>
            </div>
          </div>
        </div>

        <div class="col-lg-8">
          <div class="card shadow-sm h-100">
            <div class="card-header">
              <h3 class="card-title mb-0">Profile Information</h3>
            </div>
            <div class="card-body">
              <div v-if="profileFormError" class="alert alert-danger py-2">
                {{ profileFormError }}
              </div>

              <form novalidate @submit.prevent="saveProfile">
                <div class="row g-3">
                  <div class="col-md-6">
                    <label for="accountFirstName" class="form-label fw-semibold">First Name</label>
                    <input
                      id="accountFirstName"
                      v-model.trim="profileForm.firstName"
                      type="text"
                      class="form-control"
                      :class="{ 'is-invalid': profileSubmitted && !profileForm.firstName }"
                      maxlength="50"
                      autocomplete="given-name"
                      :disabled="isSavingProfile"
                    />
                    <div class="invalid-feedback">First name is required.</div>
                  </div>

                  <div class="col-md-6">
                    <label for="accountLastName" class="form-label fw-semibold">Last Name</label>
                    <input
                      id="accountLastName"
                      v-model.trim="profileForm.lastName"
                      type="text"
                      class="form-control"
                      :class="{ 'is-invalid': profileSubmitted && !profileForm.lastName }"
                      maxlength="50"
                      autocomplete="family-name"
                      :disabled="isSavingProfile"
                    />
                    <div class="invalid-feedback">Last name is required.</div>
                  </div>

                  <div class="col-md-6">
                    <label for="accountEmail" class="form-label fw-semibold">Email</label>
                    <input
                      id="accountEmail"
                      v-model.trim="profileForm.email"
                      type="email"
                      class="form-control"
                      :class="{ 'is-invalid': profileSubmitted && !isValidEmail(profileForm.email) }"
                      maxlength="256"
                      autocomplete="email"
                      :disabled="isSavingProfile"
                    />
                    <div class="invalid-feedback">Enter a valid email address.</div>
                  </div>

                  <div class="col-md-6">
                    <label for="accountOffice" class="form-label fw-semibold">Office</label>
                    <input
                      id="accountOffice"
                      v-model.trim="profileForm.office"
                      type="text"
                      class="form-control"
                      maxlength="150"
                      placeholder="e.g., ISDMD"
                      :disabled="isSavingProfile"
                    />
                  </div>

                  <div class="col-md-6">
                    <label class="form-label fw-semibold d-block">Role</label>
                    <div class="d-flex flex-wrap gap-1 pt-1">
                      <span
                        v-for="role in profileRoles"
                        :key="role"
                        class="badge"
                        :class="roleBadgeClass(role)"
                      >
                        {{ role }}
                      </span>
                      <span v-if="profileRoles.length === 0" class="text-muted small">No role assigned</span>
                    </div>
                    <div class="form-text">Roles are managed by an HR Admin.</div>
                  </div>

                  <div class="col-md-6">
                    <label class="form-label fw-semibold d-block">Member Since</label>
                    <p class="form-control-plaintext mb-0 pt-1">{{ formatDate(profileCreatedAt) }}</p>
                  </div>
                </div>

                <div class="d-flex justify-content-end mt-4">
                  <button type="submit" class="btn btn-primary" :disabled="isSavingProfile">
                    <span
                      v-if="isSavingProfile"
                      class="spinner-border spinner-border-sm me-1"
                      aria-hidden="true"
                    ></span>
                    Save Changes
                  </button>
                </div>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= SECURITY TAB ================= -->
    <div v-else-if="activeTab === 'security'">
      <div class="row justify-content-center">
        <div class="col-lg-7 col-xl-6">
          <div class="card shadow-sm">
            <div class="card-header">
              <h3 class="card-title mb-0">Change Password</h3>
            </div>
            <div class="card-body">
              <p class="text-muted small mb-3">
                Use at least 8 characters and include a number.
              </p>

              <div v-if="passwordError" class="alert alert-danger py-2">{{ passwordError }}</div>
              <div v-if="passwordSuccess" class="alert alert-success py-2">{{ passwordSuccess }}</div>

              <form novalidate @submit.prevent="submitPasswordChange">
                <div class="mb-3">
                  <label for="currentPassword" class="form-label fw-semibold">Current Password</label>
                  <input
                    id="currentPassword"
                    v-model="passwordForm.currentPassword"
                    :type="showPasswords ? 'text' : 'password'"
                    class="form-control"
                    :class="{ 'is-invalid': passwordSubmitted && !passwordForm.currentPassword }"
                    autocomplete="current-password"
                    :disabled="isSavingPassword"
                  />
                  <div class="invalid-feedback">Current password is required.</div>
                </div>

                <div class="mb-3">
                  <label for="newPassword" class="form-label fw-semibold">New Password</label>
                  <input
                    id="newPassword"
                    v-model="passwordForm.newPassword"
                    :type="showPasswords ? 'text' : 'password'"
                    class="form-control"
                    :class="{ 'is-invalid': newPasswordInvalid }"
                    autocomplete="new-password"
                    :disabled="isSavingPassword"
                  />
                  <div class="invalid-feedback">
                    New password must be at least 8 characters and include a number.
                  </div>
                </div>

                <div class="mb-3">
                  <label for="confirmPassword" class="form-label fw-semibold">Confirm New Password</label>
                  <input
                    id="confirmPassword"
                    v-model="passwordForm.confirmPassword"
                    :type="showPasswords ? 'text' : 'password'"
                    class="form-control"
                    :class="{ 'is-invalid': confirmPasswordInvalid }"
                    autocomplete="new-password"
                    :disabled="isSavingPassword"
                  />
                  <div class="invalid-feedback">Passwords do not match.</div>
                </div>

                <div class="form-check mb-3">
                  <input
                    id="showPasswords"
                    v-model="showPasswords"
                    type="checkbox"
                    class="form-check-input"
                  />
                  <label class="form-check-label" for="showPasswords">Show passwords</label>
                </div>

                <button type="submit" class="btn btn-primary" :disabled="isSavingPassword">
                  <span
                    v-if="isSavingPassword"
                    class="spinner-border spinner-border-sm me-1"
                    aria-hidden="true"
                  ></span>
                  Update Password
                </button>
              </form>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ================= USER MANAGEMENT TAB (ADMIN ONLY) ================= -->
    <div v-else-if="activeTab === 'users' && isAdmin">
      <div class="card shadow-sm">
        <div class="card-header d-flex flex-wrap justify-content-between align-items-center gap-2">
          <h3 class="card-title mb-0">User Management</h3>
          <button
            type="button"
            class="btn btn-primary btn-sm"
            :disabled="isLoadingUsers"
            @click="openCreateUser"
          >
            <i class="fas fa-plus me-1"></i> Add User
          </button>
        </div>

        <div class="card-body">
          <div class="row g-2 mb-3">
            <div class="col-md-6">
              <input
                v-model.trim="userSearch"
                type="search"
                class="form-control"
                placeholder="Search name, email, or office"
                aria-label="Search users"
              />
            </div>
            <div class="col-md-3">
              <select v-model="userRoleFilter" class="form-select" aria-label="Filter by role">
                <option value="all">All roles</option>
                <option v-for="role in roleOptions" :key="role" :value="role">{{ role }}</option>
              </select>
            </div>
            <div class="col-md-3 d-flex align-items-center justify-content-md-end">
              <span class="text-muted small">{{ filteredUsers.length }} user(s)</span>
            </div>
          </div>

          <div v-if="isLoadingUsers" class="text-center my-4">
            <div class="spinner-border text-primary" role="status">
              <span class="visually-hidden">Loading...</span>
            </div>
            <p class="text-muted mt-2 mb-0">Loading users...</p>
          </div>

          <div v-else-if="usersError" class="alert alert-danger d-flex justify-content-between align-items-center">
            <span><i class="fas fa-exclamation-triangle me-2"></i>{{ usersError }}</span>
            <button type="button" class="btn btn-sm btn-outline-danger" @click="loadUsers">Retry</button>
          </div>

          <div v-else class="table-responsive">
            <table class="table table-hover align-middle mb-0">
              <thead class="table-light">
                <tr>
                  <th scope="col" class="text-center" style="width: 60px;">#</th>
                  <th scope="col">Name</th>
                  <th scope="col">Email</th>
                  <th scope="col">Role</th>
                  <th scope="col">Office</th>
                  <th scope="col" style="width: 130px;">Created</th>
                  <th scope="col" class="text-center" style="width: 90px;">Actions</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="(user, index) in filteredUsers" :key="user.id">
                  <td class="text-center text-dark">{{ index + 1 }}</td>
                  <td>
                    <div class="d-flex align-items-center gap-2">
                      <span class="user-avatar">
                        <img v-if="user.avatarUrl" :src="user.avatarUrl" alt="" />
                        <template v-else>{{ initialsFor(user) }}</template>
                      </span>
                      <span class="fw-semibold text-dark">{{ user.fullName || '-' }}</span>
                    </div>
                  </td>
                  <td class="text-dark">{{ user.email }}</td>
                  <td>
                    <span
                      v-for="role in user.roles"
                      :key="role"
                      class="badge me-1"
                      :class="roleBadgeClass(role)"
                    >
                      {{ role }}
                    </span>
                    <span v-if="!user.roles?.length" class="text-muted small">None</span>
                  </td>
                  <td class="text-dark">{{ user.office || '-' }}</td>
                  <td class="text-muted small">{{ formatDate(user.createdAt) }}</td>
                  <td class="text-center">
                    <button
                      type="button"
                      class="btn btn-sm btn-outline-primary"
                      title="Edit user"
                      :disabled="isSavingUser"
                      @click="openEditUser(user)"
                    >
                      <i class="fas fa-pen"></i>
                    </button>
                  </td>
                </tr>
                <tr v-if="filteredUsers.length === 0">
                  <td colspan="7" class="text-center text-muted py-4">
                    No users match your filters.
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>

    <!-- Add / Edit user modal -->
    <div
      ref="userModalEl"
      id="accountUserModal"
      class="modal fade"
      tabindex="-1"
      aria-labelledby="accountUserModalLabel"
      aria-hidden="true"
    >
      <div class="modal-dialog modal-dialog-centered modal-lg">
        <div class="modal-content">
          <form novalidate @submit.prevent="saveUser">
            <div class="modal-header">
              <h5 id="accountUserModalLabel" class="modal-title">
                {{ isEditingUser ? 'Edit User' : 'Add User' }}
              </h5>
              <button
                type="button"
                class="btn-close"
                aria-label="Close"
                :disabled="isSavingUser"
                @click="closeUserModal"
              ></button>
            </div>

            <div class="modal-body">
              <div v-if="userFormError" class="alert alert-danger py-2">{{ userFormError }}</div>

              <div class="row g-3">
                <div class="col-md-6">
                  <label for="userFirstName" class="form-label fw-semibold">First Name</label>
                  <input
                    id="userFirstName"
                    v-model.trim="userForm.firstName"
                    type="text"
                    class="form-control"
                    :class="{ 'is-invalid': userSubmitted && !userForm.firstName }"
                    maxlength="50"
                    :disabled="isSavingUser"
                  />
                  <div class="invalid-feedback">First name is required.</div>
                </div>

                <div class="col-md-6">
                  <label for="userLastName" class="form-label fw-semibold">Last Name</label>
                  <input
                    id="userLastName"
                    v-model.trim="userForm.lastName"
                    type="text"
                    class="form-control"
                    :class="{ 'is-invalid': userSubmitted && !userForm.lastName }"
                    maxlength="50"
                    :disabled="isSavingUser"
                  />
                  <div class="invalid-feedback">Last name is required.</div>
                </div>

                <div class="col-md-6">
                  <label for="userEmail" class="form-label fw-semibold">Email</label>
                  <input
                    id="userEmail"
                    v-model.trim="userForm.email"
                    type="email"
                    class="form-control"
                    :class="{ 'is-invalid': userSubmitted && !isValidEmail(userForm.email) }"
                    maxlength="256"
                    :disabled="isSavingUser"
                  />
                  <div class="invalid-feedback">Enter a valid email address.</div>
                </div>

                <div class="col-md-6">
                  <label for="userOffice" class="form-label fw-semibold">Office</label>
                  <input
                    id="userOffice"
                    v-model.trim="userForm.office"
                    type="text"
                    class="form-control"
                    maxlength="150"
                    placeholder="e.g., ISDMD"
                    :disabled="isSavingUser"
                  />
                </div>

                <div class="col-md-6">
                  <label for="userRole" class="form-label fw-semibold">Role</label>
                  <select
                    id="userRole"
                    v-model="userForm.role"
                    class="form-select"
                    :disabled="isSavingUser"
                  >
                    <option v-for="role in roleOptions" :key="role" :value="role">{{ role }}</option>
                  </select>
                </div>

                <div class="col-md-6">
                  <label for="userPassword" class="form-label fw-semibold">
                    Password
                    <span v-if="isEditingUser" class="text-muted fw-normal">(leave blank to keep current)</span>
                  </label>
                  <input
                    id="userPassword"
                    v-model="userForm.password"
                    type="password"
                    class="form-control"
                    :class="{ 'is-invalid': userSubmitted && !isEditingUser && !userForm.password }"
                    autocomplete="new-password"
                    :disabled="isSavingUser"
                  />
                  <div class="invalid-feedback">Password is required for new users.</div>
                  <div class="form-text">At least 8 characters and include a number.</div>
                </div>
              </div>
            </div>

            <div class="modal-footer">
              <button type="submit" class="btn btn-primary" :disabled="isSavingUser">
                <span
                  v-if="isSavingUser"
                  class="spinner-border spinner-border-sm me-1"
                  aria-hidden="true"
                ></span>
                {{ isEditingUser ? 'Save Changes' : 'Create User' }}
              </button>
              <button
                type="button"
                class="btn btn-outline-secondary"
                :disabled="isSavingUser"
                @click="closeUserModal"
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <!-- Notification toast -->
    <div class="toast-container position-fixed top-0 end-0 p-3">
      <div
        id="accountToast"
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
import {
  accountAPI,
  adminUsersAPI,
  currentUser,
  getApiErrorMessage,
  isHRAdmin,
  saveProfile as persistProfile
} from '@/services/api'

const roleOptions = ['HRAdmin', 'Supervisor', 'Intern']
const isAdmin = ref(isHRAdmin())
const activeTab = ref('profile')

/* ---- Toast ---- */
const toastEl = ref(null)
let toast = null
const notification = reactive({ message: '', variant: 'success' })
const notificationTimeout = ref(null)

function showToast(message, variant = 'success') {
  notification.message = message
  notification.variant = variant

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

function removeModalBackdrops() {
  if (typeof document === 'undefined') return
  document.querySelectorAll?.('.modal-backdrop')?.forEach?.(el => el.remove?.())
  document.body?.classList?.remove('modal-open')
  document.body.style.overflow = ''
  document.body.style.paddingRight = ''
}

/* ---- Shared helpers ---- */
function isValidEmail(value) {
  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test((value || '').trim())
}

function formatDate(value) {
  if (!value) return '-'
  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) return '-'
  return parsed.toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: '2-digit'
  })
}

function roleBadgeClass(role) {
  if (role === 'HRAdmin') return 'text-bg-warning'
  if (role === 'Supervisor') return 'text-bg-info'
  return 'text-bg-secondary'
}

function initialsFor(user) {
  const first = (user.firstName || user.fullName || '').trim().charAt(0)
  const last = (user.lastName || '').trim().charAt(0)
  return `${first}${last}`.toUpperCase() || '?'
}

/* ============================================================
   PROFILE
   ============================================================ */
const isLoadingProfile = ref(false)
const profileError = ref('')
const isSavingProfile = ref(false)
const profileFormError = ref('')
const profileSubmitted = ref(false)
const profileRoles = ref([])
const profileCreatedAt = ref('')
const avatarInput = ref(null)

const profileForm = reactive({
  firstName: '',
  lastName: '',
  email: '',
  office: '',
  avatarUrl: ''
})

const initials = computed(() => {
  const first = (profileForm.firstName || '').trim().charAt(0)
  const last = (profileForm.lastName || '').trim().charAt(0)
  return `${first}${last}`.toUpperCase() || '?'
})

function setTab(tab) {
  if (tab === 'users' && !isAdmin.value) return
  activeTab.value = tab

  if (tab === 'users' && !usersLoaded.value) {
    loadUsers()
  }
}

function applyProfile(profile) {
  profileForm.firstName = profile.firstName || ''
  profileForm.lastName = profile.lastName || ''
  profileForm.email = profile.email || ''
  profileForm.office = profile.office || ''
  profileForm.avatarUrl = profile.avatarUrl || ''
  profileRoles.value = profile.roles || []
  profileCreatedAt.value = profile.createdAt || ''

  // Keep the navbar greeting and the stored session in sync.
  persistProfile(profile)
  window.dispatchEvent(new Event('ojt:profile-changed'))
}

async function loadProfile() {
  isLoadingProfile.value = true
  profileError.value = ''

  try {
    const profile = await accountAPI.getProfile()
    applyProfile(profile)
  } catch (error) {
    profileError.value = getApiErrorMessage(error, 'Failed to load your profile.')
  } finally {
    isLoadingProfile.value = false
  }
}

async function saveProfile() {
  profileSubmitted.value = true
  profileFormError.value = ''

  if (!profileForm.firstName || !profileForm.lastName) {
    profileFormError.value = 'First name and last name are required.'
    return
  }

  if (!isValidEmail(profileForm.email)) {
    profileFormError.value = 'Please enter a valid email address.'
    return
  }

  isSavingProfile.value = true

  try {
    const updated = await accountAPI.updateProfile({
      firstName: profileForm.firstName.trim(),
      lastName: profileForm.lastName.trim(),
      email: profileForm.email.trim(),
      office: profileForm.office.trim() || null,
      avatarUrl: profileForm.avatarUrl || null
    })

    applyProfile(updated)
    profileSubmitted.value = false
    showToast('Profile updated successfully.')
  } catch (error) {
    profileFormError.value = getApiErrorMessage(error, 'Failed to update your profile.')
  } finally {
    isSavingProfile.value = false
  }
}

/**
 * Reads the selected image, centre-crops it to a square and compresses it to
 * a 256x256 JPEG data URL. Keeping the image small avoids oversized API
 * payloads while still looking sharp in the profile card.
 */
function resizeImage(file, size) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()

    reader.onload = () => {
      const image = new Image()

      image.onload = () => {
        const canvas = document.createElement('canvas')
        canvas.width = size
        canvas.height = size

        const context = canvas.getContext('2d')
        if (!context) {
          reject(new Error('Could not process that image.'))
          return
        }

        const scale = Math.max(size / image.width, size / image.height)
        const width = image.width * scale
        const height = image.height * scale

        context.drawImage(
          image,
          (size - width) / 2,
          (size - height) / 2,
          width,
          height
        )

        resolve(canvas.toDataURL('image/jpeg', 0.86))
      }

      image.onerror = () => reject(new Error('Could not read that image.'))
      image.src = reader.result
    }

    reader.onerror = () => reject(new Error('Could not read the selected file.'))
    reader.readAsDataURL(file)
  })
}

async function handleAvatarChange(event) {
  const input = event.target
  const file = input.files?.[0]
  input.value = ''

  if (!file) return

  if (!file.type.startsWith('image/')) {
    showToast('Please choose an image file.', 'danger')
    return
  }

  if (file.size > 5 * 1024 * 1024) {
    showToast('Image must be 5 MB or smaller.', 'danger')
    return
  }

  try {
    profileForm.avatarUrl = await resizeImage(file, 256)
  } catch (error) {
    showToast(error.message || 'Could not read that image.', 'danger')
  }
}

/* ============================================================
   SECURITY
   ============================================================ */
const passwordForm = reactive({
  currentPassword: '',
  newPassword: '',
  confirmPassword: ''
})
const passwordSubmitted = ref(false)
const passwordError = ref('')
const passwordSuccess = ref('')
const isSavingPassword = ref(false)
const showPasswords = ref(false)

const newPasswordInvalid = computed(() => {
  if (!passwordSubmitted.value) return false
  const value = passwordForm.newPassword
  return !value || value.length < 8 || !/\d/.test(value)
})

const confirmPasswordInvalid = computed(() => {
  if (!passwordSubmitted.value) return false
  return passwordForm.confirmPassword !== passwordForm.newPassword
})

async function submitPasswordChange() {
  passwordSubmitted.value = true
  passwordError.value = ''
  passwordSuccess.value = ''

  const current = passwordForm.currentPassword
  const next = passwordForm.newPassword
  const confirm = passwordForm.confirmPassword

  if (!current) {
    passwordError.value = 'Current password is required.'
    return
  }

  if (!next || next.length < 8) {
    passwordError.value = 'New password must be at least 8 characters long.'
    return
  }

  if (!/\d/.test(next)) {
    passwordError.value = 'New password must include at least one number.'
    return
  }

  if (next !== confirm) {
    passwordError.value = 'New password and confirmation do not match.'
    return
  }

  if (next === current) {
    passwordError.value = 'New password must be different from the current password.'
    return
  }

  isSavingPassword.value = true

  try {
    await accountAPI.changePassword({
      currentPassword: current,
      newPassword: next,
      confirmPassword: confirm
    })

    passwordSuccess.value = 'Password changed successfully.'
    passwordForm.currentPassword = ''
    passwordForm.newPassword = ''
    passwordForm.confirmPassword = ''
    passwordSubmitted.value = false
    showToast('Password changed successfully.')
  } catch (error) {
    passwordError.value = getApiErrorMessage(error, 'Failed to change your password.')
  } finally {
    isSavingPassword.value = false
  }
}

/* ============================================================
   USER MANAGEMENT (ADMIN)
   ============================================================ */
const users = ref([])
const usersLoaded = ref(false)
const isLoadingUsers = ref(false)
const usersError = ref('')
const userSearch = ref('')
const userRoleFilter = ref('all')

const filteredUsers = computed(() => {
  const query = userSearch.value.trim().toLowerCase()

  return users.value.filter((user) => {
    const roles = user.roles || []

    if (userRoleFilter.value !== 'all' && !roles.includes(userRoleFilter.value)) {
      return false
    }

    if (!query) return true

    return [user.fullName, user.email, user.office]
      .filter(Boolean)
      .some((value) => String(value).toLowerCase().includes(query))
  })
})

async function loadUsers() {
  isLoadingUsers.value = true
  usersError.value = ''

  try {
    users.value = await adminUsersAPI.getAll()
    usersLoaded.value = true
  } catch (error) {
    usersError.value = getApiErrorMessage(error, 'Failed to load users.')
  } finally {
    isLoadingUsers.value = false
  }
}

function sortUsers() {
  users.value.sort((left, right) =>
    (left.fullName || '').localeCompare(right.fullName || '')
  )
}

/* ---- Add / edit modal ---- */
const userModalEl = ref(null)
let userModal = null
const isSavingUser = ref(false)
const userSubmitted = ref(false)
const userFormError = ref('')

const userForm = reactive({
  id: '',
  firstName: '',
  lastName: '',
  email: '',
  office: '',
  role: 'Intern',
  password: ''
})

const isEditingUser = computed(() => Boolean(userForm.id))

function resetUserForm() {
  userForm.id = ''
  userForm.firstName = ''
  userForm.lastName = ''
  userForm.email = ''
  userForm.office = ''
  userForm.role = 'Intern'
  userForm.password = ''
  userSubmitted.value = false
  userFormError.value = ''
}

function openCreateUser() {
  resetUserForm()
  userModal.show()
}

function openEditUser(user) {
  resetUserForm()
  userForm.id = user.id
  userForm.firstName = user.firstName || ''
  userForm.lastName = user.lastName || ''
  userForm.email = user.email || ''
  userForm.office = user.office || ''
  userForm.role = user.roles?.[0] || 'Intern'
  userModal.show()
}

function closeUserModal() {
  if (isSavingUser.value) return
  userModal.hide()
  removeModalBackdrops()
}

function validateUserForm() {
  if (!userForm.firstName || !userForm.lastName) {
    return 'First name and last name are required.'
  }

  if (!isValidEmail(userForm.email)) {
    return 'Please enter a valid email address.'
  }

  if (!roleOptions.includes(userForm.role)) {
    return 'Please choose a valid role.'
  }

  if (!isEditingUser.value && !userForm.password) {
    return 'Password is required for new users.'
  }

  if (userForm.password) {
    if (userForm.password.length < 8) {
      return 'Password must be at least 8 characters long.'
    }

    if (!/\d/.test(userForm.password)) {
      return 'Password must include at least one number.'
    }
  }

  return ''
}

async function saveUser() {
  userSubmitted.value = true
  userFormError.value = ''

  const validationError = validateUserForm()
  if (validationError) {
    userFormError.value = validationError
    return
  }

  isSavingUser.value = true

  const payload = {
    firstName: userForm.firstName.trim(),
    lastName: userForm.lastName.trim(),
    email: userForm.email.trim(),
    office: userForm.office.trim() || null,
    role: userForm.role
  }

  if (userForm.password) {
    payload.password = userForm.password
  }

  try {
    if (isEditingUser.value) {
      const updated = await adminUsersAPI.update(userForm.id, payload)
      const index = users.value.findIndex((user) => user.id === updated.id)

      if (index !== -1) users.value[index] = updated
      else users.value.push(updated)

      sortUsers()

      // If the admin edited their own account, update the stored session too.
      if (updated.id === currentUser()?.id) {
        persistProfile({ ...currentUser(), ...updated })
        window.dispatchEvent(new Event('ojt:profile-changed'))
      }

      showToast('User updated successfully.')
    } else {
      const created = await adminUsersAPI.create(payload)
      users.value.push(created)
      sortUsers()
      showToast('User created successfully.')
    }

    userSubmitted.value = false
    userModal.hide()
    removeModalBackdrops()
  } catch (error) {
    userFormError.value = getApiErrorMessage(error, 'Failed to save the user.')
  } finally {
    isSavingUser.value = false
  }
}

/* ============================================================
   LIFECYCLE
   ============================================================ */
onMounted(() => {
  userModal = new Modal(userModalEl.value)
  toast = new Toast(document.getElementById('accountToast'), { autohide: false })
  toastEl.value = document.getElementById('accountToast')

  loadProfile()
})

onBeforeUnmount(() => {
  userModal?.dispose()
  toast?.dispose()
  window.clearTimeout(notificationTimeout.value)
})
</script>

<style scoped>
.account-tabs .btn {
  min-width: 150px;
  font-weight: 600;
}

.account-avatar {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 128px;
  height: 128px;
  overflow: hidden;
  border-radius: 50%;
  background: linear-gradient(135deg, #ff8c1a, #cc6a0f);
  color: #ffffff;
  font-size: 2.5rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  box-shadow: 0 10px 24px rgba(255, 140, 26, 0.28);
}

.account-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.user-avatar {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 34px;
  height: 34px;
  overflow: hidden;
  flex: 0 0 auto;
  border-radius: 50%;
  background: #fff0dc;
  color: #a85300;
  font-size: 0.72rem;
  font-weight: 800;
}

.user-avatar img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

@media (max-width: 575.98px) {
  .account-tabs .btn {
    flex: 1 1 100%;
    min-width: 0;
  }
}
</style>
