<template>
  <div class="app-shell hold-transition sidebar-mini layout-fixed layout-navbar-fixed" :class="{ 'sidebar-collapsed': sidebarCollapsed, 'dashboard-shell': currentPage === 'dashboard.html' }">
    <header class="main-header navbar navbar-expand navbar-orange navbar-dark elevation-2">
      <a href="/dashboard.html" class="main-header-logo" aria-label="OJTMIS Dashboard" @click="navigate($event, 'dashboard.html')">
      </a>

      <button type="button" class="nav-link shell-menu-toggle" :class="{ 'is-collapsed': sidebarCollapsed }" aria-label="Toggle sidebar" @click="toggleSidebar">
        <i class="fas fa-bars"></i>
      </button>

      <div class="navbar-user dropdown" :class="{ show: userMenuOpen }">
        <button type="button" class="nav-link dropdown-toggle" :aria-expanded="userMenuOpen" @click.stop="toggleUserMenu">
          <i class="fas fa-user mr-1"></i>{{ user.name }}
        </button>
        <div class="dropdown-menu dropdown-menu-right" :class="{ show: userMenuOpen }">
          <a v-for="item in user.menu" :key="item.name" :href="item.link" class="dropdown-item" @click="handleUserMenuItem($event, item)">
            <i :class="[item.icon, 'mr-2']"></i>{{ item.name }}
          </a>
        </div>
      </div>
    </header>

    <aside class="main-sidebar sidebar-dark-primary elevation-4">
      <a href="/" class="brand-link text-center bg-orange">
        <span class="brand-text font-weight-light text-light">OJTMIS</span>
      </a>

      <nav class="sidebar mt-3" aria-label="Main navigation">
        <ul class="nav nav-pills nav-sidebar flex-column">
          <li v-for="item in menu" :key="item.name" class="nav-item" :class="{ 'menu-open': openMenus.has(item.name) }">
            <a
              v-if="item.submenu"
              href="#"
              class="nav-link"
              :class="{ active: hasActiveChild(item) }"
              @click.prevent="toggleMenu(item)"
            >
              <i :class="['nav-icon', item.icon]"></i>
              <p>{{ item.name }} <i class="right fas fa-angle-left" :class="{ 'is-open': openMenus.has(item.name) }"></i></p>
            </a>
            <a v-else :href="item.link" class="nav-link" :class="{ active: isActive(item.link) }" @click="navigate($event, item.link)">
              <i :class="['nav-icon', item.icon]"></i>
              <p>{{ item.name }}</p>
            </a>

            <Transition name="submenu">
              <ul v-if="item.submenu && openMenus.has(item.name)" class="nav nav-treeview">
                <li v-for="child in item.submenu" :key="child.name" class="nav-item">
                  <a :href="child.link" class="nav-link" :class="{ active: isActive(child.link) }" @click="navigate($event, child.link)">
                    <p>{{ child.name }}</p>
                  </a>
                </li>
              </ul>
            </Transition>
          </li>
        </ul>
      </nav>
    </aside>

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
              <a :href="`/${currentPage}`" class="text-orange">{{ pageMeta.title }}</a>
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
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import '../../assets/css/navigation.css'
import '../../assets/css/Dashboard.css'

const props = defineProps({
  currentPage: {
    type: String,
    default: 'dashboard.html'
  }
})

const emit = defineEmits(['navigate'])

const pageMeta = computed(() => ({
  'dashboard.html': { title: 'Dashboard' },
  'requests.html': { title: 'List of Requests', section: 'Request', sectionIcon: 'fas fa-table' },
  'applicants.html': { title: 'List of Applicants', section: 'Applicants', sectionIcon: 'far fa-file' },
  'intern-list.html': { title: 'Intern List', section: 'Interns', sectionIcon: 'fas fa-list' },
  'schedule-orientation.html': { title: 'Schedule Orientation', section: 'Applicants', sectionIcon: 'far fa-file' },
  'coc.html': { title: 'Issuance of COC', section: 'Forms', sectionIcon: 'fas fa-file' },
  'dtr.html': { title: 'Daily Time Records', section: 'Forms', sectionIcon: 'fas fa-file' },
  'id-generate.html': { title: 'ID Generation', section: 'Forms', sectionIcon: 'fas fa-file' },
  'utility.html': { title: 'User Management', section: 'Utility', sectionIcon: 'fas fa-table' },
  'programs.html': { title: 'Programs / Strands', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
  'schools.html': { title: 'List of Schools', section: 'System Settings', sectionIcon: 'fas fa-cogs' },
  'signatories.html': { title: 'List of Signatories', section: 'System Settings', sectionIcon: 'fas fa-cogs' }
}[props.currentPage] || { title: 'OJTMIS' }))

const menu = ref([])
const user = ref({ name: 'User', menu: [] })
const openMenus = ref(new Set())
const sidebarCollapsed = ref(false)
const userMenuOpen = ref(false)

function isActive(link) {
  return props.currentPage === (link || '').toLowerCase()
}

function hasActiveChild(item) {
  return (item.submenu || []).some((child) => isActive(child.link))
}

function toggleMenu(item) {
  const next = new Set(openMenus.value)
  if (next.has(item.name)) next.delete(item.name)
  else next.add(item.name)
  openMenus.value = next
}

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value
}

function toggleUserMenu() {
  userMenuOpen.value = !userMenuOpen.value
}

function closeUserMenu(event) {
  if (!event.target.closest('.navbar-user')) userMenuOpen.value = false
}

function handleUserMenuItem(event, item) {
  if ((item.link || '').toLowerCase() !== 'login.html') return
  event.preventDefault()
  userMenuOpen.value = false
  window.history.pushState({}, '', '/login.html')
  emit('navigate', 'login.html')
}

function navigate(event, link) {
  const page = (link || '').toLowerCase()
  const normalizedPage = page === 'forms.aspx' ? 'coc.html' : page === 'dtr.aspx' ? 'dtr.html' : page === 'idgenerate.aspx' ? 'id-generate.html' : page === 'utility.aspx' ? 'utility.html' : page === 'programslist.aspx' ? 'programs.html' : page === 'schools.aspx' ? 'schools.html' : page === 'signatories.aspx' ? 'signatories.html' : page
  if (!['dashboard.html', 'requests.html', 'applicants.html', 'intern-list.html', 'schedule-orientation.html', 'coc.html', 'dtr.html', 'id-generate.html', 'utility.html', 'programs.html', 'schools.html', 'signatories.html'].includes(normalizedPage)) return
  event.preventDefault()
  window.history.pushState({}, '', `/${normalizedPage}`)
  emit('navigate', normalizedPage)
}

onMounted(async () => {
  try {
    const response = await fetch('/assets/json/menu.json')
    if (!response.ok) throw new Error(`Failed to load menu.json: ${response.status}`)
    const data = await response.json()
    menu.value = data.menu || []
    user.value = data.navbar?.user || user.value
    openMenus.value = new Set(menu.value.filter(hasActiveChild).map((item) => item.name))
  } catch (error) {
    console.error('BaseLayout menu error:', error)
  }
  document.addEventListener('click', closeUserMenu)
})

onBeforeUnmount(() => {
  document.removeEventListener('click', closeUserMenu)
})
</script>

<style>
@import url('https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.4/css/all.min.css');
@import url('https://cdn.jsdelivr.net/npm/bootstrap@4.6.2/dist/css/bootstrap.min.css');
@import url('https://cdn.jsdelivr.net/npm/admin-lte@3.2/dist/css/adminlte.min.css');

:root {
  font-family: 'Segoe UI', sans-serif;
}

body {
  margin: 0;
  min-width: 320px;
  overflow-x: hidden;
}

.app-shell {
  min-height: 100vh;
  overflow-x: hidden;
  background-color: #f4f7fb;
  background-image: linear-gradient(rgba(6, 64, 43, 0.035) 1px, transparent 1px), linear-gradient(90deg, rgba(6, 64, 43, 0.035) 1px, transparent 1px);
  background-size: 28px 28px;
}

.main-header-logo,
.shell-menu-toggle,
.navbar-user {
  position: relative;
  z-index: 1040;
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

.main-header-logo img {
  width: 34px;
  height: 34px;
  object-fit: contain;
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

.main-sidebar,
.sidebar,
.nav-sidebar {
  max-width: 250px;
  overflow-x: hidden;
}

.nav-sidebar .nav-link,
.nav-sidebar .nav-link p {
  min-width: 0;
}

.sidebar-collapsed .main-sidebar {
  margin-left: -250px;
}

@media (min-width: 768px) {
  .main-sidebar,
  .main-header,
  .shell-content,
  .main-footer {
    transition: margin-left 0.3s ease-in-out;
  }

  .main-header {
    margin-left: 250px !important;
  }

  .sidebar-collapsed .main-header,
  .sidebar-collapsed .shell-content,
  .sidebar-collapsed .main-footer {
    margin-left: 0 !important;
  }
}

.navbar-user {
  margin-left: auto;
}

.shell-content {
  min-height: calc(100vh - 57px - 58px);
}

.shell-body {
  padding-bottom: 2rem;
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
  margin-left: 250px;
  margin-bottom: 0;
}

.sidebar-collapsed > .main-footer {
  margin-left: 0;
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
}
</style>
