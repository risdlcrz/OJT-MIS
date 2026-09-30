<template>
  <div
    class="app-shell hold-transition sidebar-mini layout-fixed layout-navbar-fixed"
    :class="{ 'sidebar-fullscreen-open': sidebarFullscreen, 'sidebar-collapsed': sidebarCollapsed }"
  >
    <header class="main-header navbar navbar-expand navbar-orange navbar-dark elevation-2">
      <a href="/dashboard.html" class="main-header-logo" aria-label="OJTMIS Dashboard" @click="navigate($event, 'dashboard.html')">
        <img src="/assets/images/logo.png" alt="OJTMIS Logo" />
        <span>OJTMIS</span>
      </a>

      <button
        type="button"
        class="nav-link shell-menu-toggle"
        :aria-expanded="String(sidebarFullscreen || !sidebarCollapsed)"
        :aria-label="sidebarFullscreen || sidebarCollapsed ? 'Expand navigation menu' : 'Collapse navigation menu'"
        title="Toggle sidebar"
        @click="toggleSidebar"
      >
        <i class="fas fa-bars"></i>
      </button>

      <div class="navbar-user dropdown" :class="{ show: isUserMenuOpen }">
        <button
          type="button"
          class="nav-link user-name-btn"
          title="Click to change your display name"
          @click="openEditName"
        >
          <i class="fas fa-user mr-1"></i>{{ user.name }}
          <span v-if="roleLabel" class="badge text-bg-secondary ms-2">{{ roleLabel }}</span>
        </button>
        <button
          type="button"
          class="nav-link user-caret-btn"
          :aria-expanded="String(isUserMenuOpen)"
          aria-label="Open user menu"
          @click="toggleUserMenu"
        >
          <i class="fas fa-caret-down"></i>
        </button>
        <div v-if="user.menu?.length" class="dropdown-menu dropdown-menu-right" :class="{ show: isUserMenuOpen }">
          <a
            v-for="item in user.menu"
            :key="item.name"
            :href="item.link"
            class="dropdown-item"
            @click="onUserMenuItemClick($event, item)"
          >
            <i :class="[item.icon, 'mr-2']"></i>{{ item.name }}
          </a>
        </div>
      </div>
    </header>

    <!-- ================= EDIT DISPLAY NAME MODAL ================= -->
    <div
      id="editNameModal"
      class="modal fade"
      :class="{ show: isEditNameOpen }"
      :style="{ display: isEditNameOpen ? 'block' : 'none' }"
      tabindex="-1"
      role="dialog"
      :aria-hidden="!isEditNameOpen"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
          <div class="modal-header modern-modal-header">
            <h5 class="modal-title font-weight-bold">Change Display Name</h5>
            <button type="button" class="close" aria-label="Close" @click="closeEditName">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>

          <form @submit.prevent="submitEditName">
            <div class="modal-body p-4">
              <div v-if="editNameError" class="alert alert-danger py-2">
                {{ editNameError }}
              </div>

              <div class="form-group mb-0">
                <label for="editNameInput">Display Name</label>
                <input
                  id="editNameInput"
                  v-model="editNameValue"
                  type="text"
                  class="form-control"
                  maxlength="40"
                  required
                  autofocus
                />
              </div>
            </div>

            <div class="modal-footer border-0 pt-0">
              <button type="submit" class="btn btn-success px-4 modern-btn">
                <i class="fas fa-save mr-1"></i>
                Save
              </button>
              <button type="button" class="btn btn-outline-secondary px-4" @click="closeEditName">
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="isEditNameOpen"
      class="modal-backdrop fade show"
      @click="closeEditName"
    ></div>

    <!-- ================= CHANGE PASSWORD MODAL ================= -->
    <div
      id="changePasswordModal"
      class="modal fade"
      :class="{ show: isChangePasswordOpen }"
      :style="{ display: isChangePasswordOpen ? 'block' : 'none' }"
      tabindex="-1"
      role="dialog"
      :aria-hidden="!isChangePasswordOpen"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
          <div class="modal-header modern-modal-header">
            <h5 class="modal-title font-weight-bold">Change Password</h5>
            <button type="button" class="close" aria-label="Close" @click="closeChangePassword">
              <span aria-hidden="true">&times;</span>
            </button>
          </div>

          <form @submit.prevent="submitChangePassword">
            <div class="modal-body p-4">
              <div v-if="changePasswordError" class="alert alert-danger py-2">
                {{ changePasswordError }}
              </div>

              <div class="form-group">
                <label for="currentPassword">Current Password</label>
                <input
                  id="currentPassword"
                  v-model="currentPassword"
                  type="password"
                  class="form-control"
                  autocomplete="current-password"
                  required
                />
              </div>

              <div class="form-group">
                <label for="newPassword">New Password</label>
                <input
                  id="newPassword"
                  v-model="newPassword"
                  type="password"
                  class="form-control"
                  autocomplete="new-password"
                  minlength="6"
                  required
                />
              </div>

              <div class="form-group mb-0">
                <label for="confirmPassword">Confirm New Password</label>
                <input
                  id="confirmPassword"
                  v-model="confirmPassword"
                  type="password"
                  class="form-control"
                  autocomplete="new-password"
                  minlength="6"
                  required
                />
              </div>
            </div>

            <div class="modal-footer border-0 pt-0">
              <button type="submit" class="btn btn-success px-4 modern-btn">
                <i class="fas fa-key mr-1"></i>
                Update Password
              </button>
              <button type="button" class="btn btn-outline-secondary px-4" @click="closeChangePassword">
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>

    <div
      v-if="isChangePasswordOpen"
      class="modal-backdrop fade show"
      @click="closeChangePassword"
    ></div>

    <aside class="main-sidebar sidebar-dark-primary elevation-4" :aria-hidden="sidebarFullscreen ? 'false' : undefined">
      <div class="brand-header">
        <a href="/" class="brand-link text-center bg-orange">
          <img src="/assets/images/logo.png" alt="OJTMIS Logo" class="brand-logo" />
          <span class="brand-text font-weight-bold text-light">OJTMIS</span>
        </a>
      </div>

      <nav class="sidebar mt-3" aria-label="Main navigation">
        <ul class="nav nav-pills nav-sidebar flex-column">
          <li v-for="item in menu" :key="item.name" class="nav-item" :class="{ 'menu-open': openMenus.has(item.name) }">
            <a
              v-if="item.submenu"
              href="#"
              class="nav-link"
              :class="{ active: hasActiveChild(item) }"
              :aria-expanded="String(openMenus.has(item.name))"
              :title="sidebarCollapsed ? item.name : undefined"
              @click.prevent="toggleMenu(item)"
            >
              <i :class="['nav-icon', item.icon]"></i>
              <p>{{ item.name }} <i class="right fas fa-angle-left"></i></p>
            </a>
            <a
              v-else
              :href="item.link"
              class="nav-link"
              :class="{ active: isActive(item.link) }"
              :title="sidebarCollapsed ? item.name : undefined"
              @click="navigate($event, item.link)"
            >
              <i :class="['nav-icon', item.icon]"></i>
              <p>{{ item.name }}</p>
            </a>

            <ul v-if="item.submenu && openMenus.has(item.name)" class="nav nav-treeview">
              <li v-for="child in item.submenu" :key="child.name" class="nav-item">
                <a
                  :href="resolveLink(child)"
                  class="nav-link"
                  :class="{ active: isActive(resolveLink(child)) }"
                  @click="navigate($event, resolveLink(child))"
                >
                  <p>{{ child.name }}</p>
                </a>
              </li>
            </ul>
          </li>
        </ul>
      </nav>
    </aside>
    <button
      v-if="sidebarFullscreen"
      type="button"
      class="sidebar-backdrop"
      aria-label="Close navigation menu"
      @click="closeSidebar"
    ></button>
    <main class="content-wrapper shell-content">
      <section class="content-header">
        <div class="container-fluid">
          <ol class="breadcrumb text-orange font-weight-bold">
            <li class="breadcrumb-item">
              <a href="/dashboard.html" class="text-orange font-weight-light" @click="navigate($event, 'dashboard.html')">
                <i class="fas fa-home"></i> Dashboard
              </a>
            </li>
            <li v-if="pageMeta.section" class="breadcrumb-item">
              <a class="text-orange font-weight-light"><i :class="pageMeta.sectionIcon"></i> {{ pageMeta.section }}</a>
            </li>
            <li v-if="pageMeta.title !== 'Dashboard'" class="breadcrumb-item active">
              <a :href="`/${currentPage}${currentHash}`" class="text-orange">{{ pageMeta.title }}</a>
            </li>
          </ol>
        </div>
      </section>

      <section class="content shell-body">
        <slot />
      </section>
    </main>

    <footer class="main-footer text-sm">
      <strong>2026 &copy; OJTMIS</strong>
    </footer>
  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, ref, watch } from 'vue'
import '../../assets/css/navigation.css'
import '../../assets/css/Dashboard.css'
import menuConfig from '../../assets/json/menu.json'
import { currentUser } from '../services/api'

const props = defineProps({
  currentPage: {
    type: String,
    default: 'dashboard.html'
  }
})

const emit = defineEmits(['navigate'])

/* Mga page na hawak ng App.vue. Ang iba ay normal na link. */
const ALLOWED_PAGES = [
  'login.html',
  'dashboard.html',
  'requests.html',
  'applicants.html',
  'schools.html',
  'internlist.html',
  'forms.html',
  'dtr.html',
  'idgenerate.html',
  'utility.html',
  'programs.html',
  'signatories.html'
]

/* Ang "#schedule" (o anumang hash) sa URL, halimbawa "/applicants.html#schedule".
   Ito ang nagsasabi kung List of Applicants o Schedule Orientation ang bukas. */
const currentHash = ref(window.location.hash.toLowerCase())

const pageMeta = computed(() => {
  if (props.currentPage === 'applicants.html' && currentHash.value === '#schedule') {
    return { title: 'Schedule Orientation', section: 'Applicants', sectionIcon: 'far fa-file' }
  }

  return ({
    'dashboard.html': { title: 'Dashboard' },
    'requests.html': { title: 'List of Requests', section: 'Request', sectionIcon: 'fas fa-table' },
    'applicants.html': { title: 'List of Applicants', section: 'Applicants', sectionIcon: 'far fa-file' },
    'schools.html': { title: 'List of Schools', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
    'internlist.html': { title: 'Intern List' },
    'forms.html': { title: 'Issuance of COC', section: 'Forms', sectionIcon: 'fas fa-file' },
    'dtr.html': { title: 'Daily Time Records', section: 'Forms', sectionIcon: 'fas fa-file' },
    'idgenerate.html': { title: 'ID Generation', section: 'Forms', sectionIcon: 'fas fa-file' },
    'utility.html': { title: 'Utility' },
    'programs.html': { title: 'Programs / Strands', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
    'signatories.html': { title: 'Signatories', section: 'System Settings', sectionIcon: 'fas fa-cogs' }
  }[props.currentPage] || { title: 'OJTMIS' })
})

const menu = ref([])
const user = ref({ name: 'User', menu: [] })

/* Ang pangalan sa navbar ay galing sa naka-login na session
   (First + Last), hindi na sa menu.json. */
const ROLE_LABELS = { HRAdmin: 'HR Admin', Supervisor: 'Supervisor', Intern: 'Intern' }

const sessionProfile = currentUser()

const roleLabel = computed(() => {
  const role = sessionProfile?.roles?.[0]
  return role ? ROLE_LABELS[role] || role : ''
})
const isUserMenuOpen = ref(false)
const openMenus = ref(new Set())
const sidebarFullscreen = ref(false)
const sidebarCollapsed = ref(false)
let previousBodyOverflow = ''

/* ---- Edit Display Name state ---- */
const DISPLAY_NAME_KEY = 'ojtmis_display_name'

const isEditNameOpen = ref(false)
const editNameValue = ref('')
const editNameError = ref('')

function openEditName() {
  editNameValue.value = user.value.name
  editNameError.value = ''
  isEditNameOpen.value = true
}

function closeEditName() {
  isEditNameOpen.value = false
  editNameError.value = ''
}

function submitEditName() {
  const trimmed = editNameValue.value.trim()

  if (!trimmed) {
    editNameError.value = 'Please enter a name.'
    return
  }

  user.value = { ...user.value, name: trimmed }
  localStorage.setItem(DISPLAY_NAME_KEY, trimmed)

  closeEditName()
}

/* ---- Change Password state ---- */

const CURRENT_PASSWORD_KEY = 'ojtmis_password'
const DEFAULT_PASSWORD = 'password123'

const isChangePasswordOpen = ref(false)
const currentPassword = ref('')
const newPassword = ref('')
const confirmPassword = ref('')
const changePasswordError = ref('')

function getStoredPassword() {
  return localStorage.getItem(CURRENT_PASSWORD_KEY) || DEFAULT_PASSWORD
}

function openChangePassword() {
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  changePasswordError.value = ''
  isChangePasswordOpen.value = true
}

function closeChangePassword() {
  isChangePasswordOpen.value = false
  currentPassword.value = ''
  newPassword.value = ''
  confirmPassword.value = ''
  changePasswordError.value = ''
}

function submitChangePassword() {
  changePasswordError.value = ''

  if (currentPassword.value !== getStoredPassword()) {
    changePasswordError.value = 'Current password is incorrect.'
    return
  }

  if (newPassword.value.length < 6) {
    changePasswordError.value = 'New password must be at least 6 characters.'
    return
  }

  if (newPassword.value !== confirmPassword.value) {
    changePasswordError.value = 'New password and confirmation do not match.'
    return
  }

  localStorage.setItem(CURRENT_PASSWORD_KEY, newPassword.value)

  closeChangePassword()

  alert('Password updated successfully.')
}

/* "Change Password" ang dropdown item na walang sariling page, kaya binubuksan
   ang modal sa halip na sundan ang href. Ang ibang item (e.g. Logout) ay
   sumusunod pa rin sa normal na link nito. */
function onUserMenuItemClick(event, item) {
  const name = (item.name || '').trim().toLowerCase()

  if (name === 'change password') {
    event.preventDefault()
    closeUserMenu()
    openChangePassword()
    return
  }

  if (name === 'logout') {
    // Huwag na ang full page reload: basta na ang SPA navigation para
    // malinis ang session at hindi ma-trigger ang auto-redirect ng login.
    event.preventDefault()
    closeUserMenu()
    navigate(event, 'login.html')
    return
  }

  closeUserMenu()
}

/* "applicants.html#schedule" -> { page: 'applicants.html', hash: '#schedule' } */
function splitLink(link) {
  const [page = '', hash = ''] = (link || '')
    .toLowerCase()
    .replace(/^\//, '')
    .split('#')

  return { page, hash: hash ? `#${hash}` : '' }
}

/* Kung hindi kilalang page ang link ng "Schedule Orientation" sa menu.json,
   idiretso ito sa Applicants page, Schedule tab. Mas maganda pa ring
   gawing "applicants.html#schedule" ang link sa menu.json. */
function resolveLink(item) {
  const { page } = splitLink(item.link)

  if (ALLOWED_PAGES.includes(page)) return item.link

  if ((item.name || '').trim().toLowerCase() === 'schedule orientation') {
    return 'applicants.html#schedule'
  }

  return item.link
}

function syncHash() {
  currentHash.value = window.location.hash.toLowerCase()
}

function isActive(link) {
  const { page, hash } = splitLink(link)

  if (!page || props.currentPage !== page) return false

  return hash === currentHash.value
}

function hasActiveChild(item) {
  return (item.submenu || []).some((child) => isActive(resolveLink(child)))
}

function toggleMenu(item) {
  if (sidebarCollapsed.value && window.innerWidth >= 768) {
    sidebarCollapsed.value = false
  }
  const next = new Set(openMenus.value)
  if (next.has(item.name)) next.delete(item.name)
  else next.add(item.name)
  openMenus.value = next
}

function toggleSidebar() {
  if (window.innerWidth < 768) {
    sidebarFullscreen.value = !sidebarFullscreen.value
    return
  }
  sidebarCollapsed.value = !sidebarCollapsed.value
}

function toggleUserMenu() {
  isUserMenuOpen.value = !isUserMenuOpen.value
}

function closeUserMenu() {
  isUserMenuOpen.value = false
}

function closeSidebar() {
  sidebarFullscreen.value = false
}

function navigate(event, link) {
  const { page, hash } = splitLink(link)

  if (!ALLOWED_PAGES.includes(page)) return

  event.preventDefault()

  window.history.pushState({}, '', `/${page}${hash}`)

  syncHash()
  emit('navigate', page)

  /* pushState hindi nagti-trigger ng hashchange. Ipaalam sa page
     (applicants.vue) para lumipat ito ng tab kahit nasa parehong page na. */
  window.dispatchEvent(new HashChangeEvent('hashchange'))

  if (sidebarFullscreen.value) closeSidebar()
}

function handleKeydown(event) {
  if (event.key === 'Escape') closeSidebar()
}

function handleResize() {
  if (window.innerWidth < 768) sidebarCollapsed.value = false
}

function handleDocumentClick(event) {
  const userDropdown = document.querySelector('.navbar-user')
  if (!userDropdown || userDropdown.contains(event.target)) return
  closeUserMenu()
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  window.addEventListener('resize', handleResize)
  window.addEventListener('hashchange', syncHash)
  window.addEventListener('popstate', syncHash)
  document.addEventListener('click', handleDocumentClick)

  menu.value = menuConfig.menu || []
  user.value = {
    ...menuConfig.navbar?.user,
    ...sessionProfile,
    name: sessionProfile?.fullName || menuConfig.navbar?.user?.name || 'User'
  }

  /* Ang pangalang binago via "Change Display Name" ay naka-save sa
     localStorage, kaya nananatili ito kahit mag-refresh o bumalik. */
  const savedName = localStorage.getItem(DISPLAY_NAME_KEY)
  if (savedName) {
    user.value = { ...user.value, name: savedName }
  }

  openMenus.value = new Set(menu.value.filter(hasActiveChild).map((item) => item.name))
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  window.removeEventListener('resize', handleResize)
  window.removeEventListener('hashchange', syncHash)
  window.removeEventListener('popstate', syncHash)
  document.removeEventListener('click', handleDocumentClick)
  document.body.style.overflow = previousBodyOverflow
})

watch(sidebarFullscreen, (isOpen) => {
  if (isOpen) {
    previousBodyOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
  } else {
    document.body.style.overflow = previousBodyOverflow
  }
})
</script>

<style>
@import url('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css');

:root {
  font-family: 'Segoe UI', sans-serif;
}

body {
  margin: 0;
  min-width: 320px;
}

.app-shell {
  min-height: 100vh;
  background-color: #f4f7fb;
  background-image: linear-gradient(rgba(6, 64, 43, 0.035) 1px, transparent 1px), linear-gradient(90deg, rgba(6, 64, 43, 0.035) 1px, transparent 1px);
  background-size: 28px 28px;
}

@media (min-width: 768px) {
  .app-shell > .main-header {
    width: calc(100% - 190px);
    margin-left: 190px !important;
  }

  .app-shell > .main-sidebar {
    top: 0;
    width: 190px;
    background: #000;
  }

  .app-shell > .content-wrapper,
  .app-shell > .main-footer {
    margin-left: 190px !important;
  }

  .app-shell .main-header-logo {
    display: none;
  }

  .sidebar-collapsed > .main-header {
    width: calc(100% - 65px);
    margin-left: 65px !important;
  }

  .sidebar-collapsed > .main-sidebar {
    width: 65px;
  }

  .sidebar-collapsed > .content-wrapper,
  .sidebar-collapsed > .main-footer {
    margin-left: 65px !important;
  }

  .sidebar-collapsed .brand-link {
    padding: 0.7rem 0.45rem;
  }

  .sidebar-collapsed .brand-text,
  .sidebar-collapsed .nav-sidebar p,
  .sidebar-collapsed .nav-treeview {
    display: none !important;
  }

  .sidebar-collapsed .nav-sidebar .right {
    display: none !important;
  }

  .sidebar-collapsed .sidebar {
    padding: 0.8rem 0.35rem 1rem;
  }

  .sidebar-collapsed .nav-sidebar .nav-link {
    justify-content: center;
    width: 100%;
    min-height: 42px;
    padding: 0.72rem 0.4rem;
  }

  .sidebar-collapsed .nav-sidebar .nav-link i.nav-icon {
    display: inline-block;
    width: 22px;
    font-size: 1rem;
    margin: 0;
    visibility: visible;
    opacity: 1;
  }

  .sidebar-collapsed .nav-sidebar .nav-item {
    width: 100%;
  }

  .sidebar-collapsed .nav-sidebar .nav-link.active {
    box-shadow: 0 8px 16px rgba(255, 140, 26, 0.24);
  }
}

.main-header-logo,
.shell-menu-toggle,
.navbar-user {
  position: relative;
  z-index: 1040;
}

.main-sidebar {
  position: fixed;
  top: 57px;
  bottom: 0;
  left: 0;
  width: 190px;
  z-index: 1030;
  transition: transform 0.28s ease, width 0.28s ease, top 0.28s ease;
}

.sidebar-fullscreen-open .main-sidebar {
  top: 0;
  width: min(280px, calc(100vw - 2rem));
  height: 100vh;
  transform: translateX(0);
  z-index: 1050;
}

.sidebar-backdrop {
  display: none;
}

.brand-header {
  display: flex;
  align-items: center;
  width: 100%;
  gap: 0.35rem;
}

.brand-link {
  flex: 1 1 auto;
}

.sidebar-close {
  display: none;
  position: static;
  width: 2.4rem;
  height: 2.4rem;
  border: 0;
  border-radius: 0.5rem;
  background: transparent;
  color: rgba(255, 255, 255, 0.92);
  cursor: pointer;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 1.5rem;
  line-height: 1;
  box-shadow: none;
  flex-shrink: 0;
  margin-left: auto;
}

.sidebar-close:hover,
.sidebar-close:focus {
  background: rgba(255, 255, 255, 0.08);
  outline: none;
}

.sidebar-fullscreen-open .sidebar-close {
  display: flex;
}

.main-header-logo {
  display: inline-flex;
  align-items: center;
  gap: 0.55rem;
  margin-right: 0.75rem;
  color: #fff;
  font-size: 1.05rem;
  font-weight: 800;
  letter-spacing: 0.08em;
  text-decoration: none;
  text-transform: uppercase;
}

.main-header-logo img,
.brand-link .brand-logo {
  width: 34px;
  height: 34px;
  object-fit: contain;
  border-radius: 10px;
  background: rgba(255, 255, 255, 0.1);
  padding: 4px;
}

.main-header-logo:hover,
.main-header-logo:focus {
  color: #fff;
  text-decoration: none;
}

.shell-menu-toggle {
  border: 0;
  background: transparent;
  cursor: pointer;
}

@media (min-width: 768px) {
  .main-sidebar,
  .main-header,
  .shell-content,
  .main-footer,
  .content-wrapper {
    transition: margin-left 0.2s ease-in-out, transform 0.2s ease-in-out;
  }
}

@media (max-width: 767.98px) {
  .app-shell > .main-header {
    width: 100%;
    margin-left: 0 !important;
  }

  .main-sidebar {
    position: fixed !important;
    top: 0;
    right: auto;
    margin-left: 0 !important;
    width: min(280px, calc(100vw - 2rem));
    height: 100vh;
    transform: translateX(-100%);
  }

  .sidebar-fullscreen-open .main-sidebar {
    transform: translateX(0);
  }

  .sidebar-fullscreen-open .sidebar-backdrop {
    display: block;
    position: fixed;
    inset: 0;
    width: 100%;
    height: 100%;
    padding: 0;
    border: 0;
    background: rgba(15, 23, 42, 0.48);
    cursor: pointer;
    z-index: 1040;
  }
}

.navbar-user {
  margin-left: auto;
  position: relative;
  display: flex;
  align-items: center;
}

.navbar-user .nav-link {
  color: #fff !important;
  background: #ff8c1a;
}

.navbar-user .nav-link:hover,
.navbar-user .nav-link:focus {
  color: #fff !important;
  background: #e67300;
}

.navbar-user .user-name-btn {
  border-radius: 999px 0 0 999px;
  padding: 0.45rem 0.75rem 0.45rem 0.9rem;
}

.navbar-user .user-caret-btn {
  border-radius: 0 999px 999px 0;
  padding: 0.45rem 0.85rem 0.45rem 0.5rem;
  border-left: 1px solid rgba(255, 255, 255, 0.35);
}

.navbar-user .dropdown-menu {
  margin-top: 0.5rem;
}

.navbar-user .dropdown-menu.show {
  display: block;
}

.shell-content {
  min-height: calc(100vh - 57px - 58px);
}

#changePasswordModal {
  z-index: 1060;
  overflow-x: hidden;
  overflow-y: auto;
}

#changePasswordModal .modal-dialog {
  margin: 1.75rem auto;
}

#changePasswordModal ~ .modal-backdrop {
  z-index: 1055;
}

.modern-modal {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  border: 0;
  border-radius: 18px;
  box-shadow: 0 20px 60px rgba(15, 23, 42, 0.2);
}

.modern-modal-header {
  background: #ff8c1a;
}

.modern-modal-header .modal-title,
.modern-modal-header .close {
  color: #fff;
}

.modern-modal label {
  display: block;
  margin-bottom: 0.45rem;
  color: #334155;
  font-weight: 600;
}

.modern-modal .form-control {
  width: 100%;
  border: 1px solid #dbe4f0;
  border-radius: 8px;
}

.modern-modal .form-control:focus {
  border-color: #0f766e;
  box-shadow: 0 0 0 0.2rem rgba(15, 118, 110, 0.12);
}

.modern-modal .modal-footer {
  padding: 1rem 1.2rem 1.2rem;
}

.modern-modal .modal-footer .btn {
  border-radius: 10px;
}

.modern-btn {
  padding: 0.55rem 1rem;
  border-radius: 999px;
  font-weight: 600;
  box-shadow: 0 10px 20px rgba(37, 99, 235, 0.16);
}

.shell-body {
  padding-bottom: 2rem;
}

.applicant-modal-dialog {
  position: relative;
  height: calc(100vh - 1rem);
  max-height: calc(100vh - 1rem);
  margin: 0.5rem auto;
}

.applicant-modal-dialog .modal-content {
  display: flex;
  flex-direction: column;
  height: 100%;
  max-height: calc(100vh - 1rem);
}

.applicant-modal-dialog form {
  flex: 1 1 auto;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.applicant-modal-dialog .modal-body {
  flex: 1 1 auto;
  height: 0;
  min-height: 0;
  overflow-y: auto;
  overscroll-behavior: contain;
  -webkit-overflow-scrolling: touch;
}

@media (max-width: 575.98px) {
  .applicant-modal-dialog {
    height: calc(100vh - 0.5rem);
    max-height: calc(100vh - 0.5rem);
    margin: 0.25rem;
  }

  .applicant-modal-dialog .modal-content {
    max-height: calc(100vh - 0.5rem);
  }
}

.modal.show .modal-dialog {
  animation: modal-pop-in 0.24s ease-out both;
}

@keyframes modal-pop-in {
  from {
    opacity: 0;
    transform: translateY(18px) scale(0.97);
  }
  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@media (prefers-reduced-motion: reduce) {
  .modal.show .modal-dialog {
    animation: none;
  }
}

.app-shell > .main-footer {
  margin-left: 190px;
  margin-bottom: 0;
}

.shell-page-heading {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  gap: 1rem;
  padding: 0.75rem 0 1.25rem;
}

.shell-eyebrow {
  display: block;
  margin-bottom: 0.35rem;
  color: #0f766e;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.shell-page-heading h1 {
  margin: 0;
  color: #102a26;
  font-size: clamp(1.75rem, 3vw, 2.7rem);
  font-weight: 800;
  letter-spacing: -0.03em;
}

.shell-page-heading p {
  margin: 0.35rem 0 0;
  color: #64748b;
  font-size: 0.98rem;
}

.shell-status {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.5rem 0.75rem;
  color: #166534;
  background: #dcfce7;
  border: 1px solid #bbf7d0;
  border-radius: 999px;
  font-size: 0.78rem;
  font-weight: 700;
  white-space: nowrap;
}

.shell-status i {
  font-size: 0.5rem;
}

@media (max-width: 575px) {
  .main-header-logo span {
    display: none;
  }

  .shell-page-heading {
    align-items: flex-start;
    flex-direction: column;
  }

  .shell-status {
    align-self: flex-start;
  }
}

@media (max-width: 767.98px) {
  .shell-content {
    min-height: calc(100vh - 57px - 70px);
  }

  .app-shell > .main-footer {
    margin-left: 0;
  }

  .app-shell .main-header-logo {
    display: inline-flex;
  }
}
</style>