<template>
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
</template>

<script setup>
/*
 * Notification bell in the top bar. Loads the user's notifications, polls
 * every 30 seconds, and emits "open" with a page link when a notification
 * is clicked. The layout (BaseLayout.vue) does the actual navigation.
 */
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import '../../assets/css/notification-bell.css'
import { isIntern, isSupervisor, notificationsAPI } from '../services/api'
import { canOpen } from '../utils/access'
import { pageOf } from '../utils/navigation'

const emit = defineEmits(['open'])

const POLL_MS = 30000

/* Pages that can open with one intern already selected (?internId=). */
const INTERN_ID_PAGES = ['internlist.html', 'coc-generate.html', 'idgenerate.html', 'supervisor-dtr.html']

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
    /* If a refresh fails, keep the page as it is. */
  }
}

async function toggleNotifications() {
  notificationsOpen.value = !notificationsOpen.value
  if (notificationsOpen.value) await loadNotifications()
}

async function markRead(item) {
  if (!item.isRead) {
    try {
      await notificationsAPI.markRead(item.id)
      item.isRead = true
    } catch {
      await loadNotifications()
    }
  }

  /* Some notifications point to a page (for example, an intern who finished).
     Close the list and open that page. */
  const link = notificationDestination(item)
  if (link) {
    notificationsOpen.value = false
    emit('open', link)
  }
}

/* Completion alerts store /requests/certificate-completion plus internId.
   That opens the certificate page with the intern already selected. */
function notificationDestination(item) {
  const internId = Number(item?.internId)
  const hasIntern = Number.isFinite(internId) && internId > 0
  let link = String(item?.link || '').trim()

  const completion = item?.type === 'InternCompleted'
    || link === '/requests/certificate-completion'
    || link.endsWith('/certificate-completion')
  if (completion) link = '/coc-generate.html'

  /* Every item should be clickable. If there is no saved link (old
     notifications), or the user's role cannot open it (for example, an HR
     link on a supervisor's notification), guess the page from the type or module. */
  if (!link || !canOpen(pageOf(link))) link = notificationFallback(item)
  if (!link || !canOpen(pageOf(link))) return ''

  /* Only add internId to pages that use it. */
  if (hasIntern && INTERN_ID_PAGES.includes(pageOf(link)) && !/[?&]internId=/.test(link)) {
    link += `${link.includes('?') ? '&' : '?'}internId=${internId}`
  }

  return link
}

/* Page to open based on the notification type or module, so that
   notifications without a saved link still open something useful. */
function notificationFallback(item) {
  const type = String(item?.type || '').trim()
  const module = String(item?.module || '').trim().toLowerCase()

  if (isSupervisor()) {
    if (type === 'AttendanceAdjustment' || module === 'attendance') return '/supervisor-time-adjustments.html'
    if (type === 'InternCompleted' || type === 'Intern' || module === 'interns') {
      return Number(item?.internId) > 0 ? '/supervisor-dtr.html' : '/supervisor-interns.html'
    }
    if (type === 'Applicant' || module === 'applicants') return '/supervisor-orientations.html'
    if (type === 'InternRequest' || module === 'applicant requests') return '/supervisor-requests.html'
    return ''
  }

  const byType = {
    Intern: '/internlist.html',
    Applicant: '/applicants.html',
    InternRequest: '/requests.html',
    Signatory: '/signatories.html',
    School: '/schools.html',
    DocumentRequested: isIntern() ? '/intern-documents.html' : '/idgenerate.html',
    AttendanceAdjustment: isIntern() ? '/intern-attendance.html' : '/time-adjustments.html'
  }
  if (byType[type]) return byType[type]

  const byModule = {
    interns: '/internlist.html',
    applicants: '/applicants.html',
    'applicant requests': '/requests.html',
    signatories: '/signatories.html',
    schools: '/schools.html',
    documents: isIntern() ? '/intern-documents.html' : '/idgenerate.html',
    attendance: isIntern() ? '/intern-attendance.html' : '/time-adjustments.html',
    'certificate of completion': '/coc-generate.html'
  }
  return byModule[module] || ''
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

/* Close the list on Escape or on a click outside the bell. */
function handleKeydown(event) {
  if (event.key === 'Escape') notificationsOpen.value = false
}

function handleDocumentClick(event) {
  if (!notificationsOpen.value) return
  if (notificationRoot.value?.contains(event.target)) return
  notificationsOpen.value = false
}

onMounted(() => {
  window.addEventListener('keydown', handleKeydown)
  document.addEventListener('click', handleDocumentClick)
  /* Pages fire this after a change, so the bell refreshes without waiting for the poll. */
  window.addEventListener('ojt:notifications-changed', loadNotifications)

  loadNotifications()
  notificationPoll = window.setInterval(loadNotifications, POLL_MS)
})

onBeforeUnmount(() => {
  window.removeEventListener('keydown', handleKeydown)
  document.removeEventListener('click', handleDocumentClick)
  window.removeEventListener('ojt:notifications-changed', loadNotifications)
  if (notificationPoll) window.clearInterval(notificationPoll)
})
</script>
