<template>
  <section class="content">
    <div class="card modern-card">
      <div class="card-header modern-card-header">
        <h3 class="card-title">User Management</h3>
        <div class="card-tools">
          <button type="button" class="btn btn-primary btn-sm modern-btn intern-request-action" @click="openCreate">
            <i class="fas fa-plus"></i> Add User
          </button>
        </div>
      </div>
      <div class="card-body">
        <div class="table-responsive">
          <table class="table table-hover table-bordered modern-table">
            <thead>
              <tr>
                <th>EMPLOYEE NO.</th>
                <th>LAST NAME</th>
                <th>FIRST NAME</th>
                <th>OFFICE</th>
                <th>ACTION</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="user in users" :key="user.employeeNo">
                <td>{{ user.employeeNo }}</td>
                <td>{{ user.lastName }}</td>
                <td>{{ user.firstName }}</td>
                <td>{{ user.office }}</td>
                <td class="text-center">
                  <button type="button" class="btn btn-outline-primary btn-sm modern-action mr-1" @click="openEdit(user)">
                    <i class="fas fa-edit"></i> Edit
                  </button>
                  <button type="button" class="btn btn-outline-danger btn-sm modern-action" @click="deleteUser(user)">
                    <i class="fa fa-times"></i> Delete
                  </button>
                </td>
              </tr>
              <tr v-if="users.length === 0">
                <td colspan="5" class="text-center text-muted py-4">No users found.</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <div v-if="isModalOpen" class="modal-backdrop fade show" @click="closeModal"></div>
    <div v-if="isModalOpen" class="modal fade show" style="display: block" tabindex="-1" role="dialog" aria-modal="true" @click.self="closeModal">
      <div class="modal-dialog modal-lg modal-dialog-centered">
        <div class="modal-content">
          <div class="modal-header bg-orange py-2">
            <h5 class="modal-title text-white">{{ editingUser ? 'Edit User' : 'Add User' }}</h5>
            <button type="button" class="close text-white" aria-label="Close" @click="closeModal">&times;</button>
          </div>
          <form @submit.prevent="saveUser">
            <div class="modal-body p-4">
              <div class="row">
                <div class="col-md-6">
                  <div class="form-group">
                    <label class="font-weight-bold">First Name</label>
                    <input v-model="form.firstName" type="text" class="form-control" placeholder="First Name" required>
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Middle Name</label>
                    <input v-model="form.middleName" type="text" class="form-control" placeholder="Middle Name">
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Last Name</label>
                    <input v-model="form.lastName" type="text" class="form-control" placeholder="Last Name" required>
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Suffix</label>
                    <select v-model="form.suffix" class="form-control utility-select">
                      <option value="">Select a suffix</option>
                      <option>Jr.</option>
                      <option>Sr.</option>
                      <option>III</option>
                    </select>
                  </div>
                </div>
                <div class="col-md-6">
                  <div class="form-group">
                    <label class="font-weight-bold">Office</label>
                    <select v-model="form.office" class="form-control utility-select" required>
                      <option v-if="!editingUser" value="">Select an office</option>
                      <option v-for="office in offices" :key="office.code" :value="office.code">{{ office.name }}</option>
                    </select>
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Email</label>
                    <input v-model="form.email" type="email" class="form-control" placeholder="Email" required>
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Username</label>
                    <input v-model="form.username" type="text" class="form-control" placeholder="Username" required>
                  </div>
                  <div class="form-group">
                    <label class="font-weight-bold">Password</label>
                    <input v-model="form.password" type="password" class="form-control" placeholder="Password" :required="!editingUser">
                  </div>
                </div>
              </div>
            </div>
            <div class="modal-footer border-0">
              <button type="submit" class="btn btn-success modern-action px-4"><i class="fas fa-save mr-1"></i> Save</button>
              <button type="button" class="btn btn-outline-secondary modern-action px-4" @click="closeModal"><i class="fas fa-ban mr-1"></i> Cancel</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import '../assets/css/Utility.css'

const USERS_KEY = 'ojt_users_v1'
const emptyForm = () => ({
  firstName: '', middleName: '', lastName: '', suffix: '', office: '',
  email: '', username: '', password: ''
})
const users = ref([])
const offices = ref([])
const isModalOpen = ref(false)
const editingUser = ref(null)
const form = ref(emptyForm())

function loadUsers() {
  try {
    const raw = localStorage.getItem(USERS_KEY)
    users.value = raw ? JSON.parse(raw) : [{
      employeeNo: 'E-2024-10-001',
      firstName: 'test',
      middleName: '',
      lastName: 'account',
      suffix: '',
      office: 'TEMP_GS',
      email: '',
      username: 'account',
      password: ''
    }]
  } catch (error) {
    console.error('Failed to load users:', error)
    users.value = []
  }
}

function persistUsers() {
  localStorage.setItem(USERS_KEY, JSON.stringify(users.value))
}

function openCreate() {
  editingUser.value = null
  form.value = emptyForm()
  isModalOpen.value = true
}

function openEdit(user) {
  editingUser.value = user.employeeNo
  const selectedOffice = offices.value.find((office) => office.code === user.office || office.name === user.office)
  form.value = { ...user, office: selectedOffice?.code || user.office }
  isModalOpen.value = true
}

function closeModal() {
  isModalOpen.value = false
}

function saveUser() {
  const employeeNo = editingUser.value || `E-${new Date().getFullYear()}-10-${String(users.value.length + 1).padStart(3, '0')}`
  const user = { ...form.value, employeeNo }
  const index = users.value.findIndex((item) => item.employeeNo === employeeNo)
  if (index === -1) users.value.push(user)
  else users.value[index] = user
  persistUsers()
  closeModal()
}

function deleteUser(user) {
  if (!window.confirm(`Delete user ${user.firstName} ${user.lastName}?`)) return
  users.value = users.value.filter((item) => item.employeeNo !== user.employeeNo)
  persistUsers()
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
  loadUsers()
  loadOffices()
})
</script>

<style scoped>
.utility-select {
  min-height: 50px;
  height: 50px;
  padding-top: 0.7rem;
  padding-bottom: 0.7rem;
  padding-left: 0.75rem;
  text-indent: 0;
}
</style>
