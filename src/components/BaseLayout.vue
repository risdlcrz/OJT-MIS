<template>
  <div
    class="app-shell hold-transition sidebar-mini layout-fixed layout-navbar-fixed"
    :class="{ 'sidebar-fullscreen-open': sidebarFullscreen, 'sidebar-collapsed': sidebarCollapsed, 'shell-hero-mode': isHeroPage }"
  >
    <header class="main-header navbar navbar-expand navbar-dark">
      <button
        type="button"
        class="shell-mobile-toggle d-md-none"
        aria-label="Open navigation menu"
        @click="toggleSidebar"
      >
        <i class="fas fa-bars"></i>
      </button>

      <span class="navbar-greeting">Hello {{ userName }}</span>
    </header>

    <aside class="main-sidebar sidebar-dark-primary elevation-4" :aria-hidden="sidebarFullscreen ? 'false' : undefined">
      <div class="brand-header">
        <a
          href="/dashboard.html"
          class="brand-link"
          aria-label="OJTMIS Dashboard"
          @click="navigate($event, 'dashboard.html')"
        >
          <img src="/assets/images/logo.png" alt="OJTMIS Logo" class="brand-logo" />
        </a>
        <button
          type="button"
          class="shell-menu-toggle"
          :aria-expanded="String(sidebarFullscreen || !sidebarCollapsed)"
          aria-label="Toggle navigation menu"
          title="Toggle sidebar"
          @click="toggleSidebar"
        >
          <i class="fas fa-bars"></i>
        </button>
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
              <i v-if="item.svgIcon" class="nav-icon" v-html="item.svgIcon"></i>
              <i v-else :class="['nav-icon', item.icon]"></i>
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

      <div class="sidebar-footer">
        <button type="button" class="sidebar-logout" title="Sign out" aria-label="Sign out" @click="logout">
          <i class="fas fa-sign-out-alt"></i>
        </button>
      </div>
    </aside>
    <button
      v-if="sidebarFullscreen"
      type="button"
      class="sidebar-backdrop"
      aria-label="Close navigation menu"
      @click="closeSidebar"
    ></button>
    <main class="content-wrapper shell-content">
      <section class="content shell-body">
        <slot />
      </section>
    </main>

    <footer class="main-footer text-sm">
      <strong>2026 &copy; OJTMIS</strong>
    </footer>

    <!-- Global Alert Modal -->
    <div
      id="globalAlertModal"
      class="modal fade"
      :class="{ show: isAlertOpen }"
      :style="{ display: isAlertOpen ? 'block' : 'none' }"
      tabindex="-1"
      role="dialog"
      :aria-hidden="!isAlertOpen"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
          <div class="modal-header modern-modal-header">
            <h5 class="modal-title font-weight-bold">{{ alertTitle }}</h5>
            <button
              type="button"
              class="close"
              aria-label="Close"
              @click="closeAlert"
            >
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body p-4">
            <p class="mb-0">{{ alertMessage }}</p>
          </div>
          <div class="modal-footer border-0 pt-0">
            <button
              type="button"
              class="btn btn-primary px-4 modern-btn"
              @click="closeAlert"
            >
              OK
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Global Confirm Modal -->
    <div
      id="globalConfirmModal"
      class="modal fade"
      :class="{ show: isConfirmOpen }"
      :style="{ display: isConfirmOpen ? 'block' : 'none' }"
      tabindex="-1"
      role="dialog"
      :aria-hidden="!isConfirmOpen"
    >
      <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content modern-modal">
          <div class="modal-header modern-modal-header">
            <h5 class="modal-title font-weight-bold">{{ confirmTitle }}</h5>
            <button
              type="button"
              class="close"
              aria-label="Close"
              @click="closeConfirm(false)"
            >
              <span aria-hidden="true">&times;</span>
            </button>
          </div>
          <div class="modal-body p-4">
            <p class="mb-0">{{ confirmMessage }}</p>
          </div>
          <div class="modal-footer border-0 pt-0">
            <button
              type="button"
              class="btn btn-outline-secondary px-4 modern-btn"
              @click="closeConfirm(false)"
            >
              Cancel
            </button>
            <button
              type="button"
              class="btn btn-primary px-4 modern-btn"
              @click="closeConfirm(true)"
            >
              OK
            </button>
          </div>
        </div>
      </div>
    </div>

  </div>
</template>

<script setup>
import { computed, onBeforeUnmount, onMounted, provide, ref, watch } from 'vue'
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
  'signatories.html',
  'account.html',
  'contact.html'
]

/* ---- Global Dialog state ---- */
const isAlertOpen = ref(false)
const isConfirmOpen = ref(false)
const alertTitle = ref('')
const alertMessage = ref('')
const confirmTitle = ref('')
const confirmMessage = ref('')

let alertResolve = null
let confirmResolve = null

function closeAlert() {
  isAlertOpen.value = false
  if (alertResolve) {
    alertResolve()
    alertResolve = null
  }
}

function closeConfirm(result) {
  isConfirmOpen.value = false
  if (confirmResolve) {
    confirmResolve(result)
    confirmResolve = null
  }
}

function showAlert(message, title = 'Notification') {
  alertTitle.value = title
  alertMessage.value = message
  isAlertOpen.value = true
  return new Promise((resolve) => {
    alertResolve = resolve
  })
}

function showConfirm(message, title = 'Confirm') {
  confirmTitle.value = title
  confirmMessage.value = message
  isConfirmOpen.value = true
  return new Promise((resolve) => {
    confirmResolve = resolve
  })
}

provide('dialog', {
  showAlert,
  showConfirm,
  isAlertOpen,
  isConfirmOpen,
  alertTitle,
  alertMessage,
  confirmTitle,
  confirmMessage
})

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
    'signatories.html': { title: 'Signatories', section: 'System Settings', sectionIcon: 'fas fa-cogs' }
  }[props.currentPage] || { title: 'OJTMIS' })
})

const menu = ref([])

/* Ang hero (dashboard) ay buong background ang ipinapakita. */
const isHeroPage = computed(() => props.currentPage === 'dashboard.html')

function logout() {
  window.history.pushState({}, '', '/login.html')
  emit('navigate', 'login.html')
}

/* Ang pangalan sa navbar greeting ay galing sa naka-login na session
   (Full Name), hindi na sa menu.json. */
const userName = computed(() => currentUser()?.fullName || 'User')

const openMenus = ref(new Set())
const sidebarFullscreen = ref(false)
const sidebarCollapsed = ref(false)
let previousBodyOverflow = ''

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
  if (event.key !== 'Escape') return

  closeSidebar()
}

function handleResize() {
  if (window.innerWidth < 768) sidebarCollapsed.value = false
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  window.addEventListener('resize', handleResize)
  window.addEventListener('hashchange', syncHash)
  window.addEventListener('popstate', syncHash)

  menu.value = menuConfig.menu || []

  openMenus.value = new Set(menu.value.filter(hasActiveChild).map((item) => item.name))

  /* Expose global dialog functions for non-Vue JS files */
  window.$dialog = {
    alert: showAlert,
    confirm: showConfirm
  }
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  window.removeEventListener('resize', handleResize)
  window.removeEventListener('hashchange', syncHash)
  window.removeEventListener('popstate', syncHash)
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
  background-color: #6b4a1c;
  background-image:
    linear-gradient(to right, rgba(120, 80, 20, 0.6), rgba(120, 30, 30, 0.7)),
    url('../../assets/images/login-bg.jpg');
  background-position: center;
  background-size: cover;
  background-repeat: no-repeat;
  background-attachment: fixed;
}

@media (min-width: 768px) {
  .app-shell > .main-header {
    width: calc(100% - 232px);
    margin-left: 232px !important;
  }

  .app-shell > .main-sidebar {
    top: 0;
    width: 232px;
  }

  .app-shell > .content-wrapper {
    margin-left: 232px !important;
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

  .sidebar-collapsed > .content-wrapper {
    margin-left: 65px !important;
  }

  .sidebar-collapsed .brand-link {
    padding: 0;
  }

  .sidebar-collapsed .brand-text,
  .sidebar-collapsed .nav-treeview {
    display: none !important;
  }

  .sidebar-collapsed .nav-sidebar .nav-link p {
    display: none !important;
  }

  .sidebar-collapsed .nav-sidebar .right {
    display: none !important;
  }

  .sidebar-collapsed .brand-header {
    justify-content: center;
    padding: 0 0.25rem;
  }

  .sidebar-collapsed .brand-link {
    display: none;
  }

  .sidebar-collapsed .shell-menu-toggle {
    width: 100%;
    padding: 0.7rem 0;
    font-size: 1.25rem;
  }

  .sidebar-collapsed .sidebar {
    padding: 0.8rem 0.35rem 1rem;
  }

  .sidebar-collapsed .nav-sidebar .nav-link {
    justify-content: center;
    width: 100%;
    min-height: 46px;
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
}

.main-header-logo,
.shell-menu-toggle,
.navbar-greeting {
  position: relative;
  z-index: 1040;
}

.main-sidebar {
  position: fixed;
  top: 0;
  bottom: 0;
  left: 0;
  width: 232px;
  z-index: 1030;
  display: flex;
  flex-direction: column;
  background: rgba(30, 15, 12, 0.82);
  -webkit-backdrop-filter: blur(12px);
  backdrop-filter: blur(12px);
  border-right: 1px solid rgba(255, 255, 255, 0.16);
  box-shadow: 8px 0 30px rgba(30, 15, 5, 0.18);
  overflow: hidden;
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
  justify-content: space-between;
  gap: 0.35rem;
  width: 100%;
  height: 64px;
  padding: 0 0.95rem;
  border-bottom: 1px solid rgba(255, 255, 255, 0.14);
  flex: 0 0 auto;
}

.brand-link {
  display: inline-flex;
  align-items: center;
  padding: 0;
  background: transparent;
  box-shadow: none;
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
  width: 38px;
  height: 38px;
  object-fit: contain;
  border-radius: 50%;
  background: #ffffff;
  padding: 2px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.28);
}

.shell-menu-toggle {
  border: 0;
  background: transparent;
  color: #ffffff;
  font-size: 1.15rem;
  line-height: 1;
  padding: 0.4rem 0.45rem;
  cursor: pointer;
  border-radius: 6px;
}

.shell-menu-toggle:hover,
.shell-menu-toggle:focus {
  background: rgba(255, 255, 255, 0.14);
  outline: none;
}

.shell-mobile-toggle {
  border: 0;
  background: transparent;
  color: #ffffff;
  font-size: 1.15rem;
  padding: 0.4rem 0.6rem;
  margin-right: 0.35rem;
  cursor: pointer;
}

/* ---------- Sidebar navigation (photo reference) ---------- */
.main-sidebar .sidebar {
  flex: 1 1 auto;
  max-height: none;
  padding: 0.35rem 0 0.5rem;
  overflow-y: auto;
}

.main-sidebar .nav-sidebar .nav-link {
  gap: 0.9rem;
  margin: 0 !important;
  padding: 0.85rem 1.1rem !important;
  border-radius: 0 !important;
  background: transparent !important;
  color: #ffffff !important;
  font-size: 0.78rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.main-sidebar .nav-sidebar .nav-link i.nav-icon {
  width: 22px;
  font-size: 1rem;
  margin: 0;
}

.main-sidebar .nav-sidebar .nav-link .right {
  font-size: 0.7rem;
  opacity: 0.8;
}

.main-sidebar .nav-sidebar .nav-link:hover {
  background: rgba(255, 255, 255, 0.1) !important;
  box-shadow: none !important;
}

.main-sidebar .nav-sidebar .nav-link.active,
.main-sidebar .nav-sidebar .nav-link.menu-open,
.main-sidebar .nav-sidebar .nav-link.active-parent {
  background: linear-gradient(90deg, #dd8117 0%, #f0a43a 100%) !important;
  color: #ffffff !important;
  box-shadow: inset 4px 0 0 #ffd79a !important;
}

.main-sidebar .nav-treeview {
  margin: 0;
  padding-left: 0;
  background: rgba(0, 0, 0, 0.22);
}

.main-sidebar .nav-treeview > .nav-item > .nav-link {
  margin: 0 !important;
  padding: 0.72rem 1.1rem 0.72rem 2.7rem !important;
  border-radius: 0 !important;
  background: transparent !important;
  color: #ffe9d2 !important;
  font-size: 0.7rem;
  font-weight: 700;
  letter-spacing: 0.06em;
  text-transform: uppercase;
}

.main-sidebar .nav-treeview > .nav-item > .nav-link:hover {
  background: rgba(0, 0, 0, 0.3) !important;
  color: #ffffff !important;
}

.main-sidebar .nav-treeview > .nav-item > .nav-link.active {
  background: rgba(0, 0, 0, 0.35) !important;
  color: #ffffff !important;
  box-shadow: none !important;
  padding: 0.72rem 1.1rem 0.72rem 2.7rem !important;
}

.sidebar-footer {
  flex: 0 0 auto;
  padding: 0.7rem 1.1rem;
  border-top: 1px solid rgba(255, 255, 255, 0.14);
}

.sidebar-logout {
  border: 0;
  background: transparent;
  color: #ffffff;
  font-size: 1.15rem;
  padding: 0.35rem;
  cursor: pointer;
  border-radius: 6px;
}

.sidebar-logout:hover,
.sidebar-logout:focus {
  background: rgba(255, 255, 255, 0.14);
  outline: none;
}

/* ---------- Top navigation (photo reference) ---------- */
.app-shell > .main-header.navbar {
  position: fixed;
  top: 0;
  right: 0;
  left: 0;
  height: 64px;
  padding: 0 1.25rem;
  display: flex;
  align-items: center;
  background: rgba(30, 15, 12, 0.82);
  -webkit-backdrop-filter: blur(12px);
  backdrop-filter: blur(12px);
  border: 0;
  box-shadow: 0 6px 18px rgba(30, 15, 12, 0.22);
  z-index: 1035;
}

/* Greeting text sa top-right ng navigation bar (walang dropdown). */
.app-shell .navbar-greeting {
  margin-left: auto;
  display: inline-flex;
  align-items: center;
  color: #ffffff;
  font-size: 0.95rem;
  font-weight: 700;
  letter-spacing: 0.02em;
  white-space: nowrap;
  user-select: none;
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

.app-shell > .content-wrapper {
  margin-left: 232px;
  min-height: 100vh;
  padding: 64px 0 0;
  background: transparent !important;
}

.shell-content {
  min-height: calc(100vh - 64px);
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
  color: #1E0F0C;
  font-weight: 600;
}

.modern-modal .form-control {
  width: 100%;
  border: 1px solid #dbe4f0;
  border-radius: 8px;
}

.modern-modal .form-control:focus {
  border-color: #ff8c1a;
  box-shadow: 0 0 0 0.2rem rgba(255, 140, 26, 0.18);
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
  min-height: calc(100vh - 64px);
  padding: 1.5rem;
  background: #fdfbf1;
  border-radius: 0;
  box-shadow: none;
  color: #1E0F0C;
}

/* Dashboard: buong background ang eksena, walang cream panel. */
.shell-hero-mode > .content-wrapper {
  padding: 64px 0 0;
}

.shell-hero-mode .shell-body {
  min-height: calc(100vh - 64px);
  padding: 0;
  background: transparent;
  border-radius: 0;
  box-shadow: none;
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
  display: none;
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
  color: #ff8c1a;
  font-size: 0.72rem;
  font-weight: 800;
  letter-spacing: 0.16em;
  text-transform: uppercase;
}

.shell-page-heading h1 {
  margin: 0;
  color: #1E0F0C;
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
    min-height: calc(100vh - 64px);
  }

  .app-shell > .main-header.navbar {
    width: 100%;
    margin-left: 0 !important;
    height: 60px;
    padding: 0 0.85rem;
  }

  .app-shell > .content-wrapper {
    margin-left: 0 !important;
    padding: 60px 0 0;
  }

  .shell-body {
    min-height: calc(100vh - 60px);
    padding: 1rem;
  }

  .app-shell .main-header-logo {
    display: inline-flex;
  }
}
</style>