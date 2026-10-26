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

      <div ref="notificationRoot" class="navbar-notifications">
        <button
          type="button"
          class="notification-toggle"
          :aria-expanded="String(notificationsOpen)"
          aria-haspopup="true"
          :aria-label="unreadCount > 0 ? `Notifications (${unreadCount} unread)` : 'Notifications'"
          @click="toggleNotifications"
        >
          <svg xmlns="http://www.w3.org/2000/svg" width="1em" height="1em" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none"/><g fill="none"><path fill="currentColor" fill-rule="evenodd" d="M19.6 18.57c.48 0 .73 0 .93-.04a2 2 0 0 0 1.55-1.55c.04-.2.04-.45.04-.94v-.38a2 2 0 0 0-.62-1.28l-.34-.39c-.46-.53-.7-.79-.82-1.1l-.03-.08c-.11-.33-.11-.68-.11-1.38V7.84h-3.3l-1.81-2.4l1.18-2.8s.25-.01-.89-.45C14.02 1.66 12 1.77 12 1.77h-.25a8 8 0 0 0-7.89 7.89v2.66a2 2 0 0 1-.39 1l-.26.3l-.69.76l-.25.29a2 2 0 0 0-.38 1l-.01.39c0 .47 0 .71.04.9a2 2 0 0 0 1.56 1.57c.2.04.44.04.91.04z" clip-rule="evenodd" opacity=".2"/><path stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" d="M20.2 11.3v.14a4 4 0 0 0 .15 1.45c.13.32.36.58.82 1.1l.34.4q.18.19.24.29a2 2 0 0 1 .38.98v.39c0 .48 0 .72-.04.93a2 2 0 0 1-1.55 1.55c-.2.04-.44.04-.93.04H4.37c-.47 0-.7 0-.9-.04a2 2 0 0 1-1.56-1.57c-.04-.2-.04-.43-.04-.9v-.38a2 2 0 0 1 .64-1.3l1.33-1.47l.01-.02V9.76c0-4.4 3.57-7.97 7.97-7.97l.79.05M7.66 18.57c0 2.05 1.94 3.7 4.34 3.7s4.34-1.65 4.34-3.7"/><circle cx="18.32" cy="4.84" r="3.08" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1.5" transform="rotate(-90 18.32 4.84)"/></g></svg>
          <span v-if="unreadCount > 0" class="notification-badge" aria-hidden="true">{{ unreadBadge }}</span>
        </button>

        <div v-if="notificationsOpen" class="notification-dropdown" role="dialog" aria-label="Notifications">
          <div class="notification-dropdown-header">
            <strong>Notifications</strong>
            <button
              type="button"
              class="notification-link"
              :disabled="unreadCount === 0"
              @click="markAllRead"
            >
              Mark all read
            </button>
          </div>

          <div v-if="notifications.length === 0" class="notification-empty">
            No notifications yet.
          </div>

          <ul v-else class="notification-list">
            <li
              v-for="item in notifications"
              :key="item.id"
              class="notification-item"
              :class="{ unread: !item.isRead }"
            >
              <button type="button" class="notification-item-button" @click="markRead(item)">
                <span class="notification-item-head">
                  <span class="notification-module">{{ item.module }}</span>
                  <span class="notification-time">{{ formatNotificationTime(item.createdAt) }}</span>
                </span>
                <span class="notification-title">{{ item.title }}</span>
                <span class="notification-message">{{ item.message }}</span>
              </button>
            </li>
          </ul>

          <div v-if="notifications.length" class="notification-dropdown-footer">
            <button type="button" class="notification-link" @click="clearNotifications">
              Clear all
            </button>
          </div>
        </div>
      </div>
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
import { computed, onBeforeUnmount, onMounted, provide, ref, watch } from 'vue'
import '../../assets/css/navigation.css'
import '../../assets/css/Dashboard.css'
import menuConfig from '../../assets/json/menu.json'
import { currentUser, notificationsAPI } from '../services/api'

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
    'signatories.html': { title: 'Signatories', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
    'account.html': { title: 'Account Information', section: 'Account', sectionIcon: 'far fa-user-circle' }
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
const userProfile = ref(currentUser())
const userName = computed(() => userProfile.value?.fullName || 'User')

function handleProfileChanged() {
  userProfile.value = currentUser()
}

/* ---- Notification bell ---- */
const notificationRoot = ref(null)
const notifications = ref([])
const notificationsOpen = ref(false)
const unreadCount = computed(() => notifications.value.filter((item) => !item.isRead).length)
const unreadBadge = computed(() => (unreadCount.value > 9 ? '9+' : String(unreadCount.value)))
let notificationPoll = null

async function loadNotifications() {
  try {
    notifications.value = await notificationsAPI.getAll()
  } catch {
    /* Huwag istorbohin ang page kung bigong mag-refresh ng notifications. */
  }
}

async function toggleNotifications() {
  notificationsOpen.value = !notificationsOpen.value
  if (notificationsOpen.value) await loadNotifications()
}

async function markRead(item) {
  if (item.isRead) return
  try {
    await notificationsAPI.markRead(item.id)
    item.isRead = true
  } catch {
    await loadNotifications()
  }
}

async function markAllRead() {
  if (unreadCount.value === 0) return
  try {
    await notificationsAPI.markAllRead()
    notifications.value.forEach((item) => { item.isRead = true })
  } catch {
    await loadNotifications()
  }
}

async function clearNotifications() {
  try {
    await notificationsAPI.clear()
    notifications.value = []
    notificationsOpen.value = false
  } catch {
    await loadNotifications()
  }
}

function formatNotificationTime(value) {
  if (!value) return ''

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return ''

  const diff = Date.now() - date.getTime()
  const minute = 60 * 1000
  const hour = 60 * minute
  const day = 24 * hour

  if (diff < minute) return 'Just now'
  if (diff < hour) return `${Math.floor(diff / minute)}m ago`
  if (diff < day) return `${Math.floor(diff / hour)}h ago`
  if (diff < 7 * day) return `${Math.floor(diff / day)}d ago`
  return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
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

  notificationsOpen.value = false
  closeSidebar()
}

function handleDocumentClick(event) {
  if (!notificationsOpen.value) return
  if (notificationRoot.value?.contains(event.target)) return
  notificationsOpen.value = false
}

function handleNotificationsChanged() {
  loadNotifications()
}

function handleResize() {
  if (window.innerWidth < 768) sidebarCollapsed.value = false
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  window.addEventListener('resize', handleResize)
  window.addEventListener('hashchange', syncHash)
  window.addEventListener('popstate', syncHash)
  document.addEventListener('click', handleDocumentClick)
  window.addEventListener('ojt:notifications-changed', handleNotificationsChanged)
  window.addEventListener('ojt:profile-changed', handleProfileChanged)

  loadNotifications()
  notificationPoll = window.setInterval(loadNotifications, 30000)

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
  document.removeEventListener('click', handleDocumentClick)
  window.removeEventListener('ojt:notifications-changed', handleNotificationsChanged)
  window.removeEventListener('ojt:profile-changed', handleProfileChanged)
  if (notificationPoll) window.clearInterval(notificationPoll)
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
  transition:
    transform 0.3s cubic-bezier(0.4, 0, 0.2, 1),
    width 0.3s cubic-bezier(0.4, 0, 0.2, 1),
    top 0.3s cubic-bezier(0.4, 0, 0.2, 1);
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

/* Hamburger bars -> X when the drawer/toggle is active */
.shell-menu-toggle,
.shell-mobile-toggle {
  display: inline-flex;
  align-items: center;
  justify-content: center;
}

.hamburger-bars {
  display: inline-flex;
  flex-direction: column;
  justify-content: space-between;
  width: 20px;
  height: 14px;
}

.hamburger-bars span {
  display: block;
  width: 100%;
  height: 2px;
  border-radius: 2px;
  background: currentColor;
  transform-origin: center;
  transition:
    transform 0.3s cubic-bezier(0.4, 0, 0.2, 1),
    opacity 0.2s ease;
}

.shell-menu-toggle.is-active .hamburger-bars span:nth-child(1),
.shell-mobile-toggle.is-active .hamburger-bars span:nth-child(1) {
  transform: translateY(6px) rotate(45deg);
}

.shell-menu-toggle.is-active .hamburger-bars span:nth-child(2),
.shell-mobile-toggle.is-active .hamburger-bars span:nth-child(2) {
  opacity: 0;
  transform: scaleX(0);
}

.shell-menu-toggle.is-active .hamburger-bars span:nth-child(3),
.shell-mobile-toggle.is-active .hamburger-bars span:nth-child(3) {
  transform: translateY(-6px) rotate(-45deg);
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
  transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
}

.main-sidebar .nav-sidebar .nav-item.menu-open > .nav-link .right {
  transform: rotate(-90deg);
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
  max-height: 0;
  opacity: 0;
  overflow: hidden;
  visibility: hidden;
  pointer-events: none;
  transition:
    max-height 0.3s cubic-bezier(0.4, 0, 0.2, 1),
    opacity 0.22s ease,
    visibility 0s linear 0.3s;
}

.main-sidebar .nav-treeview.is-open {
  max-height: 14rem;
  opacity: 1;
  visibility: visible;
  pointer-events: auto;
  transition:
    max-height 0.3s cubic-bezier(0.4, 0, 0.2, 1),
    opacity 0.22s ease 0.05s,
    visibility 0s;
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

/* ---------- Notification bell ---------- */
.navbar-notifications {
  position: relative;
  display: flex;
  align-items: center;
  margin-left: 0.85rem;
  z-index: 1040;
}

.notification-toggle {
  position: relative;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 2.5rem;
  height: 2.5rem;
  padding: 0;
  border: 0;
  border-radius: 999px;
  background: transparent;
  color: #ffffff;
  font-size: 1.4rem;
  line-height: 1;
  cursor: pointer;
}

.notification-toggle:hover,
.notification-toggle:focus-visible {
  background: rgba(255, 255, 255, 0.14);
  outline: none;
}

.notification-badge {
  position: absolute;
  top: 0.15rem;
  right: 0.05rem;
  min-width: 1.1rem;
  height: 1.1rem;
  padding: 0 0.28rem;
  border: 2px solid rgba(30, 15, 12, 0.82);
  border-radius: 999px;
  background: #dc2626;
  color: #ffffff;
  font-size: 0.62rem;
  font-weight: 800;
  line-height: 0.85rem;
  text-align: center;
  box-shadow: 0 0 0 1px rgba(255, 255, 255, 0.18);
}

.notification-dropdown {
  position: absolute;
  top: calc(100% + 0.65rem);
  right: 0;
  width: min(360px, calc(100vw - 2rem));
  overflow: hidden;
  border: 1px solid rgba(30, 15, 12, 0.08);
  border-radius: 14px;
  background: #ffffff;
  color: #1E0F0C;
  box-shadow: 0 18px 40px rgba(15, 23, 42, 0.24);
  z-index: 1060;
}

.notification-dropdown-header,
.notification-dropdown-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.75rem;
  padding: 0.8rem 1rem;
}

.notification-dropdown-header {
  border-bottom: 1px solid #f1e7d8;
}

.notification-dropdown-header strong {
  font-size: 0.9rem;
}

.notification-dropdown-footer {
  justify-content: flex-end;
  border-top: 1px solid #f1e7d8;
}

.notification-link {
  border: 0;
  background: transparent;
  color: #b85f00;
  font-size: 0.75rem;
  font-weight: 700;
  cursor: pointer;
}

.notification-link:disabled {
  opacity: 0.45;
  cursor: default;
}

.notification-empty {
  padding: 1.5rem 1rem;
  color: #6b7280;
  font-size: 0.85rem;
  text-align: center;
}

.notification-list {
  max-height: 360px;
  margin: 0;
  padding: 0;
  overflow-y: auto;
  list-style: none;
}

.notification-item + .notification-item {
  border-top: 1px solid #f4ece1;
}

.notification-item.unread {
  background: #fff7ec;
}

.notification-item-button {
  display: flex;
  flex-direction: column;
  gap: 0.2rem;
  width: 100%;
  padding: 0.75rem 1rem;
  border: 0;
  background: transparent;
  color: inherit;
  text-align: left;
  cursor: pointer;
}

.notification-item-button:hover,
.notification-item-button:focus-visible {
  background: #fffaf2;
  outline: none;
}

.notification-item-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 0.5rem;
}

.notification-module {
  padding: 0.12rem 0.4rem;
  border-radius: 999px;
  background: #fff0dc;
  color: #a85300;
  font-size: 0.6rem;
  font-weight: 800;
  letter-spacing: 0.05em;
  text-transform: uppercase;
}

.notification-time {
  color: #8b8b8b;
  font-size: 0.68rem;
  white-space: nowrap;
}

.notification-title {
  font-size: 0.82rem;
  font-weight: 700;
}

.notification-message {
  display: -webkit-box;
  overflow: hidden;
  color: #5b5b5b;
  font-size: 0.76rem;
  line-height: 1.3;
  -webkit-box-orient: vertical;
  -webkit-line-clamp: 2;
}

@media (max-width: 575.98px) {
  .navbar-notifications {
    margin-left: 0.4rem;
  }

  .notification-dropdown {
    position: fixed;
    top: 66px;
    right: 0.75rem;
    left: 0.75rem;
    width: auto;
  }
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
    transition:
      margin-left 0.3s cubic-bezier(0.4, 0, 0.2, 1),
      width 0.3s cubic-bezier(0.4, 0, 0.2, 1),
      transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
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