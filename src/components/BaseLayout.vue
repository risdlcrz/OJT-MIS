<template>
  <div
    class="app-shell hold-transition sidebar-mini layout-fixed layout-navbar-fixed"
    :class="{ 'sidebar-fullscreen-open': sidebarFullscreen, 'sidebar-collapsed': sidebarCollapsed, 'shell-hero-mode': isHeroPage }"
  >
    <header class="main-header navbar navbar-expand navbar-dark">
      <button
        type="button"
        class="shell-mobile-toggle d-md-none"
        :class="{ 'is-active': sidebarFullscreen }"
        :aria-expanded="String(sidebarFullscreen)"
        aria-label="Open navigation menu"
        @click="toggleSidebar"
      >
        <span class="hamburger-bars" aria-hidden="true">
          <span></span>
          <span></span>
          <span></span>
        </span>
      </button>

      <span class="navbar-greeting">Hello {{ userName }}</span>

      <NotificationBell @open="navigateToLink" />
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
          :class="{ 'is-active': sidebarFullscreen || !sidebarCollapsed }"
          :aria-expanded="String(sidebarFullscreen || !sidebarCollapsed)"
          aria-label="Toggle navigation menu"
          title="Toggle sidebar"
          @click="toggleSidebar"
        >
          <span class="hamburger-bars" aria-hidden="true">
            <span></span>
            <span></span>
            <span></span>
          </span>
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

            <ul
              v-if="item.submenu"
              class="nav nav-treeview"
              :class="{ 'is-open': openMenus.has(item.name) }"
            >
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
import { computed, nextTick, onBeforeUnmount, onMounted, provide, ref, watch } from 'vue'
import '../../assets/css/navigation.css'
import '../../assets/css/Dashboard.css'
import '../../assets/css/layout.css'
import menuConfig from '../../assets/json/menu.json'
import internMenuConfig from '../../assets/json/intern-menu.json'
import supervisorMenuConfig from '../../assets/json/supervisor-menu.json'
import NotificationBell from './NotificationBell.vue'
import { clearSession, currentUser, isIntern, isSupervisor } from '../services/api'
import { ALL_PAGES } from '../utils/access'
import { go } from '../utils/navigation'

const props = defineProps({
  currentPage: {
    type: String,
    default: 'dashboard.html'
  }
})

/* Every page the router can render; anything else is a normal link. */
const ALLOWED_PAGES = ['login.html', ...ALL_PAGES]

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

/* The "#schedule" part (or any hash) of the URL, for example "/applicants.html#schedule".
   It tells whether List of Applicants or Schedule Orientation is open. */
const currentHash = ref(window.location.hash.toLowerCase())

const pageMeta = computed(() => {
  if (props.currentPage === 'applicants.html' && currentHash.value === '#schedule') {
    return { title: 'Schedule Orientation', section: 'Applicants', sectionIcon: 'far fa-file' }
  }

  return ({
    'dashboard.html': { title: 'Dashboard' },
    'requests.html': { title: 'List of Requests', section: 'Request', sectionIcon: 'fas fa-table' },
    'time-adjustments.html': { title: 'Time Adjustment Requests', section: 'Request', sectionIcon: 'fas fa-table' },
    'applicants.html': { title: 'List of Applicants', section: 'Applicants', sectionIcon: 'far fa-file' },
    'schools.html': { title: 'List of Schools', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
    'internlist.html': { title: 'Intern List' },
    'forms.html': { title: 'Issuance of COC', section: 'Forms', sectionIcon: 'fas fa-file' },
    'dtr.html': { title: 'Daily Time Record', section: 'Monitoring', sectionIcon: 'fas fa-users' },
    'coc-generate.html': { title: 'Certificate of Completion', section: 'Forms', sectionIcon: 'fas fa-file' },
    'documents-history.html': { title: 'Documents History', section: 'Forms', sectionIcon: 'fas fa-file' },
    'attendance.html': { title: 'Attendance Report', section: 'Monitoring', sectionIcon: 'fas fa-chart-bar' },
    'logs.html': { title: 'Activity Logs', section: 'Monitoring', sectionIcon: 'fas fa-users' },
    'idgenerate.html': { title: 'ID Generation', section: 'Forms', sectionIcon: 'fas fa-file' },
    'utility.html': { title: 'Utility' },
    'signatories.html': { title: 'Signatories', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
    'account.html': { title: 'Account Information', section: 'Account', sectionIcon: 'far fa-user-circle' },
    'intern-dtr.html': { title: 'Daily Time Record', section: 'Intern', sectionIcon: 'fas fa-id-card' },
    'intern-attendance.html': { title: 'Attendance', section: 'Intern', sectionIcon: 'fas fa-calendar-check' },
    'intern-documents.html': { title: 'Document Requests', section: 'Intern', sectionIcon: 'far fa-folder' }
  }[props.currentPage] || { title: 'OJTMIS' })
})

const menu = ref([])

/* The dashboard shows a full-page background. */
const isHeroPage = computed(() => props.currentPage === 'dashboard.html')

function logout() {
  clearSession()
  go('/login.html')
}

/* The name in the top-bar greeting comes from the signed-in session
   (Full Name), not from menu.json. */
const userProfile = ref(currentUser())
const userName = computed(() => userProfile.value?.fullName || 'User')

function handleProfileChanged() {
  userProfile.value = currentUser()
}

/* Opens a notification link such as "/coc-generate.html?internId=12",
   keeping its query string and hash intact. */
async function navigateToLink(link) {
  let url
  try {
    url = new URL(link, window.location.origin)
  } catch {
    return
  }

  const page = url.pathname.replace(/^\//, '').toLowerCase()
  if (!ALLOWED_PAGES.includes(page)) return

  await go(`${url.pathname}${url.search}${url.hash}`)

  syncHash()

  const internId = Number(url.searchParams.get('internId'))
  const hasIntern = Number.isFinite(internId) && internId > 0
  if (hasIntern) {
    if (page === 'coc-generate.html') {
      window.dispatchEvent(new CustomEvent('ojt:certificate-intern', { detail: { internId } }))
    } else if (page === 'internlist.html') {
      window.dispatchEvent(new CustomEvent('ojt:intern-profile', { detail: { internId } }))
    } else if (page === 'idgenerate.html') {
      window.dispatchEvent(new CustomEvent('ojt:id-intern', { detail: { internId } }))
    }
  }

  /* Router navigation does not fire hashchange, so tell the target page
     (coc-generate.vue) to refresh its route parameters. */
  window.dispatchEvent(new HashChangeEvent('hashchange'))

  if (sidebarFullscreen.value) closeSidebar()
}

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

/* If the "Schedule Orientation" link in menu.json is not a known page,
   send it to the Applicants page, Schedule tab. It is still better to
   set the link in menu.json to "applicants.html#schedule". */
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
  const opening = !next.has(item.name)
  if (opening) next.add(item.name)
  else next.delete(item.name)
  openMenus.value = next

  if (!opening) return
  nextTick(() => {
    const reveal = () => {
      document.querySelector('.main-sidebar .nav-treeview.is-open')
        ?.lastElementChild
        ?.scrollIntoView({ block: 'nearest' })
    }
    reveal()
    window.setTimeout(reveal, 320)
  })
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

async function navigate(event, link) {
  const { page, hash } = splitLink(link)

  if (!ALLOWED_PAGES.includes(page)) return

  event.preventDefault()

  await go(`/${page}${hash}`)

  syncHash()

  /* Router navigation does not fire hashchange. Tell the page
     (applicants.vue) so it switches tab even when it is already open. */
  window.dispatchEvent(new HashChangeEvent('hashchange'))

  if (sidebarFullscreen.value) closeSidebar()
}

/* Escape closes the full-screen sidebar on phones (the bell handles its own list). */
function handleKeydown(event) {
  if (event.key === 'Escape') closeSidebar()
}

function handleResize() {
  if (window.innerWidth < 768) sidebarCollapsed.value = false
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  window.addEventListener('resize', handleResize)
  window.addEventListener('hashchange', syncHash)
  window.addEventListener('popstate', syncHash)
  window.addEventListener('ojt:profile-changed', handleProfileChanged)

  menu.value = isIntern()
    ? (internMenuConfig.menu || [])
    : isSupervisor()
      ? (supervisorMenuConfig.menu || [])
      : (menuConfig.menu || [])

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
  window.removeEventListener('ojt:profile-changed', handleProfileChanged)
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
