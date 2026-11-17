<template>
  <section class="content dashboard-page" :style="{ '--scroll': scrollY }">
    <div class="row">
      <div class="col-md-12">
        <div class="ojt-hero">
          <div class="ojt-hero-inner">
            <h1 class="ojt-hero-title">OJTMIS</h1>
            <div class="ojt-hero-actions">
              <a class="ojt-btn ojt-btn-primary" href="/requests.html" @click.prevent="go('requests.html')">
                Request for Intern
              </a>
              <a class="ojt-btn ojt-btn-ghost" href="/internlist.html" @click.prevent="go('internlist.html')">
                Monitor Interns
              </a>
            </div>
          </div>

          <button type="button" class="ojt-scroll-cue" aria-label="Scroll to overview" @click="scrollToSummary">
            <span></span>
            <span></span>
          </button>
        </div>

        <section ref="summaryEl" class="ojt-summary">
          <h2 class="ojt-section-title">Overview</h2>

          <div class="ojt-stats">
            <a class="stat-card stat-blue reveal" href="/requests.html" @click.prevent="go('requests.html')">
              <div>
                <span class="stat-label">Requests</span>
                <span class="stat-number">{{ internRequests }}</span>
              </div>
              <i class="fas fa-file-alt"></i>
            </a>

            <a class="stat-card stat-purple reveal" href="/applicants.html" @click.prevent="go('applicants.html')">
              <div>
                <span class="stat-label">Applicants</span>
                <span class="stat-number">{{ internApplicants }}</span>
              </div>
              <i class="fas fa-users"></i>
            </a>

            <a class="stat-card stat-orange reveal" href="/internlist.html" @click.prevent="go('internlist.html')">
              <div>
                <span class="stat-label">Active Interns</span>
                <span class="stat-number">{{ activeInterns }}</span>
              </div>
              <i class="fas fa-user-graduate"></i>
            </a>
          </div>

          <div class="ojt-info-grid">
            <article class="ojt-info reveal">
              <h3>Last Active Request</h3>
              <template v-if="lastRequest">
                <p class="ojt-info-main">#{{ lastRequest.id }} &middot; {{ lastRequest.officeName }}</p>
                <p class="ojt-info-meta">{{ lastRequest.status }} &middot; {{ lastRequest.remaining ?? 0 }} slot(s) left</p>
                <span class="ojt-chip">Requested {{ formatDate(lastRequest.createdAt) }}</span>
              </template>
              <p v-else class="ojt-info-meta">No active request yet.</p>
            </article>

            <article class="ojt-info reveal">
              <h3>Upcoming MOA Expiry</h3>
              <ul v-if="moaList.length" class="ojt-list">
                <li v-for="s in moaList" :key="s.id">
                  <span>{{ s.abbreviation || s.name }}</span>
                  <span class="ojt-chip" :class="{ 'is-warn': s.days <= 30 }">{{ formatDate(s.moaExpiry) }}</span>
                </li>
              </ul>
              <p v-else class="ojt-info-meta">No MOA expiring within a year.</p>
            </article>

            <article class="ojt-info reveal">
              <h3>Summary</h3>
              <ul class="ojt-list">
                <li><span>Schools with active MOA</span><strong>{{ activeMoa }}</strong></li>
                <li><span>Open requests</span><strong>{{ openRequests }}</strong></li>
                <li><span>Pending applicants</span><strong>{{ internApplicants }}</strong></li>
              </ul>
            </article>
          </div>
        </section>
      </div>
    </div>
  </section>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { internsAPI, internRequestsAPI, applicantsAPI, schoolsAPI } from '@/services/api'
import { useHeroDashboard } from '@/utils/dashboard'
import { go } from '@/utils/navigation'
import '../assets/css/Dashboard.css'

const { scrollY, summaryEl, scrollToSummary } = useHeroDashboard()

const activeInterns = ref(0)
const internRequests = ref(0)
const internApplicants = ref(0)
const openRequests = ref(0)
const activeMoa = ref(0)
const lastRequest = ref(null)
const moaList = ref([])

function formatDate(v) {
  const d = v ? new Date(v) : null
  return d && !Number.isNaN(d.getTime())
    ? d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })
    : '-'
}

async function loadStats() {
  try {
    const [interns, requests, applicants, schools] = await Promise.all([
      internsAPI.getAll(),
      internRequestsAPI.getAll(),
      applicantsAPI.getAll(),
      schoolsAPI.getAll()
    ])
    activeInterns.value = interns.length
    internRequests.value = requests.length
    // Count only applicants not yet hired, so this matches the
    // Applicants table (see displayApplicants in applicants.vue).
    internApplicants.value = applicants.filter(a => !a.hired).length

    const open = requests.filter(r => (r.status || '').toLowerCase() === 'open')
    openRequests.value = open.length
    lastRequest.value = [...open].sort((a, b) => new Date(b.createdAt) - new Date(a.createdAt))[0] || null

    activeMoa.value = schools.filter(s => s.moaStatus).length
    const now = Date.now()
    moaList.value = schools
      .filter(s => s.moaExpiry)
      .map(s => ({ ...s, days: Math.ceil((new Date(s.moaExpiry) - now) / 86400000) }))
      .filter(s => s.days <= 365)
      .sort((a, b) => a.days - b.days)
      .slice(0, 4)
  } catch (error) {
    console.error('Failed to load dashboard stats:', error)
  }
}

onMounted(loadStats)
</script>



